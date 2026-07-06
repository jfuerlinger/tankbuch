using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Tankbuch.Cli.Infrastructure;
using Tankbuch.Contracts;

namespace Tankbuch.Cli.Commands;

public sealed class EntryListSettings : ApiSettings
{
    [CommandOption("-v|--vehicle <ID>"), Description("Nur Tankvorgänge dieses Fahrzeugs.")] public Guid? Vehicle { get; set; }
    [CommandOption("-d|--days <N>"), Description("Nur die letzten N Tage.")] public int? Days { get; set; }
}

/// <summary>tb entries list – listet Tankvorgänge (optional gefiltert).</summary>
public sealed class EntriesListCommand : TbCommand<EntryListSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, EntryListSettings s)
    {
        var vehicles = (await client.ListFahrzeugeAsync()).ToDictionary(v => v.Id, v => v.Name);
        var list = await client.ListTankvorgaengeAsync(s.Vehicle, s.Days);
        if (list.Count == 0) { AnsiConsole.MarkupLine("[grey]Keine Tankvorgänge gefunden.[/]"); return 0; }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumns("Id", "Datum", "Fahrzeug", "Liter", "€/l", "Gesamt", "km-Stand", "l/100km", "Voll");
        foreach (var e in list)
            table.AddRow(e.Id.ToString(), Fmt.Date(e.Datum), vehicles.GetValueOrDefault(e.FahrzeugId, "?"),
                Fmt.N(e.Liter, 2), Fmt.N(e.PreisProLiter, 3), Fmt.N(e.Gesamtpreis, 2) + " €",
                Fmt.N(e.Kilometerstand), Fmt.N(e.Verbrauch, 1), e.Volltankung ? "ja" : "nein");
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLineInterpolated($"[grey]{list.Count} Tankvorgänge.[/]");
        return 0;
    }
}

public sealed class EntryAddSettings : ApiSettings
{
    [CommandOption("-v|--vehicle <ID>"), Description("Fahrzeug-ID (erforderlich).")] public Guid Vehicle { get; set; }
    [CommandOption("-d|--date <DATUM>"), Description("Datum (TT.MM.JJJJ oder JJJJ-MM-TT; Standard: heute).")] public string? Date { get; set; }
    [CommandOption("-l|--liter <LITER>"), Description("Getankte Liter (erforderlich).")] public string? Liter { get; set; }
    [CommandOption("-p|--ppl <PREIS>"), Description("Preis pro Liter (optional; wird sonst berechnet).")] public string? Ppl { get; set; }
    [CommandOption("-t|--total <BETRAG>"), Description("Gesamtpreis (erforderlich).")] public string? Total { get; set; }
    [CommandOption("-k|--km <KM>"), Description("Kilometerstand (erforderlich).")] public string? Km { get; set; }
    [CommandOption("-s|--station <NAME>"), Description("Tankstelle (optional).")] public string? Station { get; set; }
    [CommandOption("--note <TEXT>"), Description("Notiz (optional).")] public string? Note { get; set; }
    [CommandOption("--voll <ja|nein>"), Description("Volltankung (Standard: ja).")] public string Voll { get; set; } = "ja";
}

/// <summary>tb entries add – erfasst einen Tankvorgang.</summary>
public sealed class EntriesAddCommand : TbCommand<EntryAddSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, EntryAddSettings s)
    {
        if (s.Vehicle == Guid.Empty) { AnsiConsole.MarkupLine("[red]Fehler:[/] [yellow]--vehicle[/] ist erforderlich."); return 1; }
        var liter = Fmt.Dec(s.Liter); var total = Fmt.Dec(s.Total); var km = Fmt.Long(s.Km);
        if (liter is not > 0) { AnsiConsole.MarkupLine("[red]Fehler:[/] Gültige [yellow]--liter[/] angeben."); return 1; }
        if (total is not > 0) { AnsiConsole.MarkupLine("[red]Fehler:[/] Gültigen [yellow]--total[/] angeben."); return 1; }
        if (km is not > 0) { AnsiConsole.MarkupLine("[red]Fehler:[/] Gültigen [yellow]--km[/] angeben."); return 1; }
        var datum = Fmt.Day(s.Date) ?? DateOnly.FromDateTime(DateTime.Now);
        var voll = s.Voll.Trim().ToLowerInvariant() is "ja" or "true" or "1" or "j";

        var e = await client.CreateTankvorgangAsync(new CreateTankvorgangRequest(
            s.Vehicle, datum, liter.Value, Fmt.Dec(s.Ppl), total.Value, km.Value,
            s.Station, s.Note, voll));
        AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Tankvorgang gespeichert: {Fmt.N(e.Liter, 2)} l · {Fmt.N(e.Gesamtpreis, 2)} € · {Fmt.N(e.PreisProLiter, 3)} €/l ([grey]{e.Id}[/])");
        return 0;
    }
}

public sealed class EntryUpdateSettings : ApiSettings
{
    [CommandArgument(0, "<id>"), Description("Tankvorgang-ID.")] public Guid Id { get; set; }
    [CommandOption("-d|--date <DATUM>")] public string? Date { get; set; }
    [CommandOption("-l|--liter <LITER>")] public string? Liter { get; set; }
    [CommandOption("-p|--ppl <PREIS>")] public string? Ppl { get; set; }
    [CommandOption("-t|--total <BETRAG>")] public string? Total { get; set; }
    [CommandOption("-k|--km <KM>")] public string? Km { get; set; }
    [CommandOption("-s|--station <NAME>")] public string? Station { get; set; }
    [CommandOption("--note <TEXT>")] public string? Note { get; set; }
    [CommandOption("--voll <ja|nein>")] public string? Voll { get; set; }
}

/// <summary>tb entries update – bearbeitet einen Tankvorgang (nur angegebene Felder ändern sich).</summary>
public sealed class EntriesUpdateCommand : TbCommand<EntryUpdateSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, EntryUpdateSettings s)
    {
        var cur = (await client.ListTankvorgaengeAsync(null, null)).FirstOrDefault(x => x.Id == s.Id);
        if (cur is null) { AnsiConsole.MarkupLine("[red]Fehler:[/] Tankvorgang nicht gefunden."); return 1; }
        var voll = s.Voll is null ? cur.Volltankung : s.Voll.Trim().ToLowerInvariant() is "ja" or "true" or "1" or "j";
        var req = new UpdateTankvorgangRequest(
            Fmt.Day(s.Date) ?? cur.Datum,
            Fmt.Dec(s.Liter) ?? cur.Liter,
            Fmt.Dec(s.Ppl) ?? cur.PreisProLiter,
            Fmt.Dec(s.Total) ?? cur.Gesamtpreis,
            Fmt.Long(s.Km) ?? cur.Kilometerstand,
            s.Station ?? cur.Tankstelle, s.Note ?? cur.Notiz, voll);
        var e = await client.UpdateTankvorgangAsync(s.Id, req);
        AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Tankvorgang aktualisiert ({Fmt.Date(e.Datum)}).");
        return 0;
    }
}

public sealed class EntryDeleteSettings : ApiSettings
{
    [CommandArgument(0, "<id>"), Description("Tankvorgang-ID.")] public Guid Id { get; set; }
}

/// <summary>tb entries delete – löscht einen Tankvorgang.</summary>
public sealed class EntriesDeleteCommand : TbCommand<EntryDeleteSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, EntryDeleteSettings s)
    {
        await client.DeleteTankvorgangAsync(s.Id);
        AnsiConsole.MarkupLine("[green]✓[/] Tankvorgang gelöscht.");
        return 0;
    }
}
