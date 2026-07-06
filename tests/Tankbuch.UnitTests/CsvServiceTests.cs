using Shouldly;
using Tankbuch.Api.Domain;
using Tankbuch.Api.Services;

namespace Tankbuch.UnitTests;

public class CsvServiceTests
{
    [Fact]
    public void Export_verwendet_deutsche_Konvention()
    {
        var fid = Guid.NewGuid();
        var veh = new Fahrzeug { Id = fid, Name = "Test Auto", Kennzeichen = "W-1 AB", Kraftstoffart = "Diesel", Farbe = "#000", AnfangsKilometer = 1000 };
        var e = new Tankvorgang { Id = Guid.NewGuid(), FahrzeugId = fid, Datum = new DateOnly(2025, 11, 4), Liter = 45.5m, PreisProLiter = 1.589m, Gesamtpreis = 72.30m, Kilometerstand = 48230, Tankstelle = "OMV Wien Nord", Notiz = "", Volltankung = true };

        var csv = CsvService.Export(new Dictionary<Guid, Fahrzeug> { [fid] = veh }, new[] { e });

        csv.ShouldStartWith("﻿"); // BOM
        csv.ShouldContain("fahrzeug;kennzeichen;datum;liter;preis_pro_liter;gesamtpreis;kilometerstand;tankstelle;volltankung;notiz");
        csv.ShouldContain("45,50");
        csv.ShouldContain("1,589");
        csv.ShouldContain("72,30");
        csv.ShouldContain("04.11.2025");
        csv.ShouldContain(";ja;");
    }

    [Fact]
    public void Export_dann_Parse_ergibt_dieselben_Werte()
    {
        var fid = Guid.NewGuid();
        var veh = new Fahrzeug { Id = fid, Name = "Kombi", Kennzeichen = "W-2 CD", Kraftstoffart = "Diesel", Farbe = "#111", AnfangsKilometer = 0 };
        var e = new Tankvorgang { Id = Guid.NewGuid(), FahrzeugId = fid, Datum = new DateOnly(2025, 12, 24), Liter = 38.12m, PreisProLiter = 1.649m, Gesamtpreis = 62.87m, Kilometerstand = 51000, Tankstelle = "Shell", Notiz = "Weihnachten", Volltankung = false };

        var csv = CsvService.Export(new Dictionary<Guid, Fahrzeug> { [fid] = veh }, new[] { e });
        var rows = CsvService.Parse(csv);

        rows.Count.ShouldBe(1);
        var r = rows[0];
        r.Fahrzeug.ShouldBe("Kombi");
        r.Datum.ShouldBe(new DateOnly(2025, 12, 24));
        r.Liter.ShouldBe(38.12m);
        r.PreisProLiter.ShouldBe(1.649m);
        r.Gesamtpreis.ShouldBe(62.87m);
        r.Kilometerstand.ShouldBe(51000);
        r.Volltankung.ShouldBeFalse();
        r.Notiz.ShouldBe("Weihnachten");
    }

    [Fact]
    public void Parse_akzeptiert_ISO_und_deutsches_Datum()
    {
        var csv = "fahrzeug;datum;liter;gesamtpreis\nAuto;2025-01-15;10,00;16,50\nAuto;15.02.2025;12,00;19,80\n";
        var rows = CsvService.Parse(csv);
        rows.Count.ShouldBe(2);
        rows[0].Datum.ShouldBe(new DateOnly(2025, 1, 15));
        rows[1].Datum.ShouldBe(new DateOnly(2025, 2, 15));
    }
}
