namespace Tankbuch.Api.Domain;

/// <summary>Leichtgewichtiger Wert für die Verbrauchs-/Statistik-Berechnung (entkoppelt von EF-Entities, damit rein testbar).</summary>
public readonly record struct Fuellung(
    Guid Id,
    Guid FahrzeugId,
    DateOnly Datum,
    decimal Liter,
    decimal PreisProLiter,
    decimal Gesamtpreis,
    long Kilometerstand,
    bool Volltankung);

public readonly record struct FahrzeugInfo(Guid Id, long AnfangsKilometer);

public sealed record Kennzahlen(
    decimal Gesamtkosten,
    decimal Gesamtliter,
    long GefahreneKilometer,
    int Anzahl,
    decimal? Verbrauch,
    decimal? PreisProLiter,
    decimal? KostenProKm,
    decimal? KostenProMonat,
    Fuellung? Letzte);

/// <summary>
/// Portierung der Auswertungs-Logik aus der Design-Vorlage (verbMap / kpis / Trio-Berechnung).
/// Einheiten: Liter (2 NK), €/l (3 NK), Beträge (2 NK), Verbrauch l/100 km.
/// </summary>
public static class Berechnung
{
    private const MidpointRounding Half = MidpointRounding.AwayFromZero;

    /// <summary>Verbrauch (l/100 km) je Eintrag – jeweils zwischen zwei Volltankungen.
    /// Erster Eintrag bzw. fehlender Vorwert → null (kein Fehler).</summary>
    public static Dictionary<Guid, decimal?> VerbrauchProEintrag(IEnumerable<Fuellung> fuellungen)
    {
        var map = new Dictionary<Guid, decimal?>();
        foreach (var grp in fuellungen.GroupBy(f => f.FahrzeugId))
        {
            var es = grp.OrderBy(f => f.Kilometerstand).ThenBy(f => f.Datum).ToList();
            long? prevFull = null;
            decimal sum = 0;
            foreach (var e in es)
            {
                sum += e.Liter;
                if (e.Volltankung)
                {
                    map[e.Id] = (prevFull is long pf && e.Kilometerstand > pf)
                        ? Math.Round(sum / (e.Kilometerstand - pf) * 100m, 2, Half)
                        : (decimal?)null;
                    prevFull = e.Kilometerstand;
                    sum = 0;
                }
                else
                {
                    map[e.Id] = null;
                }
            }
        }
        return map;
    }

    /// <summary>Kennzahlen über die gewählten Fahrzeuge (einzeln oder gesamt).</summary>
    public static Kennzahlen Kpis(IReadOnlyCollection<FahrzeugInfo> fahrzeuge, IReadOnlyCollection<Fuellung> fuellungen)
    {
        decimal cost = 0, liters = 0, segL = 0;
        long km = 0, segKm = 0;
        int count = 0;
        Fuellung? last = null;
        DateOnly? minT = null, maxT = null;

        foreach (var v in fahrzeuge)
        {
            var es = fuellungen.Where(f => f.FahrzeugId == v.Id)
                .OrderBy(f => f.Kilometerstand).ThenBy(f => f.Datum).ToList();
            if (es.Count == 0) continue;
            km += Math.Max(0, es[^1].Kilometerstand - v.AnfangsKilometer);
            long? prevFull = null;
            decimal sum = 0;
            foreach (var e in es)
            {
                cost += e.Gesamtpreis;
                liters += e.Liter;
                count++;
                sum += e.Liter;
                if (e.Volltankung)
                {
                    if (prevFull is long pf && e.Kilometerstand > pf)
                    {
                        segL += sum;
                        segKm += e.Kilometerstand - pf;
                    }
                    prevFull = e.Kilometerstand;
                    sum = 0;
                }
                if (last is null || e.Datum > last.Value.Datum) last = e;
                if (minT is null || e.Datum < minT) minT = e.Datum;
                if (maxT is null || e.Datum > maxT) maxT = e.Datum;
            }
        }

        double months = (minT is DateOnly a && maxT is DateOnly b)
            ? Math.Max(1, (b.ToDateTime(TimeOnly.MinValue) - a.ToDateTime(TimeOnly.MinValue)).TotalDays / 30.44)
            : 1;

        return new Kennzahlen(
            Gesamtkosten: Math.Round(cost, 2, Half),
            Gesamtliter: Math.Round(liters, 2, Half),
            GefahreneKilometer: km,
            Anzahl: count,
            Verbrauch: segKm > 0 ? Math.Round(segL / segKm * 100m, 2, Half) : null,
            PreisProLiter: liters > 0 ? Math.Round(cost / liters, 3, Half) : null,
            KostenProKm: km > 0 ? Math.Round(cost / km, 4, Half) : null,
            KostenProMonat: count > 0 ? Math.Round(cost / (decimal)months, 2, Half) : null,
            Letzte: last);
    }

    /// <summary>€/l aus Gesamtpreis und Liter (3 NK).</summary>
    public static decimal PreisProLiter(decimal gesamtpreis, decimal liter)
        => liter > 0 ? Math.Round(gesamtpreis / liter, 3, Half) : 0m;

    /// <summary>Trio-Auto-Berechnung: liegen zwei der drei Werte vor, wird der dritte ergänzt.
    /// Rundung: €/l 3 NK, Liter 2 NK, Gesamtpreis 2 NK.</summary>
    public static (decimal? Liter, decimal? PreisProLiter, decimal? Gesamtpreis) Trio(
        decimal? liter, decimal? ppl, decimal? total)
    {
        if (liter is > 0 && total is > 0 && ppl is not > 0)
            ppl = Math.Round(total.Value / liter.Value, 3, Half);
        else if (liter is > 0 && ppl is > 0 && total is not > 0)
            total = Math.Round(liter.Value * ppl.Value, 2, Half);
        else if (total is > 0 && ppl is > 0 && liter is not > 0)
            liter = Math.Round(total.Value / ppl.Value, 2, Half);
        return (liter, ppl, total);
    }
}
