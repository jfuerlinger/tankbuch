using System.Globalization;
using System.Text;
using Tankbuch.Api.Domain;

namespace Tankbuch.Api.Services;

public readonly record struct CsvZeile(
    string Fahrzeug, string Kennzeichen, DateOnly? Datum, decimal? Liter,
    decimal? PreisProLiter, decimal? Gesamtpreis, long? Kilometerstand,
    string Tankstelle, bool Volltankung, string Notiz);

/// <summary>
/// CSV-Export/-Import in österreichischer/deutscher Konvention:
/// Trennzeichen Semikolon, Dezimal-Komma, UTF-8 mit BOM.
/// Spalten: fahrzeug;kennzeichen;datum;liter;preis_pro_liter;gesamtpreis;kilometerstand;tankstelle;volltankung;notiz
/// </summary>
public static class CsvService
{
    public static readonly string[] Header =
        { "fahrzeug", "kennzeichen", "datum", "liter", "preis_pro_liter", "gesamtpreis", "kilometerstand", "tankstelle", "volltankung", "notiz" };

    public static string Export(IReadOnlyDictionary<Guid, Fahrzeug> fahrzeuge, IEnumerable<Tankvorgang> tankvorgaenge)
    {
        var sb = new StringBuilder();
        sb.Append('﻿');
        sb.Append(string.Join(';', Header)).Append("\r\n");

        foreach (var e in tankvorgaenge.OrderBy(x => x.Datum))
        {
            fahrzeuge.TryGetValue(e.FahrzeugId, out var v);
            string[] cells =
            {
                v?.Name ?? "",
                v?.Kennzeichen ?? "",
                e.Datum.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                De(e.Liter, 2),
                De(e.PreisProLiter, 3),
                De(e.Gesamtpreis, 2),
                e.Kilometerstand.ToString(CultureInfo.InvariantCulture),
                e.Tankstelle ?? "",
                e.Volltankung ? "ja" : "nein",
                e.Notiz ?? "",
            };
            sb.Append(string.Join(';', cells.Select(Quote))).Append("\r\n");
        }
        return sb.ToString();
    }

    public static IReadOnlyList<CsvZeile> Parse(string text)
    {
        var rows = new List<CsvZeile>();
        var lines = text.Replace("﻿", "").Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Trim().Length > 0).ToList();
        if (lines.Count < 2) return rows;

        var header = ParseLine(lines[0]).Select(h => h.Trim().ToLowerInvariant()).ToList();
        int Idx(string n) => header.IndexOf(n);
        int iFz = Idx("fahrzeug"), iKz = Idx("kennzeichen"), iDat = Idx("datum"), iLit = Idx("liter"),
            iPpl = Idx("preis_pro_liter"), iTot = Idx("gesamtpreis"), iKm = Idx("kilometerstand"),
            iTank = Idx("tankstelle"), iVoll = Idx("volltankung"), iNot = Idx("notiz");

        if (iDat < 0 || iLit < 0) return rows; // Pflichtspalten fehlen

        for (int i = 1; i < lines.Count; i++)
        {
            var c = ParseLine(lines[i]);
            string G(int idx) => idx >= 0 && idx < c.Count ? c[idx].Trim() : "";
            rows.Add(new CsvZeile(
                Fahrzeug: G(iFz),
                Kennzeichen: G(iKz),
                Datum: ParseDatum(G(iDat)),
                Liter: ParseDe(G(iLit)),
                PreisProLiter: ParseDe(G(iPpl)),
                Gesamtpreis: ParseDe(G(iTot)),
                Kilometerstand: long.TryParse(G(iKm), NumberStyles.Any, CultureInfo.InvariantCulture, out var km) ? km
                    : (ParseDe(G(iKm)) is decimal d ? (long)Math.Round(d) : null),
                Tankstelle: G(iTank),
                Volltankung: System.Text.RegularExpressions.Regex.IsMatch(G(iVoll), "^(ja|true|1|x)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                Notiz: G(iNot)));
        }
        return rows;
    }

    private static string De(decimal n, int digits) => n.ToString("F" + digits, CultureInfo.InvariantCulture).Replace('.', ',');

    private static string Quote(string s) => s.IndexOfAny([';', '"', '\n']) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    public static decimal? ParseDe(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        if (t.Contains(',')) t = t.Replace(".", "").Replace(',', '.');
        return decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    private static DateOnly? ParseDatum(string raw)
    {
        var m = System.Text.RegularExpressions.Regex.Match(raw, @"^(\d{1,2})\.(\d{1,2})\.(\d{4})$");
        if (m.Success)
            return new DateOnly(int.Parse(m.Groups[3].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[1].Value));
        return DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }

    private static List<string> ParseLine(string l)
    {
        var outp = new List<string>();
        var cur = new StringBuilder();
        bool q = false;
        for (int i = 0; i < l.Length; i++)
        {
            char ch = l[i];
            if (q)
            {
                if (ch == '"')
                {
                    if (i + 1 < l.Length && l[i + 1] == '"') { cur.Append('"'); i++; }
                    else q = false;
                }
                else cur.Append(ch);
            }
            else if (ch == '"') q = true;
            else if (ch == ';') { outp.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(ch);
        }
        outp.Add(cur.ToString());
        return outp;
    }
}
