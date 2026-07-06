namespace Tankbuch.Contracts;

// ---------- Auth ----------
public record RequestCodeRequest(string Email);
public record RequestCodeResponse(string Message, string? DemoCode);
public record VerifyCodeRequest(string Email, string Code);
public record VerifyCodeResponse(string Token, string Email, Guid TenantId, string TenantName);
public record MeResponse(string Email, Guid TenantId, string TenantName);

// ---------- Fahrzeuge ----------
public record FahrzeugDto(
    Guid Id,
    string Name,
    string Kennzeichen,
    string Kraftstoffart,
    string Farbe,
    long AnfangsKilometer,
    long AktuellerKilometerstand,
    int AnzahlTankvorgaenge,
    decimal? DurchschnittsVerbrauch,
    decimal Gesamtkosten);

public record CreateFahrzeugRequest(
    string Name,
    string? Kennzeichen,
    string Kraftstoffart,
    string Farbe,
    long AnfangsKilometer);

public record UpdateFahrzeugRequest(
    string Name,
    string? Kennzeichen,
    string Kraftstoffart,
    string Farbe,
    long AnfangsKilometer);

// ---------- Tankvorgänge ----------
public record TankvorgangDto(
    Guid Id,
    Guid FahrzeugId,
    DateOnly Datum,
    decimal Liter,
    decimal PreisProLiter,
    decimal Gesamtpreis,
    long Kilometerstand,
    string? Tankstelle,
    string? Notiz,
    bool Volltankung,
    decimal? Verbrauch);

public record CreateTankvorgangRequest(
    Guid FahrzeugId,
    DateOnly Datum,
    decimal Liter,
    decimal? PreisProLiter,
    decimal Gesamtpreis,
    long Kilometerstand,
    string? Tankstelle,
    string? Notiz,
    bool Volltankung);

public record UpdateTankvorgangRequest(
    DateOnly Datum,
    decimal Liter,
    decimal? PreisProLiter,
    decimal Gesamtpreis,
    long Kilometerstand,
    string? Tankstelle,
    string? Notiz,
    bool Volltankung);

// ---------- Statistik ----------
public record StatistikDto(
    string Auswahl,
    decimal Gesamtliter,
    decimal Gesamtkosten,
    decimal? DurchschnittsVerbrauch,
    decimal? DurchschnittsPreis,
    long GefahreneKilometer,
    decimal? KostenProKm,
    decimal? KostenProMonat,
    int AnzahlTankvorgaenge,
    TankvorgangDto? LetzterTankvorgang);

// ---------- OCR ----------
// Meldung: nur gesetzt wenn Simuliert = true – erklärt, warum auf Zufallswerte zurückgefallen wurde
// (z. B. Zeitüberschreitung, kein JSON in der Modell-Antwort, unplausible Werte).
public record PumpOcrResult(decimal? Liter, decimal? Gesamtpreis, decimal? PreisProLiter, bool Simuliert, string? Meldung = null);
public record TachoOcrResult(long? Kilometerstand, bool Simuliert, string? Meldung = null);

/// <summary>Diagnose-Status der Bilderkennung – für Fehlersuche ohne eigenes Foto (siehe GET /api/ocr/status).</summary>
public record VisionStatus(
    bool Aktiv,
    string? Modell,
    DateTimeOffset? LetzterAufruf,
    bool? LetzterAufrufErfolgreich,
    string? LetzteMeldung,
    string? LetzteRohantwort);

// ---------- CSV ----------
public record CsvImportResult(int Importiert, int Uebersprungen, int NeueFahrzeuge, string Meldung);
