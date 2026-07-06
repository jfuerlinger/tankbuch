using System.Text;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tankbuch.Api.Domain;
using Tankbuch.Api.Endpoints;
using Tankbuch.Api.Services;
using Tankbuch.Contracts;

namespace Tankbuch.Api.Features.Csv;

/// <summary>Vollständiger Datenbestand als CSV (Semikolon, Dezimal-Komma, UTF-8 mit BOM).</summary>
public sealed class CsvExportEndpoint : ApiEndpoint<object>
{
    public override void Configure()
    {
        Get("/api/csv/export");
        AllowAnonymous();
        Summary(s => s.Summary = "Alle Tankvorgänge als CSV exportieren (Backup)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        var vehicles = await Db.Fahrzeuge.Where(f => f.TenantId == TenantId).ToDictionaryAsync(f => f.Id, ct);
        var entries = await Db.Tankvorgaenge.Where(t => t.TenantId == TenantId).ToListAsync(ct);
        var csv = CsvService.Export(vehicles, entries);
        var bytes = Encoding.UTF8.GetBytes(csv);
        await Send.BytesAsync(bytes, $"tankbuch-export-{DateOnly.FromDateTime(DateTime.Now):yyyy-MM-dd}.csv",
            "text/csv", cancellation: ct);
    }
}

/// <summary>CSV importieren/zusammenführen (Restore). Neue Fahrzeuge werden angelegt, Duplikate übersprungen.</summary>
public sealed class CsvImportEndpoint : ApiEndpoint<CsvImportResult>
{
    private static readonly string[] Colors = { "#3B82F6", "#2DD4BF", "#A78BFA", "#F472B6", "#34D399", "#FB923C" };

    public override void Configure()
    {
        Post("/api/csv/import");
        AllowAnonymous();
        AllowFileUploads();
        Summary(s => s.Summary = "CSV importieren und mit dem Bestand zusammenführen");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;

        var text = await ReadTextAsync(ct);
        if (string.IsNullOrWhiteSpace(text)) ThrowError("Bitte eine CSV-Datei hochladen");

        var rows = CsvService.Parse(text);
        if (rows.Count == 0) { await Send.OkAsync(new CsvImportResult(0, 0, 0, "Die Datei enthält keine gültigen Daten oder Pflichtspalten fehlen."), ct); return; }

        var vehicles = await Db.Fahrzeuge.Where(f => f.TenantId == TenantId).ToListAsync(ct);
        var entries = await Db.Tankvorgaenge.Where(t => t.TenantId == TenantId).ToListAsync(ct);
        int added = 0, skipped = 0, newVeh = 0;

        foreach (var row in rows)
        {
            var name = string.IsNullOrWhiteSpace(row.Fahrzeug) ? "Importiertes Fahrzeug" : row.Fahrzeug;
            var v = vehicles.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (v is null)
            {
                v = new Fahrzeug
                {
                    Id = Guid.NewGuid(),
                    TenantId = TenantId,
                    Name = name,
                    Kennzeichen = row.Kennzeichen,
                    Kraftstoffart = "Diesel",
                    Farbe = Colors[vehicles.Count % Colors.Length],
                    AnfangsKilometer = 0,
                };
                vehicles.Add(v);
                Db.Fahrzeuge.Add(v);
                newVeh++;
            }

            if (row.Datum is not DateOnly datum || row.Liter is not > 0m) { skipped++; continue; }
            var liter = Math.Round(row.Liter.Value, 2, MidpointRounding.AwayFromZero);
            long km = row.Kilometerstand ?? 0;
            var ppl = row.PreisProLiter is > 0m ? row.PreisProLiter.Value
                : (row.Gesamtpreis is > 0m ? Berechnung.PreisProLiter(row.Gesamtpreis.Value, liter) : 0m);
            var total = row.Gesamtpreis is > 0m ? Math.Round(row.Gesamtpreis.Value, 2, MidpointRounding.AwayFromZero)
                : (ppl > 0 ? Math.Round(liter * ppl, 2, MidpointRounding.AwayFromZero) : 0m);

            if (entries.Any(e => e.FahrzeugId == v.Id && e.Datum == datum && e.Kilometerstand == km)) { skipped++; continue; }

            var e2 = new Tankvorgang
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                FahrzeugId = v.Id,
                Datum = datum,
                Liter = liter,
                PreisProLiter = ppl,
                Gesamtpreis = total,
                Kilometerstand = km,
                Tankstelle = row.Tankstelle,
                Notiz = row.Notiz,
                Volltankung = row.Volltankung,
            };
            entries.Add(e2);
            Db.Tankvorgaenge.Add(e2);
            added++;
        }

        // Anfangs-Kilometer neuer Fahrzeuge aus dem kleinsten km-Stand ableiten.
        foreach (var v in vehicles.Where(v => v.AnfangsKilometer == 0))
        {
            var ks = entries.Where(e => e.FahrzeugId == v.Id && e.Kilometerstand > 0).Select(e => e.Kilometerstand).ToList();
            if (ks.Count > 0) v.AnfangsKilometer = ks.Min();
        }

        await Db.SaveChangesAsync(ct);

        var meldung = $"{added} Einträge importiert, {skipped} übersprungen" + (newVeh > 0 ? $", {newVeh} neue Fahrzeuge" : "");
        await Send.OkAsync(new CsvImportResult(added, skipped, newVeh, meldung), ct);
    }

    private async Task<string> ReadTextAsync(CancellationToken ct)
    {
        if (HttpContext.Request.HasFormContentType)
        {
            var form = await HttpContext.Request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file is not null)
            {
                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
                return await reader.ReadToEndAsync(ct);
            }
            if (form.TryGetValue("csv", out var raw)) return raw.ToString();
            return "";
        }
        using var r = new StreamReader(HttpContext.Request.Body, Encoding.UTF8);
        return await r.ReadToEndAsync(ct);
    }
}
