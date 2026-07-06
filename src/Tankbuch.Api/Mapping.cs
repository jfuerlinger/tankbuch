using Tankbuch.Api.Domain;
using Tankbuch.Contracts;

namespace Tankbuch.Api;

public static class Mapping
{
    public static Fuellung ToFuellung(this Tankvorgang t)
        => new(t.Id, t.FahrzeugId, t.Datum, t.Liter, t.PreisProLiter, t.Gesamtpreis, t.Kilometerstand, t.Volltankung);

    public static TankvorgangDto ToDto(this Tankvorgang t, decimal? verbrauch)
        => new(t.Id, t.FahrzeugId, t.Datum, t.Liter, t.PreisProLiter, t.Gesamtpreis, t.Kilometerstand,
               t.Tankstelle, t.Notiz, t.Volltankung, verbrauch);

    public static FahrzeugDto ToDto(this Fahrzeug f, IReadOnlyList<Tankvorgang> eigeneEintraege)
    {
        var fuellungen = eigeneEintraege.Select(ToFuellung).ToList();
        var kpi = Berechnung.Kpis([new FahrzeugInfo(f.Id, f.AnfangsKilometer)], fuellungen);
        long aktuell = eigeneEintraege.Count > 0 ? eigeneEintraege.Max(e => e.Kilometerstand) : f.AnfangsKilometer;
        return new FahrzeugDto(
            f.Id, f.Name, f.Kennzeichen ?? "", f.Kraftstoffart, f.Farbe,
            f.AnfangsKilometer, aktuell, kpi.Anzahl, kpi.Verbrauch, kpi.Gesamtkosten);
    }
}
