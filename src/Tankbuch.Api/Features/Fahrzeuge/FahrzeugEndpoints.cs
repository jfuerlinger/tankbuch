using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tankbuch.Api.Domain;
using Tankbuch.Api.Endpoints;
using Tankbuch.Contracts;

namespace Tankbuch.Api.Features.Fahrzeuge;

public sealed class ListFahrzeugeEndpoint : ApiEndpoint<List<FahrzeugDto>>
{
    public override void Configure()
    {
        Get("/api/fahrzeuge");
        AllowAnonymous();
        Summary(s => s.Summary = "Alle Fahrzeuge des Mandanten auflisten");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        var vehicles = await Db.Fahrzeuge.Where(f => f.TenantId == TenantId).OrderBy(f => f.Name).ToListAsync(ct);
        var entries = await Db.Tankvorgaenge.Where(t => t.TenantId == TenantId).ToListAsync(ct);
        var byVeh = entries.GroupBy(e => e.FahrzeugId).ToDictionary(g => g.Key, g => (IReadOnlyList<Tankvorgang>)g.ToList());
        var dtos = vehicles.Select(v => v.ToDto(byVeh.GetValueOrDefault(v.Id, Array.Empty<Tankvorgang>()))).ToList();
        await Send.OkAsync(dtos, ct);
    }
}

public sealed class GetFahrzeugEndpoint : ApiEndpoint<FahrzeugDto>
{
    public override void Configure()
    {
        Get("/api/fahrzeuge/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Einzelnes Fahrzeug abrufen");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        var id = Route<Guid>("id");
        var v = await Db.Fahrzeuge.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == TenantId, ct);
        if (v is null) { await Send.NotFoundAsync(ct); return; }
        var entries = await Db.Tankvorgaenge.Where(t => t.TenantId == TenantId && t.FahrzeugId == id).ToListAsync(ct);
        await Send.OkAsync(v.ToDto(entries), ct);
    }
}

public sealed class CreateFahrzeugEndpoint : ApiEndpoint<CreateFahrzeugRequest, FahrzeugDto>
{
    public override void Configure()
    {
        Post("/api/fahrzeuge");
        AllowAnonymous();
        Summary(s => s.Summary = "Fahrzeug anlegen");
    }

    public override async Task HandleAsync(CreateFahrzeugRequest req, CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        if (string.IsNullOrWhiteSpace(req.Name)) ThrowError("Bitte einen Namen angeben");

        var v = new Fahrzeug
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = req.Name.Trim(),
            Kennzeichen = (req.Kennzeichen ?? "").Trim(),
            Kraftstoffart = string.IsNullOrWhiteSpace(req.Kraftstoffart) ? "Diesel" : req.Kraftstoffart,
            Farbe = string.IsNullOrWhiteSpace(req.Farbe) ? "#3B82F6" : req.Farbe,
            AnfangsKilometer = Math.Max(0, req.AnfangsKilometer),
        };
        Db.Fahrzeuge.Add(v);
        await Db.SaveChangesAsync(ct);
        await Send.ResponseAsync(v.ToDto(Array.Empty<Tankvorgang>()), 201, ct);
    }
}

public sealed class UpdateFahrzeugEndpoint : ApiEndpoint<UpdateFahrzeugRequest, FahrzeugDto>
{
    public override void Configure()
    {
        Put("/api/fahrzeuge/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Fahrzeug bearbeiten");
    }

    public override async Task HandleAsync(UpdateFahrzeugRequest req, CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        var id = Route<Guid>("id");
        var v = await Db.Fahrzeuge.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == TenantId, ct);
        if (v is null) { await Send.NotFoundAsync(ct); return; }
        if (string.IsNullOrWhiteSpace(req.Name)) ThrowError("Bitte einen Namen angeben");

        v.Name = req.Name.Trim();
        v.Kennzeichen = (req.Kennzeichen ?? "").Trim();
        v.Kraftstoffart = string.IsNullOrWhiteSpace(req.Kraftstoffart) ? v.Kraftstoffart : req.Kraftstoffart;
        v.Farbe = string.IsNullOrWhiteSpace(req.Farbe) ? v.Farbe : req.Farbe;
        v.AnfangsKilometer = Math.Max(0, req.AnfangsKilometer);
        await Db.SaveChangesAsync(ct);

        var entries = await Db.Tankvorgaenge.Where(t => t.TenantId == TenantId && t.FahrzeugId == id).ToListAsync(ct);
        await Send.OkAsync(v.ToDto(entries), ct);
    }
}

public sealed class DeleteFahrzeugEndpoint : ApiEndpoint<EmptyResponse>
{
    public override void Configure()
    {
        Delete("/api/fahrzeuge/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Fahrzeug (und zugehörige Tankvorgänge) löschen");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        var id = Route<Guid>("id");
        var v = await Db.Fahrzeuge.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == TenantId, ct);
        if (v is null) { await Send.NotFoundAsync(ct); return; }
        Db.Fahrzeuge.Remove(v); // Cascade entfernt Tankvorgänge
        await Db.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
