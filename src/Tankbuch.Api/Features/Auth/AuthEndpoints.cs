using System.Text.RegularExpressions;
using FastEndpoints;
using Tankbuch.Api.Data;
using Tankbuch.Api.Endpoints;
using Tankbuch.Api.Services;
using Tankbuch.Contracts;

namespace Tankbuch.Api.Features.Auth;

/// <summary>Schritt 1: E-Mail → „Code senden". Prototyp: keine echte E-Mail, Demo-Code 123456.</summary>
public sealed class RequestCodeEndpoint : Endpoint<RequestCodeRequest, RequestCodeResponse>
{
    public override void Configure()
    {
        Post("/api/auth/request-code");
        AllowAnonymous();
        Summary(s => s.Summary = "OTP-Code anfordern (Demo)");
    }

    public override async Task HandleAsync(RequestCodeRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || !Regex.IsMatch(req.Email, @"^\S+@\S+\.\S+$"))
            ThrowError("Bitte eine gültige E-Mail-Adresse eingeben");

        await Send.OkAsync(
            new RequestCodeResponse("Demo-Code: 123456 – es wird keine echte E-Mail versendet", "123456"), ct);
    }
}

/// <summary>Schritt 2: 6-stelligen Code prüfen → Token ausstellen. Akzeptiert jeden 6-stelligen Code.</summary>
public sealed class VerifyCodeEndpoint(TokenService tokens) : Endpoint<VerifyCodeRequest, VerifyCodeResponse>
{
    public override void Configure()
    {
        Post("/api/auth/verify");
        AllowAnonymous();
        Summary(s => s.Summary = "OTP-Code prüfen und anmelden (Demo)");
    }

    public override async Task HandleAsync(VerifyCodeRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            ThrowError("Bitte eine E-Mail-Adresse angeben");
        if (!Regex.IsMatch(req.Code ?? "", @"^\d{6}$"))
            ThrowError("Bitte den 6-stelligen Code eingeben");

        var db = Resolve<TankbuchDbContext>();
        var user = await DbInitializer.UpsertDemoUserAsync(db, req.Email, ct);
        var token = tokens.Create(user.TenantId, user.Id, user.Email);

        await Send.OkAsync(new VerifyCodeResponse(token, user.Email, user.TenantId, DbInitializer.DemoTenantName), ct);
    }
}

/// <summary>Aktueller Mandant/Nutzer (für Session-Wiederherstellung im Frontend/CLI).</summary>
public sealed class MeEndpoint : ApiEndpoint<MeResponse>
{
    public override void Configure()
    {
        Get("/api/auth/me");
        AllowAnonymous();
        Summary(s => s.Summary = "Angemeldeten Nutzer abfragen");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        await Send.OkAsync(new MeResponse(Tenant.Email, Tenant.TenantId, DbInitializer.DemoTenantName), ct);
    }
}
