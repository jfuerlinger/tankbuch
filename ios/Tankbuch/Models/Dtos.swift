import Foundation

// Codable-Spiegel der Contracts-DTOs (API-JSON = camelCase, DateOnly = "yyyy-MM-dd").

struct Fahrzeug: Codable, Identifiable, Hashable {
    let id: UUID
    var name: String
    var kennzeichen: String
    var kraftstoffart: String
    var farbe: String
    var anfangsKilometer: Int64
    var aktuellerKilometerstand: Int64
    var anzahlTankvorgaenge: Int
    var durchschnittsVerbrauch: Double?
    var gesamtkosten: Double
}

struct Tankvorgang: Codable, Identifiable, Hashable {
    let id: UUID
    var fahrzeugId: UUID
    var datum: String // yyyy-MM-dd – String wie im Web, damit Sortierung/Vergleiche identisch bleiben
    var liter: Double
    var preisProLiter: Double
    var gesamtpreis: Double
    var kilometerstand: Int64
    var tankstelle: String?
    var notiz: String?
    var volltankung: Bool
    var verbrauch: Double?
}

struct RequestCodeResponse: Codable {
    let message: String
    let demoCode: String?
}

struct VerifyCodeResponse: Codable {
    let token: String
    let email: String
    let tenantId: UUID
    let tenantName: String
}

struct MeResponse: Codable {
    let email: String
    let tenantId: UUID
    let tenantName: String
}

struct PumpOcrResult: Codable {
    let liter: Double?
    let gesamtpreis: Double?
    let preisProLiter: Double?
    let simuliert: Bool
    let meldung: String?
}

struct TachoOcrResult: Codable {
    let kilometerstand: Int64?
    let simuliert: Bool
    let meldung: String?
}

struct CsvImportResult: Codable {
    let importiert: Int
    let uebersprungen: Int
    let neueFahrzeuge: Int
    let meldung: String
}

// Request-Bodies (synthetisiertes Encodable lässt nil-Optionals weg)

struct FahrzeugBody: Encodable {
    var name: String
    var kennzeichen: String
    var kraftstoffart: String
    var farbe: String
    var anfangsKilometer: Int64
}

struct TankvorgangBody: Encodable {
    var fahrzeugId: UUID? // nur beim Anlegen
    var datum: String
    var liter: Double
    var preisProLiter: Double?
    var gesamtpreis: Double
    var kilometerstand: Int64
    var tankstelle: String
    var notiz: String
    var volltankung: Bool
}
