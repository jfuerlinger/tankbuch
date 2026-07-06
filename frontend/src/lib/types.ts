// TS-Spiegel der Contracts-DTOs (API-JSON = camelCase)

export interface Fahrzeug {
  id: string;
  name: string;
  kennzeichen: string;
  kraftstoffart: string;
  farbe: string;
  anfangsKilometer: number;
  aktuellerKilometerstand: number;
  anzahlTankvorgaenge: number;
  durchschnittsVerbrauch: number | null;
  gesamtkosten: number;
}

export interface Tankvorgang {
  id: string;
  fahrzeugId: string;
  datum: string; // yyyy-MM-dd
  liter: number;
  preisProLiter: number;
  gesamtpreis: number;
  kilometerstand: number;
  tankstelle: string | null;
  notiz: string | null;
  volltankung: boolean;
  verbrauch: number | null;
}

export interface VerifyCodeResponse {
  token: string;
  email: string;
  tenantId: string;
  tenantName: string;
}

export interface RequestCodeResponse {
  message: string;
  demoCode: string | null;
}

export interface PumpOcrResult {
  liter: number | null;
  gesamtpreis: number | null;
  preisProLiter: number | null;
  simuliert: boolean;
  meldung: string | null;
}

export interface TachoOcrResult {
  kilometerstand: number | null;
  simuliert: boolean;
  meldung: string | null;
}

export interface VisionStatus {
  aktiv: boolean;
  modell: string | null;
  letzterAufruf: string | null;
  letzterAufrufErfolgreich: boolean | null;
  letzteMeldung: string | null;
  letzteRohantwort: string | null;
}

export interface CsvImportResult {
  importiert: number;
  uebersprungen: number;
  neueFahrzeuge: number;
  meldung: string;
}

export type Theme = 'system' | 'light' | 'dark';
export type Screen = 'dashboard' | 'erfassen' | 'verlauf' | 'statistik' | 'fahrzeuge' | 'einstellungen' | 'mehr';

export interface EntryForm {
  fahrzeugId: string | null;
  datum: string;
  liter: string;
  ppl: string;
  total: string;
  km: string;
  tankstelle: string;
  notiz: string;
  voll: boolean;
}

export interface VehicleForm {
  name: string;
  kz: string;
  fuel: string;
  color: string;
  startKm: string;
}
