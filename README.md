# Tankbuch

**Tankbuch** – das digitale Fahrtenbuch fürs Tanken. Fahrzeuge und Tankvorgänge erfassen (per Foto der Zapfsäule/​des Tachos oder manuell), Verbrauch und Kosten pro Fahrzeug und gesamt auswerten, als CSV sichern/wiederherstellen.

Voll funktionsfähiger Full-Stack-Prototyp, orchestriert per **.NET Aspire**.

## Architektur

```
Aspire AppHost (src/Tankbuch.AppHost)
├── PostgreSQL            (Aspire-Hosting, mit pgAdmin)
├── Ollama + llama3.2-vision   (lokales Vision-Modell für die Foto-OCR, mit Open WebUI)
├── API  (src/Tankbuch.Api)     .NET 10 WebApi · FastEndpoints · EF Core/Npgsql
└── Frontend (frontend/)        Vite · React 19 · TypeScript (SPA)

src/Tankbuch.Cli         .NET 10 Konsolen-App (`tb`) – dünner API-Client (kein DB-Zugriff)
src/Tankbuch.Contracts   gemeinsame DTOs (API + CLI)
src/Tankbuch.ServiceDefaults  Aspire Service Defaults (Telemetry, Health, Resilience)
tests/…                  Unit-, Integrations- (Testcontainers) und E2E-Tests (Playwright)
```

Design: Die App bildet exakt die hinterlegte Design-Vorlage (`assets/Tankbuch - Standalone.html`) nach – IBM Plex Sans, Akzent Amber `#F59E0B`, helles/dunkles Theme (System/Hell/Dunkel), mobile-first mit Bottom-Nav und Desktop-Sidebar.

## Voraussetzungen

- **.NET 10 SDK** (`global.json` pinnt 10.0.100+)
- **Aspire CLI** (`aspire`), **Docker** (für Postgres, Ollama & Testcontainers)
- **Node.js + npm** (Frontend; Abhängigkeiten via `frontend/`)
- Beim ersten Start lädt Ollama das Modell **`llama3.2-vision:11b` (~7,9 GB)** herunter (per Daten-Volume danach dauerhaft gecacht).

## Starten

```bash
cd frontend && npm install && cd ..     # einmalig: Frontend-Abhängigkeiten
aspire run                              # startet Postgres, Ollama, API und Frontend
```

Das Aspire-Dashboard zeigt die URLs aller Dienste (Frontend, API/Swagger, pgAdmin, Open WebUI) und die Ports. Die API seedet beim Start automatisch Demo-Daten (2 Fahrzeuge mit mehreren Monaten Tankvorgängen).

**Anmeldung (Prototyp):** beliebige E-Mail → „Code senden“ → Demo-Code **`123456`** (jeder 6-stellige Code wird akzeptiert). Es wird keine echte E-Mail versendet.

> Hinweis macOS: Ollama läuft im Container ohne GPU-Durchgriff (CPU-Inferenz von `llama3.2-vision` ist langsam). Die OCR-Endpunkte fallen bei Fehler/Timeout automatisch auf plausible **simulierte** Werte zurück, sodass der Erfassungs-Flow immer funktioniert. Das Modell ist konfigurierbar (`AppHost.cs`).

## CLI (`tb`)

Konsolen-App, die **jede** API-Methode spiegelt und ausschließlich über die HTTP-API zugreift:

```bash
dotnet build src/Tankbuch.Cli
tb login --email demo@tankbuch.at --api <API-URL aus dem Aspire-Dashboard>
tb vehicles list
tb entries add --vehicle <id> --liter 45,50 --total 72,30 --km 48230
tb stats
tb csv export --out backup.csv
tb ocr pump zapfsaeule.jpg
```

Vollständige Referenz: `.claude/skills/tankbuch-cli/SKILL.md` (bzw. `tb --help`).

## Tests

```bash
dotnet test tests/Tankbuch.UnitTests            # Domänenlogik: Verbrauch, Trio, CSV, Seed
dotnet test tests/Tankbuch.IntegrationTests     # echte API gegen PostgreSQL (Testcontainers, Docker nötig)
# E2E (Playwright) – benötigt laufendes Frontend + einmalig Browser-Install:
pwsh tests/Tankbuch.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium
TANKBUCH_E2E_URL=<Frontend-URL> dotnet test tests/Tankbuch.E2ETests
```

Ohne `TANKBUCH_E2E_URL` wird der E2E-Test übersprungen (kein Fehlschlag im Standardlauf).

## API-Überblick

FastEndpoints unter `/api/*` (Swagger-UI beim Dev-Start aktiv):

| Bereich | Endpunkte |
|---|---|
| Auth | `POST /api/auth/request-code`, `POST /api/auth/verify`, `GET /api/auth/me` |
| Fahrzeuge | `GET/POST /api/fahrzeuge`, `GET/PUT/DELETE /api/fahrzeuge/{id}` |
| Tankvorgänge | `GET/POST /api/tankvorgaenge`, `GET/PUT/DELETE /api/tankvorgaenge/{id}` |
| Statistik | `GET /api/statistik?fahrzeugId=` |
| CSV | `GET /api/csv/export`, `POST /api/csv/import` |
| OCR | `POST /api/ocr/pump`, `POST /api/ocr/tacho` |

Alle Datenendpunkte sind mandantengetrennt (Bearer-Token). Berechnung: €/l auf 3, Liter/Beträge auf 2 Nachkommastellen; Verbrauch (l/100 km) zwischen zwei Volltankungen. Formatierung im Frontend/CLI durchgängig `de-AT` (Dezimal-Komma, `TT.MM.JJJJ`).
