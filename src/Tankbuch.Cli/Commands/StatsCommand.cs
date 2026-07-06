using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Tankbuch.Cli.Infrastructure;

namespace Tankbuch.Cli.Commands;

public sealed class StatsSettings : ApiSettings
{
    [CommandOption("-v|--vehicle <ID>"), Description("Statistik für ein Fahrzeug (sonst gesamt).")]
    public Guid? Vehicle { get; set; }
}

/// <summary>tb stats – Kennzahlen für ein Fahrzeug oder gesamt.</summary>
public sealed class StatsCommand : TbCommand<StatsSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, StatsSettings s)
    {
        var st = await client.GetStatistikAsync(s.Vehicle);
        var scope = s.Vehicle is null ? "Alle Fahrzeuge (gesamt)" : "Fahrzeug " + s.Vehicle;

        var table = new Table().Border(TableBorder.Rounded).Title($"[bold]Statistik – {scope}[/]");
        table.AddColumns("Kennzahl", "Wert");
        table.AddRow("Gesamtliter", Fmt.N(st.Gesamtliter, 0) + " l");
        table.AddRow("Gesamtkosten", Fmt.N(st.Gesamtkosten, 2) + " €");
        table.AddRow("Ø Verbrauch", Fmt.N(st.DurchschnittsVerbrauch, 1) + " l/100 km");
        table.AddRow("Ø Preis", Fmt.N(st.DurchschnittsPreis, 3) + " €/l");
        table.AddRow("Gefahrene Kilometer", Fmt.N(st.GefahreneKilometer) + " km");
        table.AddRow("Kosten pro km", st.KostenProKm is decimal ck ? Fmt.N(ck * 100, 1) + " Cent" : "—");
        table.AddRow("Kosten pro Monat", Fmt.N(st.KostenProMonat, 2) + " €");
        table.AddRow("Tankvorgänge", st.AnzahlTankvorgaenge.ToString());
        if (st.LetzterTankvorgang is { } l)
            table.AddRow("Letzter Tankvorgang", $"{Fmt.Date(l.Datum)} · {Fmt.N(l.Liter, 2)} l · {Fmt.N(l.Gesamtpreis, 2)} €");
        AnsiConsole.Write(table);
        return 0;
    }
}
