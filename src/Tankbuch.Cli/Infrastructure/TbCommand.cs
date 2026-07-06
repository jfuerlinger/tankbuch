using System.Globalization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Tankbuch.Cli.Infrastructure;

/// <summary>Basis-Command: lädt Konfiguration, baut den API-Client und behandelt Fehler einheitlich.</summary>
public abstract class TbCommand<TSettings> : AsyncCommand<TSettings> where TSettings : ApiSettings
{
    protected override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellation)
    {
        var cfg = CliConfig.Load();
        var client = Cli.CreateClient(settings, cfg);
        try
        {
            return await RunAsync(client, cfg, settings);
        }
        catch (ApiException ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Fehler:[/] {ex.Message}");
            return 1;
        }
        catch (HttpRequestException)
        {
            AnsiConsole.MarkupLine("[red]Fehler:[/] Keine Verbindung zur API. Läuft der AppHost? Basis-URL ggf. mit [yellow]--api[/] setzen.");
            return 1;
        }
    }

    protected abstract Task<int> RunAsync(ApiClient client, CliConfig cfg, TSettings settings);
}

public static class Fmt
{
    public static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-AT");

    public static string N(decimal? v, int d) => v is decimal x ? x.ToString("N" + d, De) : "—";
    public static string N(long v) => v.ToString("N0", De);
    public static string Date(DateOnly d) => d.ToString("dd.MM.yyyy", De);
    public static string ShortId(Guid id) => id.ToString()[..8];

    // Toleranter Parser: akzeptiert "45,50" (de) und "45.50" (invariant).
    public static decimal? Dec(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        if (t.Contains(',')) t = t.Replace(".", "").Replace(',', '.');
        return decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static long? Long(string? s) => Dec(s) is decimal x ? (long)Math.Round(x) : null;

    public static DateOnly? Day(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        if (DateOnly.TryParseExact(t, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso)) return iso;
        if (DateOnly.TryParseExact(t, "dd.MM.yyyy", De, DateTimeStyles.None, out var de)) return de;
        return DateOnly.TryParse(t, De, DateTimeStyles.None, out var any) ? any : null;
    }
}
