using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Tankbuch.Api.Services;

/// <summary>
/// Reine, unit-testbare Hilfsfunktionen zum robusten Auslesen der Vision-Modell-Antwort.
/// Vision-Modelle (llama3.2-vision) halten sich nicht immer exakt an das geforderte JSON-Format:
/// sie umschließen die Antwort z. B. mit Markdown-Codefences, hängen Erklärtext an oder liefern
/// Zahlen im deutschen Format (Komma als Dezimaltrennzeichen, Punkt als Tausendertrennzeichen).
/// Diese Klasse fängt die üblichen Abweichungen ab, statt bei der ersten Unregelmäßigkeit auf die
/// Simulation zurückzufallen.
/// </summary>
public static class VisionParsing
{
    /// <summary>Sucht das erste vollständige, balancierte JSON-Objekt im Antworttext (auch wenn
    /// von Markdown-Codefences oder Fließtext umgeben) und parst es. Liefert false, wenn kein
    /// gültiges JSON-Objekt gefunden werden konnte.</summary>
    public static bool TryExtractJson(string? text, out JsonElement result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var start = text.IndexOf('{');
        while (start >= 0)
        {
            var depth = 0;
            for (var i = start; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        var candidate = text[start..(i + 1)];
                        try
                        {
                            using var doc = JsonDocument.Parse(candidate);
                            result = doc.RootElement.Clone();
                            return true;
                        }
                        catch (JsonException)
                        {
                            // Kandidat war kein gültiges JSON (z. B. Brace innerhalb eines Strings) – weitersuchen.
                        }
                        break;
                    }
                }
            }
            start = text.IndexOf('{', start + 1);
        }
        return false;
    }

    /// <summary>Liest ein numerisches Feld robust aus: akzeptiert JSON-Zahlen sowie Strings im
    /// deutschen ("34,56" / "1.234,56") oder invarianten Format ("34.56"), auch mit angehängten
    /// Einheiten ("34,56 l", "72,30 €", "48.230 km"). Liefert true, wenn das Feld vorhanden war
    /// (auch bei explizitem JSON null, dann value = null).</summary>
    public static bool TryGetFlexibleNumber(JsonElement element, string property, out decimal? value)
    {
        value = null;
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var p))
            return false;

        switch (p.ValueKind)
        {
            case JsonValueKind.Null:
                value = null;
                return true;
            case JsonValueKind.Number when p.TryGetDecimal(out var d):
                value = d;
                return true;
            case JsonValueKind.String when TryParseFlexibleDecimal(p.GetString(), out var s):
                value = s;
                return true;
            default:
                return false;
        }
    }

    /// <summary>Parst eine Zahl unabhängig davon, ob Komma oder Punkt als Dezimaltrennzeichen
    /// verwendet wurde, und entfernt Einheiten/Währungssymbole (l, km, €, EUR, …).</summary>
    public static bool TryParseFlexibleDecimal(string? raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        // Nur Ziffern, Komma, Punkt und Vorzeichen behalten (Einheiten/Währung/Leerzeichen entfernen).
        var s = Regex.Replace(raw.Trim(), "[^0-9,.\\-]", "");
        if (s.Length == 0) return false;

        var lastComma = s.LastIndexOf(',');
        var lastDot = s.LastIndexOf('.');
        string normalized;
        if (lastComma >= 0 && lastDot >= 0)
        {
            // Beide Trennzeichen vorhanden: das später stehende ist das Dezimaltrennzeichen.
            normalized = lastComma > lastDot
                ? s.Replace(".", "").Replace(',', '.')
                : s.Replace(",", "");
        }
        else if (lastComma >= 0)
        {
            // Nur Komma → deutsches Dezimaltrennzeichen (typisch für €/l und Liter).
            normalized = s.Replace(",", ".");
        }
        else
        {
            // Nur Punkt (oder gar kein Trennzeichen) → bereits invariantes Format.
            normalized = s;
        }

        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Liest den Kilometerstand robust aus: erwartet eine Ganzzahl, daher werden alle
    /// Trennzeichen (Punkt/Komma/Leerzeichen als Tausendertrennzeichen) einfach entfernt statt
    /// als Dezimaltrennzeichen interpretiert – ein Tacho hat keine Nachkommastellen.</summary>
    public static bool TryGetFlexibleInteger(JsonElement element, string property, out long? value)
    {
        value = null;
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var p))
            return false;

        switch (p.ValueKind)
        {
            case JsonValueKind.Null:
                value = null;
                return true;
            case JsonValueKind.Number when p.TryGetInt64(out var l):
                value = l;
                return true;
            case JsonValueKind.Number when p.TryGetDecimal(out var d):
                value = (long)Math.Round(d);
                return true;
            case JsonValueKind.String when TryParseFlexibleInteger(p.GetString(), out var s):
                value = s;
                return true;
            default:
                return false;
        }
    }

    public static bool TryParseFlexibleInteger(string? raw, out long value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var digits = Regex.Replace(raw.Trim(), "[^0-9\\-]", "");
        return long.TryParse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Kürzt Text für Log-/Diagnoseausgaben auf eine handhabbare Länge.</summary>
    public static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= maxLength ? text : text[..maxLength] + "…";
    }
}
