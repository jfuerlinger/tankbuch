using Shouldly;
using Tankbuch.Api.Domain;

namespace Tankbuch.UnitTests;

public class DemoSeedTests
{
    [Fact]
    public void Generate_liefert_zwei_Fahrzeuge_mit_Tankvorgaengen()
    {
        var tenant = Guid.NewGuid();
        var (fahrzeuge, tankvorgaenge) = DemoSeed.Generate(tenant);

        fahrzeuge.Count.ShouldBe(2);
        fahrzeuge.ShouldAllBe(f => f.TenantId == tenant);
        fahrzeuge.ShouldContain(f => f.Kraftstoffart == "Diesel");
        tankvorgaenge.Count.ShouldBeGreaterThan(10);
        tankvorgaenge.ShouldAllBe(t => t.Liter > 0 && t.Gesamtpreis > 0 && t.Kilometerstand > 0);
        tankvorgaenge.ShouldAllBe(t => t.TenantId == tenant);
    }

    [Fact]
    public void Generate_ist_deterministisch()
    {
        var t = Guid.NewGuid();
        var a = DemoSeed.Generate(t).Tankvorgaenge.Select(x => (x.Datum, x.Liter, x.PreisProLiter, x.Kilometerstand)).ToList();
        var b = DemoSeed.Generate(t).Tankvorgaenge.Select(x => (x.Datum, x.Liter, x.PreisProLiter, x.Kilometerstand)).ToList();
        a.ShouldBe(b); // seeded PRNG → reproduzierbar
    }
}
