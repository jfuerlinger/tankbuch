using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Tankbuch.Cli.Infrastructure;

namespace Tankbuch.Cli.Commands;

public sealed class LoginSettings : ApiSettings
{
    [CommandOption("-e|--email <EMAIL>")]
    [Description("E-Mail-Adresse für die Anmeldung.")]
    public string Email { get; set; } = "";

    [CommandOption("-c|--code <CODE>")]
    [Description("6-stelliger Code (Prototyp akzeptiert jeden; Standard: 123456).")]
    public string Code { get; set; } = "123456";
}

/// <summary>tb login – fordert den OTP-Code an und meldet sich an; Token wird lokal gespeichert.</summary>
public sealed class LoginCommand : TbCommand<LoginSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, LoginSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.Email))
        {
            AnsiConsole.MarkupLine("[red]Fehler:[/] Bitte eine E-Mail-Adresse mit [yellow]--email[/] angeben.");
            return 1;
        }
        var codeResp = await client.RequestCodeAsync(s.Email);
        AnsiConsole.MarkupLineInterpolated($"[grey]{codeResp.Message}[/]");

        var verify = await client.VerifyAsync(s.Email, s.Code);
        cfg.ApiUrl = s.ResolveBaseUrl(cfg);
        cfg.Token = verify.Token;
        cfg.Email = verify.Email;
        cfg.TenantName = verify.TenantName;
        cfg.Save();

        AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Angemeldet als [bold]{verify.Email}[/] (Mandant: {verify.TenantName}).");
        return 0;
    }
}

/// <summary>tb logout – löscht das lokal gespeicherte Token.</summary>
public sealed class LogoutCommand : TbCommand<ApiSettings>
{
    protected override Task<int> RunAsync(ApiClient client, CliConfig cfg, ApiSettings s)
    {
        CliConfig.Clear();
        AnsiConsole.MarkupLine("[green]✓[/] Abgemeldet.");
        return Task.FromResult(0);
    }
}

/// <summary>tb whoami – zeigt den aktuell angemeldeten Nutzer/Mandanten.</summary>
public sealed class WhoAmICommand : TbCommand<ApiSettings>
{
    protected override async Task<int> RunAsync(ApiClient client, CliConfig cfg, ApiSettings s)
    {
        if (string.IsNullOrWhiteSpace(cfg.Token))
        {
            AnsiConsole.MarkupLine("[yellow]Nicht angemeldet.[/] Bitte [green]tb login --email <E-Mail>[/] ausführen.");
            return 1;
        }
        var me = await client.MeAsync();
        AnsiConsole.MarkupLineInterpolated($"Angemeldet als [bold]{me.Email}[/] · Mandant: [bold]{me.TenantName}[/]");
        AnsiConsole.MarkupLineInterpolated($"[grey]API:[/] {s.ResolveBaseUrl(cfg)}");
        return 0;
    }
}
