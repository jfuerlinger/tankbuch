using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Tankbuch.Cli.Infrastructure;
using Tankbuch.Contracts;

namespace Tankbuch.Cli.Commands;

/// <summary>tb vehicles list – listet alle Fahrzeuge.</summary>
public sealed class VehiclesListCommand : TbCommand<ApiSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, ApiSettings s)
    {
        var list = await client.ListFahrzeugeAsync();
        if (list.Count == 0) { AnsiConsole.MarkupLine("[grey]Keine Fahrzeuge vorhanden.[/]"); return 0; }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumns("Id", "Name", "Kennzeichen", "Kraftstoff", "km-Stand", "Ø l/100km", "Kosten", "#");
        foreach (var v in list)
            table.AddRow(v.Id.ToString(), v.Name, v.Kennzeichen, v.Kraftstoffart, Fmt.N(v.AktuellerKilometerstand),
                Fmt.N(v.DurchschnittsVerbrauch, 1), Fmt.N(v.Gesamtkosten, 2) + " €", v.AnzahlTankvorgaenge.ToString());
        AnsiConsole.Write(table);
        return 0;
    }
}

public sealed class VehicleAddSettings : ApiSettings
{
    [CommandOption("-n|--name <NAME>"), Description("Anzeigename.")] public string Name { get; set; } = "";
    [CommandOption("-k|--kennzeichen <KZ>"), Description("Kennzeichen.")] public string Kennzeichen { get; set; } = "";
    [CommandOption("-f|--fuel <ART>"), Description("Kraftstoffart (Standard: Diesel).")] public string Fuel { get; set; } = "Diesel";
    [CommandOption("--color <HEX>"), Description("Farbe als Hex (Standard: #3B82F6).")] public string Color { get; set; } = "#3B82F6";
    [CommandOption("--start-km <KM>"), Description("Anfangs-Kilometerstand.")] public long StartKm { get; set; }
}

/// <summary>tb vehicles add – legt ein Fahrzeug an.</summary>
public sealed class VehiclesAddCommand : TbCommand<VehicleAddSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, VehicleAddSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.Name)) { AnsiConsole.MarkupLine("[red]Fehler:[/] [yellow]--name[/] ist erforderlich."); return 1; }
        var v = await client.CreateFahrzeugAsync(new CreateFahrzeugRequest(s.Name, s.Kennzeichen, s.Fuel, s.Color, s.StartKm));
        AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Fahrzeug angelegt: [bold]{v.Name}[/] ([grey]{v.Id}[/])");
        return 0;
    }
}

public sealed class VehicleUpdateSettings : ApiSettings
{
    [CommandArgument(0, "<id>"), Description("Fahrzeug-ID.")] public Guid Id { get; set; }
    [CommandOption("-n|--name <NAME>")] public string? Name { get; set; }
    [CommandOption("-k|--kennzeichen <KZ>")] public string? Kennzeichen { get; set; }
    [CommandOption("-f|--fuel <ART>")] public string? Fuel { get; set; }
    [CommandOption("--color <HEX>")] public string? Color { get; set; }
    [CommandOption("--start-km <KM>")] public long? StartKm { get; set; }
}

/// <summary>tb vehicles update – bearbeitet ein Fahrzeug (nur angegebene Felder ändern sich).</summary>
public sealed class VehiclesUpdateCommand : TbCommand<VehicleUpdateSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, VehicleUpdateSettings s)
    {
        var cur = (await client.ListFahrzeugeAsync()).FirstOrDefault(x => x.Id == s.Id);
        if (cur is null) { AnsiConsole.MarkupLine("[red]Fehler:[/] Fahrzeug nicht gefunden."); return 1; }
        var req = new UpdateFahrzeugRequest(
            s.Name ?? cur.Name, s.Kennzeichen ?? cur.Kennzeichen, s.Fuel ?? cur.Kraftstoffart,
            s.Color ?? cur.Farbe, s.StartKm ?? cur.AnfangsKilometer);
        var v = await client.UpdateFahrzeugAsync(s.Id, req);
        AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Fahrzeug aktualisiert: [bold]{v.Name}[/]");
        return 0;
    }
}

public sealed class VehicleDeleteSettings : ApiSettings
{
    [CommandArgument(0, "<id>"), Description("Fahrzeug-ID.")] public Guid Id { get; set; }
}

/// <summary>tb vehicles delete – löscht ein Fahrzeug samt Tankvorgängen.</summary>
public sealed class VehiclesDeleteCommand : TbCommand<VehicleDeleteSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, VehicleDeleteSettings s)
    {
        await client.DeleteFahrzeugAsync(s.Id);
        AnsiConsole.MarkupLine("[green]✓[/] Fahrzeug gelöscht.");
        return 0;
    }
}
