namespace Tankbuch.Api.Domain;

/// <summary>
/// Portierung der Seed-Daten-Erzeugung aus der Design-Vorlage (seeded PRNG mulberry32),
/// damit Dashboard und Statistiken sofort aussagekräftig sind: 2 Fahrzeuge mit mehreren
/// Monaten realistischer Tankvorgänge.
/// </summary>
public static class DemoSeed
{
    private sealed class Mulberry32(uint seed)
    {
        private uint _a = seed;
        public double Next()
        {
            unchecked
            {
                _a += 0x6D2B79F5u;
                uint t = _a;
                t = Imul(t ^ (t >> 15), 1u | t);
                t = (t + Imul(t ^ (t >> 7), 61u | t)) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
        private static uint Imul(uint a, uint b) => unchecked(a * b);
    }

    public static (List<Fahrzeug> Fahrzeuge, List<Tankvorgang> Tankvorgaenge) Generate(Guid tenantId)
    {
        var rnd = new Mulberry32(7);

        var v1 = new Fahrzeug { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Škoda Octavia Combi", Kennzeichen = "W-847 KX", Kraftstoffart = "Diesel", Farbe = "#3B82F6", AnfangsKilometer = 41250 };
        var v2 = new Fahrzeug { Id = Guid.NewGuid(), TenantId = tenantId, Name = "VW Polo", Kennzeichen = "W-312 TE", Kraftstoffart = "Super 95", Farbe = "#2DD4BF", AnfangsKilometer = 8120 };
        var fahrzeuge = new List<Fahrzeug> { v1, v2 };

        string[] stations = { "OMV Wien Nord", "BP Donaustadt", "Shell Simmering", "JET Erdberg", "ENI Kagran", "Avanti Floridsdorf" };
        var entries = new List<Tankvorgang>();
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = DateTime.UtcNow;

        void Gen(Fahrzeug veh, string startIso, int count, int stepDays, double priceBase,
                 double litFull, double litVar, double verbBase, double verbVar, long startKm)
        {
            var t = DateTime.SpecifyKind(DateTime.Parse(startIso).Date.AddHours(12), DateTimeKind.Utc);
            long km = startKm;
            for (int i = 0; i < count; i++)
            {
                if (t > now) break;
                bool partial = i > 1 && rnd.Next() < 0.15;
                double verb = verbBase + (rnd.Next() - 0.5) * verbVar;
                double liter = Math.Round((partial ? 12 + rnd.Next() * 8 : litFull + (rnd.Next() - 0.5) * litVar), 2, MidpointRounding.AwayFromZero);
                km += (long)Math.Round(liter / verb * 100, MidpointRounding.AwayFromZero);
                double tMs = (t - epoch).TotalMilliseconds;
                double season = Math.Sin(tMs / 86400000.0 / 58.0) * 0.055;
                double ppl = Math.Round(priceBase + season + (rnd.Next() - 0.5) * 0.05, 3, MidpointRounding.AwayFromZero);
                double total = Math.Round(liter * ppl, 2, MidpointRounding.AwayFromZero);
                entries.Add(new Tankvorgang
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FahrzeugId = veh.Id,
                    Datum = DateOnly.FromDateTime(t),
                    Liter = (decimal)liter,
                    PreisProLiter = (decimal)ppl,
                    Gesamtpreis = (decimal)total,
                    Kilometerstand = km,
                    Tankstelle = rnd.Next() < 0.85 ? stations[(int)Math.Floor(rnd.Next() * stations.Length)] : "",
                    Notiz = "",
                    Volltankung = !partial,
                });
                t = t.AddDays(stepDays + Math.Round((rnd.Next() - 0.5) * 8, MidpointRounding.AwayFromZero));
            }
        }

        Gen(v1, "2025-11-04", 16, 15, 1.649, 50, 8, 6.1, 0.7, 41250);
        Gen(v2, "2025-12-02", 11, 19, 1.529, 33, 6, 6.2, 0.6, 8120);
        entries.Sort((a, b) => a.Datum.CompareTo(b.Datum));
        return (fahrzeuge, entries);
    }
}
