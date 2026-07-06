using System.Text.Json;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Tankbuch.Cli.Infrastructure;

/// <summary>Persistenter CLI-Zustand: API-URL + Session-Token unter ~/.config/tankbuch/config.json.</summary>
public sealed class CliConfig
{
    public string? ApiUrl { get; set; }
    public string? Token { get; set; }
    public string? Email { get; set; }
    public string? TenantName { get; set; }

    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };
    private static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "tankbuch");
    private static string PathName => Path.Combine(Dir, "config.json");

    public static CliConfig Load()
    {
        try
        {
            if (System.IO.File.Exists(PathName))
                return JsonSerializer.Deserialize<CliConfig>(System.IO.File.ReadAllText(PathName)) ?? new CliConfig();
        }
        catch { /* beschädigte Datei → Standardwerte */ }
        return new CliConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(Dir);
        System.IO.File.WriteAllText(PathName, JsonSerializer.Serialize(this, Opts));
    }

    public static void Clear()
    {
        try { if (System.IO.File.Exists(PathName)) System.IO.File.Delete(PathName); } catch { /* egal */ }
    }
}

/// <summary>Basis-Optionen für alle Befehle (globale --api-Überschreibung).</summary>
public class ApiSettings : CommandSettings
{
    [CommandOption("--api <URL>")]
    [Description("Basis-URL der Tankbuch-API (überschreibt Konfiguration/Umgebung).")]
    public string? Api { get; set; }

    public string ResolveBaseUrl(CliConfig cfg) =>
        Api
        ?? Environment.GetEnvironmentVariable("TANKBUCH_API_URL")
        ?? cfg.ApiUrl
        ?? "http://localhost:5080";
}

public static class Cli
{
    public static ApiClient CreateClient(ApiSettings settings, CliConfig cfg) =>
        new(settings.ResolveBaseUrl(cfg), cfg.Token);
}
