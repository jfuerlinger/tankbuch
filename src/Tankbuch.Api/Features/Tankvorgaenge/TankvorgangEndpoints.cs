using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tankbuch.Api.Domain;
using Tankbuch.Api.Endpoints;
using Tankbuch.Contracts;

namespace Tankbuch.Api.Features.Tankvorgaenge;

public sealed class ListTankvorgaengeEndpoint : ApiEndpoint<List<TankvorgangDto>>
{
    public override void Configure()
    {
        Get("/api/tankvorgaenge");
        AllowAnonymous();
        Summary(s => s.Summary = "Tankvorgänge auflisten (optional gefiltert nach Fahrzeug und Zeitraum in Tagen)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        var fahrzeugId = Query<Guid?>("fahrzeugId", isRequired: false);
        var tage = Query<int?>("tage", isRequired: false);

        var all = await Db.Tankvorgaenge.Where(t => t.TenantId == TenantId).ToListAsync(ct);
        // Verbrauch immer über die vollständige Fahrzeug-Historie berechnen, erst danach filtern.
        var vm = Berechnung.VerbrauchProEintrag(all.Select(x => x.ToFuellung()));

        IEnumerable<Tankvorgang> q = all;
        if (fahrzeugId is Guid fid) q = q.Where(t => t.FahrzeugId == fid);
        if (tage is int d && d > 0)
        {
            var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-d));
            q = q.Where(t => t.Datum >= cutoff);
        }

        var list = q
            .OrderByDescending(t => t.Datum).ThenByDescending(t => t.Kilometerstand)
            .Select(t => t.ToDto(vm.GetValueOrDefault(t.Id)))
            .ToList();
        await Send.OkAsync(list, ct);
    }
}

public sealed class GetTankvorgangEndpoint : ApiEndpoint<TankvorgangDto>
{
    public override void Configure()
    {
        Get("/api/tankvorgaenge/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Einzelnen Tankvorgang abrufen");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        var id = Route<Guid>("id");
        var e = await Db.Tankvorgaenge.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == TenantId, ct);
        if (e is null) { await Send.NotFoundAsync(ct); return; }
        var vm = await VerbrauchFuer(e.FahrzeugId, ct);
        await Send.OkAsync(e.ToDto(vm.GetValueOrDefault(e.Id)), ct);
    }

    private async Task<Dictionary<Guid, decimal?>> VerbrauchFuer(Guid fahrzeugId, CancellationToken ct)
    {
        var es = await Db.Tankvorgaenge.Where(t => t.TenantId == TenantId && t.FahrzeugId == fahrzeugId).ToListAsync(ct);
        return Berechnung.VerbrauchProEintrag(es.Select(x => x.ToFuellung()));
    }
}

public sealed class CreateTankvorgangEndpoint : ApiEndpoint<CreateTankvorgangRequest, TankvorgangDto>
{
    public override void Configure()
    {
        Post("/api/tankvorgaenge");
        AllowAnonymous();
        Summary(s => s.Summary = "Tankvorgang erfassen");
    }

    public override async Task HandleAsync(CreateTankvorgangRequest req, CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        var veh = await Db.Fahrzeuge.FirstOrDefaultAsync(f => f.Id == req.FahrzeugId && f.TenantId == TenantId, ct);
        if (veh is null) ThrowError("Bitte ein gültiges Fahrzeug wählen");
        if (req.Liter <= 0) ThrowError("Bitte die getankten Liter angeben");
        if (req.Gesamtpreis <= 0) ThrowError("Bitte den Gesamtpreis angeben");
        if (req.Kilometerstand <= 0) ThrowError("Bitte den Kilometerstand angeben");

        var datum = req.Datum == default ? DateOnly.FromDateTime(DateTime.Now) : req.Datum;
        var ppl = req.PreisProLiter is > 0 ? req.PreisProLiter.Value : Berechnung.PreisProLiter(req.Gesamtpreis, req.Liter);

        var e = new Tankvorgang
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            FahrzeugId = req.FahrzeugId,
            Datum = datum,
            Liter = Math.Round(req.Liter, 2, MidpointRounding.AwayFromZero),
            PreisProLiter = ppl,
            Gesamtpreis = Math.Round(req.Gesamtpreis, 2, MidpointRounding.AwayFromZero),
            Kilometerstand = req.Kilometerstand,
            Tankstelle = req.Tankstelle?.Trim() ?? "",
            Notiz = req.Notiz?.Trim() ?? "",
            Volltankung = req.Volltankung,
        };
        Db.Tankvorgaenge.Add(e);
        await Db.SaveChangesAsync(ct);

        var es = await Db.Tankvorgaenge.Where(t => t.TenantId == TenantId && t.FahrzeugId == req.FahrzeugId).ToListAsync(ct);
        var vm = Berechnung.VerbrauchProEintrag(es.Select(x => x.ToFuellung()));
        await Send.ResponseAsync(e.ToDto(vm.GetValueOrDefault(e.Id)), 201, ct);
    }
}

public sealed class UpdateTankvorgangEndpoint : ApiEndpoint<UpdateTankvorgangRequest, TankvorgangDto>
{
    public override void Configure()
    {
        Put("/api/tankvorgaenge/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Tankvorgang bearbeiten");
    }

    public override async Task HandleAsync(UpdateTankvorgangRequest req, CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        var id = Route<Guid>("id");
        var e = await Db.Tankvorgaenge.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == TenantId, ct);
        if (e is null) { await Send.NotFoundAsync(ct); return; }
        if (req.Liter <= 0) ThrowError("Bitte die getankten Liter angeben");
        if (req.Gesamtpreis <= 0) ThrowError("Bitte den Gesamtpreis angeben");
        if (req.Kilometerstand <= 0) ThrowError("Bitte den Kilometerstand angeben");

        var ppl = req.PreisProLiter is > 0 ? req.PreisProLiter.Value : Berechnung.PreisProLiter(req.Gesamtpreis, req.Liter);
        e.Datum = req.Datum == default ? e.Datum : req.Datum;
        e.Liter = Math.Round(req.Liter, 2, MidpointRounding.AwayFromZero);
        e.PreisProLiter = ppl;
        e.Gesamtpreis = Math.Round(req.Gesamtpreis, 2, MidpointRounding.AwayFromZero);
        e.Kilometerstand = req.Kilometerstand;
        e.Tankstelle = req.Tankstelle?.Trim() ?? "";
        e.Notiz = req.Notiz?.Trim() ?? "";
        e.Volltankung = req.Volltankung;
        await Db.SaveChangesAsync(ct);

        var es = await Db.Tankvorgaenge.Where(t => t.TenantId == TenantId && t.FahrzeugId == e.FahrzeugId).ToListAsync(ct);
        var vm = Berechnung.VerbrauchProEintrag(es.Select(x => x.ToFuellung()));
        await Send.OkAsync(e.ToDto(vm.GetValueOrDefault(e.Id)), ct);
    }
}

public sealed class DeleteTankvorgangEndpoint : ApiEndpoint<EmptyResponse>
{
    public override void Configure()
    {
        Delete("/api/tankvorgaenge/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Tankvorgang löschen");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        var id = Route<Guid>("id");
        var e = await Db.Tankvorgaenge.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == TenantId, ct);
        if (e is null) { await Send.NotFoundAsync(ct); return; }
        Db.Tankvorgaenge.Remove(e);
        await Db.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
