using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Tankbuch.Cli.Infrastructure;

namespace Tankbuch.Cli.Commands;

public sealed class CsvExportSettings : ApiSettings
{
    [CommandOption("-o|--out <DATEI>"), Description("Zieldatei (Standard: vom Server vorgeschlagener Name im aktuellen Ordner).")]
    public string? Out { get; set; }
}

/// <summary>tb csv export – lädt alle Tankvorgänge als CSV herunter.</summary>
public sealed class CsvExportCommand : TbCommand<CsvExportSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, CsvExportSettings s)
    {
        var (fileName, content) = await client.ExportCsvAsync();
        var path = Path.GetFullPath(s.Out ?? fileName);
        await File.WriteAllBytesAsync(path, content);
        AnsiConsole.MarkupLineInterpolated($"[green]✓[/] {content.Length} Bytes exportiert nach [bold]{path}[/]");
        return 0;
    }
}

public sealed class CsvImportSettings : ApiSettings
{
    [CommandArgument(0, "<datei>"), Description("Pfad zur CSV-Datei.")] public string File { get; set; } = "";
}

/// <summary>tb csv import – importiert/merged eine CSV-Datei.</summary>
public sealed class CsvImportCommand : TbCommand<CsvImportSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, CsvImportSettings s)
    {
        if (!System.IO.File.Exists(s.File)) { AnsiConsole.MarkupLineInterpolated($"[red]Fehler:[/] Datei nicht gefunden: {s.File}"); return 1; }
        var bytes = await System.IO.File.ReadAllBytesAsync(s.File);
        var r = await client.ImportCsvAsync(bytes, Path.GetFileName(s.File));
        AnsiConsole.MarkupLineInterpolated($"[green]✓[/] {r.Meldung}");
        return 0;
    }
}
