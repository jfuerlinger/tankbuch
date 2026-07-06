using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Tankbuch.Cli.Infrastructure;

namespace Tankbuch.Cli.Commands;

public sealed class OcrSettings : ApiSettings
{
    [CommandArgument(0, "<bild>"), Description("Pfad zum Foto (JPG/PNG).")] public string File { get; set; } = "";
}

internal static class Mime
{
    public static string FromPath(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        _ => "image/jpeg",
    };
}

/// <summary>tb ocr pump – liest Liter + Gesamtpreis aus einem Zapfsäulen-Foto (Vision-Modell).</summary>
public sealed class OcrPumpCommand : TbCommand<OcrSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, OcrSettings s)
    {
        if (!File.Exists(s.File)) { AnsiConsole.MarkupLineInterpolated($"[red]Fehler:[/] Datei nicht gefunden: {s.File}"); return 1; }
        var bytes = await File.ReadAllBytesAsync(s.File);
        var r = await client.OcrPumpAsync(bytes, Path.GetFileName(s.File), Mime.FromPath(s.File));
        AnsiConsole.MarkupLineInterpolated($"Liter: [bold]{Fmt.N(r.Liter, 2)}[/] · Gesamtpreis: [bold]{Fmt.N(r.Gesamtpreis, 2)} €[/] · €/l: [bold]{Fmt.N(r.PreisProLiter, 3)}[/]");
        if (r.Simuliert) AnsiConsole.MarkupLineInterpolated($"[yellow](simuliert – {r.Meldung ?? "kein Vision-Modell verfügbar oder nicht erkennbar"})[/]");
        return 0;
    }
}

/// <summary>tb ocr tacho – liest den Kilometerstand aus einem Tacho-Foto (Vision-Modell).</summary>
public sealed class OcrTachoCommand : TbCommand<OcrSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, OcrSettings s)
    {
        if (!File.Exists(s.File)) { AnsiConsole.MarkupLineInterpolated($"[red]Fehler:[/] Datei nicht gefunden: {s.File}"); return 1; }
        var bytes = await File.ReadAllBytesAsync(s.File);
        var r = await client.OcrTachoAsync(bytes, Path.GetFileName(s.File), Mime.FromPath(s.File));
        AnsiConsole.MarkupLineInterpolated($"Kilometerstand: [bold]{(r.Kilometerstand is long km ? Fmt.N(km) : "—")} km[/]");
        if (r.Simuliert) AnsiConsole.MarkupLineInterpolated($"[yellow](simuliert – {r.Meldung ?? "kein Vision-Modell verfügbar oder nicht erkennbar"})[/]");
        return 0;
    }
}

/// <summary>tb ocr status – Diagnose der Bilderkennung (aktives Modell, letzter Aufruf, letzte Meldung/Rohantwort).</summary>
public sealed class OcrStatusCommand : TbCommand<ApiSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, ApiSettings s)
    {
        var st = await client.OcrStatusAsync();
        var modellSuffix = st.Modell is not null ? $" ([bold]{st.Modell}[/])" : "";
        AnsiConsole.MarkupLine($"Vision-Modell aktiv: [bold]{(st.Aktiv ? "ja" : "nein")}[/]{modellSuffix}");
        var erfolgSuffix = st.LetzterAufrufErfolgreich is bool ok ? $" · {(ok ? "[green]erfolgreich[/]" : "[red]fehlgeschlagen[/]")}" : "";
        AnsiConsole.MarkupLine($"Letzter Aufruf: [bold]{(st.LetzterAufruf is { } t ? t.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") : "—")}[/]{erfolgSuffix}");
        if (!string.IsNullOrWhiteSpace(st.LetzteMeldung)) AnsiConsole.MarkupLineInterpolated($"Meldung: [yellow]{st.LetzteMeldung}[/]");
        if (!string.IsNullOrWhiteSpace(st.LetzteRohantwort)) AnsiConsole.MarkupLineInterpolated($"Rohantwort des Modells: [grey]{st.LetzteRohantwort}[/]");
        return 0;
    }
}
