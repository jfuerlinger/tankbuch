using Shouldly;
using Tankbuch.Api.Domain;

namespace Tankbuch.UnitTests;

public class BerechnungTests
{
    private static Fuellung F(Guid id, Guid veh, long km, decimal liter, decimal ppl, bool voll, string datum = "2025-11-04")
        => new(id, veh, DateOnly.Parse(datum), liter, ppl, Math.Round(liter * ppl, 2), km, voll);

    [Fact]
    public void Verbrauch_zwischen_zwei_Volltankungen()
    {
        var veh = Guid.NewGuid();
        var id1 = Guid.NewGuid(); var id2 = Guid.NewGuid();
        var e1 = F(id1, veh, 1000, 40m, 1.6m, voll: true);
        var e2 = F(id2, veh, 1500, 30m, 1.6m, voll: true, datum: "2025-11-20");

        var map = Berechnung.VerbrauchProEintrag(new[] { e1, e2 });

        map[id1].ShouldBeNull(); // erster Eintrag → kein Vorwert
        map[id2].ShouldBe(6.00m); // 30 l / 500 km * 100
    }

    [Fact]
    public void Teilbetankung_hat_keinen_Verbrauch()
    {
        var veh = Guid.NewGuid();
        var id = Guid.NewGuid();
        var map = Berechnung.VerbrauchProEintrag(new[] { F(id, veh, 1200, 15m, 1.6m, voll: false) });
        map[id].ShouldBeNull();
    }

    [Fact]
    public void Kpis_berechnet_Kosten_Liter_und_Verbrauch()
    {
        var veh = Guid.NewGuid();
        var fuellungen = new[]
        {
            F(Guid.NewGuid(), veh, 1000, 40m, 1.6m, voll: true),
            F(Guid.NewGuid(), veh, 1500, 30m, 1.6m, voll: true, datum: "2025-11-20"),
        };
        var k = Berechnung.Kpis(new[] { new FahrzeugInfo(veh, 800) }, fuellungen);

        k.Anzahl.ShouldBe(2);
        k.Gesamtliter.ShouldBe(70m);
        k.Gesamtkosten.ShouldBe(112m);
        k.GefahreneKilometer.ShouldBe(700); // 1500 - 800
        k.Verbrauch.ShouldBe(6.00m);
        k.PreisProLiter.ShouldBe(1.6m);
    }

    [Theory]
    [InlineData(40, 64, 1.600)]      // Liter + Gesamtpreis → €/l
    public void Trio_berechnet_PreisProLiter(double liter, double total, double erwartetPpl)
    {
        var (_, ppl, _) = Berechnung.Trio((decimal)liter, null, (decimal)total);
        ppl.ShouldBe((decimal)erwartetPpl);
    }

    [Fact]
    public void Trio_berechnet_Gesamtpreis_aus_Liter_und_Ppl()
    {
        var (_, _, total) = Berechnung.Trio(40m, 1.6m, null);
        total.ShouldBe(64.00m);
    }

    [Fact]
    public void Trio_berechnet_Liter_aus_Gesamtpreis_und_Ppl()
    {
        var (liter, _, _) = Berechnung.Trio(null, 1.6m, 64m);
        liter.ShouldBe(40.00m);
    }

    [Fact]
    public void PreisProLiter_rundet_auf_drei_Nachkommastellen()
    {
        Berechnung.PreisProLiter(72.30m, 45.5m).ShouldBe(1.589m);
    }
}
