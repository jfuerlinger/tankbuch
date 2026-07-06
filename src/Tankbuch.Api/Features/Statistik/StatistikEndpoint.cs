using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tankbuch.Api.Domain;
using Tankbuch.Api.Endpoints;
using Tankbuch.Contracts;

namespace Tankbuch.Api.Features.Statistik;

/// <summary>Kennzahlen pro Fahrzeug oder gesamt ("all"). Query: fahrzeugId (leer = gesamt).</summary>
public sealed class StatistikEndpoint : ApiEndpoint<StatistikDto>
{
    public override void Configure()
    {
        Get("/api/statistik");
        AllowAnonymous();
        Summary(s => s.Summary = "Statistik-Kennzahlen (einzelnes Fahrzeug oder gesamt)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        var fahrzeugId = Query<Guid?>("fahrzeugId", isRequired: false);

        var vehicles = await Db.Fahrzeuge.Where(f => f.TenantId == TenantId).ToListAsync(ct);
        var entries = await Db.Tankvorgaenge.Where(t => t.TenantId == TenantId).ToListAsync(ct);

        var selectedVehicles = fahrzeugId is Guid fid ? vehicles.Where(v => v.Id == fid).ToList() : vehicles;
        var selectedIds = selectedVehicles.Select(v => v.Id).ToHashSet();
        var selectedEntries = entries.Where(e => selectedIds.Contains(e.FahrzeugId)).ToList();

        var infos = selectedVehicles.Select(v => new FahrzeugInfo(v.Id, v.AnfangsKilometer)).ToList();
        var kpi = Berechnung.Kpis(infos, selectedEntries.Select(x => x.ToFuellung()).ToList());

        TankvorgangDto? letzter = null;
        if (kpi.Letzte is Fuellung lf)
        {
            var vm = Berechnung.VerbrauchProEintrag(entries.Where(e => e.FahrzeugId == lf.FahrzeugId).Select(x => x.ToFuellung()));
            var ent = entries.First(e => e.Id == lf.Id);
            letzter = ent.ToDto(vm.GetValueOrDefault(ent.Id));
        }

        var dto = new StatistikDto(
            Auswahl: fahrzeugId?.ToString() ?? "all",
            Gesamtliter: kpi.Gesamtliter,
            Gesamtkosten: kpi.Gesamtkosten,
            DurchschnittsVerbrauch: kpi.Verbrauch,
            DurchschnittsPreis: kpi.PreisProLiter,
            GefahreneKilometer: kpi.GefahreneKilometer,
            KostenProKm: kpi.KostenProKm,
            KostenProMonat: kpi.KostenProMonat,
            AnzahlTankvorgaenge: kpi.Anzahl,
            LetzterTankvorgang: letzter);

        await Send.OkAsync(dto, ct);
    }
}
