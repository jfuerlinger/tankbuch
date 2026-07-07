# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repository is

**Tankbuch** ("Fuel logbook") is a full-stack prototype for logging and analysing vehicle
refuelling (vehicles, fill-ups, consumption, cost history — via photo OCR of the pump/odometer
or manual entry), orchestrated with **.NET Aspire**.

```
Aspire AppHost (src/Tankbuch.AppHost)
├── PostgreSQL              (Aspire-Hosting, with pgAdmin)
├── Ollama + llama3.2-vision (local vision model for photo OCR, with Open WebUI)
├── API  (src/Tankbuch.Api)  .NET 10 WebApi · FastEndpoints · EF Core/Npgsql
└── Frontend (frontend/)     Vite · React 19 · TypeScript (SPA)

src/Tankbuch.Cli            .NET 10 console app (`tb`) — thin API client (no DB access)
src/Tankbuch.Contracts      DTOs shared by API and CLI
src/Tankbuch.ServiceDefaults Aspire service defaults (telemetry, health, resilience)
tests/…                     Unit, Integration (Testcontainers) and E2E (Playwright) tests
```

`assets/Tankbuch - Standalone.html` is the **authoritative design template** (a Claude Design
export). The React UI must match it 1:1 (design tokens, layout, screens) — IBM Plex Sans, accent
amber `#F59E0B`, light/dark theme (system/light/dark), mobile-first with bottom nav and desktop
sidebar.

## Build / run / test

```bash
# Run the whole stack (Postgres, Ollama, API, frontend) via Aspire
aspire run

# Build all .NET projects
dotnet build Tankbuch.slnx

# Tests
dotnet test tests/Tankbuch.UnitTests            # domain logic (consumption, trio calc, CSV, seed) — fast, no Docker
dotnet test tests/Tankbuch.IntegrationTests      # real API against PostgreSQL (Testcontainers, needs Docker)
# E2E (Playwright) — needs a running frontend + one-time browser install:
pwsh tests/Tankbuch.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium
TANKBUCH_E2E_URL=<frontend-url> dotnet test tests/Tankbuch.E2ETests
# Without TANKBUCH_E2E_URL the E2E test is skipped (not a failure) in a standard run.

# Run a single test (any of the .NET test projects)
dotnet test tests/Tankbuch.UnitTests --filter "FullyQualifiedName~Berechnung"

# Frontend
cd frontend && npm install     # once, to install deps
cd frontend && npm run dev     # standalone Vite dev server
cd frontend && npm run build   # tsc -b && vite build
cd frontend && npm run lint    # oxlint

# CLI (tb) — build then run, or `dotnet run --project`
dotnet build src/Tankbuch.Cli
tb login --email demo@tankbuch.at --api <API-URL from the Aspire dashboard>
```

Prerequisites: **.NET 10 SDK** (`global.json` pins `10.0.100+`), **Aspire CLI** (`aspire`),
**Docker** (Postgres, Ollama, Testcontainers), **Node.js + npm**. First `aspire run` downloads
the `llama3.2-vision:11b` model (~7.9 GB), cached afterward in a data volume.

Login (prototype): any e-mail → "send code" → demo code **`123456`** (any 6-digit code is
accepted). No real e-mail is sent.

## Architecture

### Backend (`src/Tankbuch.Api`)

FastEndpoints under `/api/*` (Swagger UI active during dev), organized as vertical slices in
`Features/<Area>/*Endpoints.cs` (Auth, Csv, Fahrzeuge, Ocr, Statistik, Tankvorgaenge). All
endpoints derive from `ApiEndpoint<TRequest, TResponse>` / `ApiEndpoint<TResponse>`
(`Endpoints/ApiEndpointBase.cs`), which exposes `Db`, `Tenant`, `TenantId`, and an `AuthAsync()`
gate — there is no ASP.NET auth scheme; auth is a prototype bearer token resolved per-request by
`TenantResolutionMiddleware` into a scoped `ITenantContext`.

- `Domain/Berechnung.cs` — pure, unit-tested calculation logic ported from the design template
  (consumption, KPIs, the liter/price-per-liter/total "trio" auto-fill). Kept decoupled from EF
  entities so it's trivially testable; extend it (not the endpoints) when adding calculated
  fields.
- `Domain/Entities.cs`, `Data/TankbuchDbContext.cs` — EF Core/Npgsql model.
- `Data/DbInitializer.cs` + `Domain/DemoSeed.cs` — creates the DB and seeds demo data
  (`SeedDemoData` config, default `true`; disabled in integration tests).
- `Services/VisionService.cs` / `VisionParsing.cs` — OCR abstraction: `OllamaVisionService` when
  a `vision` connection string is configured (i.e. Ollama is wired up in the AppHost),
  `SimulatedVisionService` otherwise. OCR endpoints fall back to plausible simulated values on
  model error/timeout, so the capture flow always works even without a GPU.
- `Services/TokenService.cs`, `TenantContext.cs`, `TenantResolutionMiddleware.cs` — prototype
  auth/tenancy. All data endpoints are tenant-scoped by bearer token.

### Frontend (`frontend/`)

Vite + React 19 + TypeScript SPA (no router — screens switched in `App.tsx`).

- `src/store.ts` — zustand global state.
- `src/lib/calc.ts` — the client-side port of the same calculation logic as `Domain/Berechnung.cs`
  (keep the two in sync when changing rounding/consumption rules).
- `src/lib/api.ts` — typed fetch wrapper; all requests use **relative** `/api/*` paths.
- `src/screens/` — one component per app screen (Dashboard, Erfassen, Verlauf, Statistik,
  Fahrzeuge, Einstellungen, Mehr, Login).
- `src/modals/`, `src/ui/` — shared modal and UI/chart components.

In dev, Vite's proxy (`vite.config.ts`) forwards `/api/*` to the API URL Aspire injects via
service discovery (`API_HTTPS`/`API_HTTP` env vars, default `http://localhost:5080`). In the
Docker Compose deployment, the frontend is published as a static site container and YARP performs
the same `/api/*` forwarding (`AppHost.cs`, `PublishAsStaticWebsite`). **`VITE_API_BASE_URL` is
intentionally never set** — Vite env vars bake in at build time and wouldn't work at deploy
runtime, so the frontend always talks to same-origin relative `/api/*` paths.

### CLI (`src/Tankbuch.Cli`)

Spectre.Console.Cli app producing the `tb` binary. **Mirrors the API and talks only to the HTTP
API — never touches the DB/EF layer directly.** Every new API endpoint needs a matching `tb`
command. Base URL resolution order: `--api` flag → `TANKBUCH_API_URL` env var → saved config
(`~/.config/tankbuch/config.json`, written on `tb login`) → `http://localhost:5080`. Keep
`.claude/skills/tankbuch-cli/SKILL.md` in sync with CLI changes.

### AppHost (`src/Tankbuch.AppHost/AppHost.cs`)

Defines the whole distributed app graph: Postgres (+pgAdmin), Ollama (+Open WebUI, model
`llama3.2-vision`), the API, the Vite frontend, and (dev-only) a DevTunnel exposing the frontend
publicly for mobile testing. Also configures `AddDockerComposeEnvironment` for
`aspire publish` / `aspire deploy` (see README's Deployment section for the compose commands).

## Conventions

- **German** UI strings and domain terms throughout (Fahrzeug, Tankvorgang, Volltankung, Liter,
  `€/l`) — this is not accidental, keep new code consistent with it.
- Locale `de-AT` everywhere (frontend and CLI): decimal comma, thousands dot, dates `TT.MM.JJJJ`.
- Rounding: `€/l` → 3 decimals, liters/amounts → 2 decimals. Consumption (l/100 km) is computed
  between consecutive full tank-ups (`Volltankung`), not per fill-up.
- Types: money/liters/price-per-liter are `decimal`; odometer km is `long`; dates are `DateOnly`.
- Money/decimal calculation logic exists in exactly two ported places that must stay in sync:
  `src/Tankbuch.Api/Domain/Berechnung.cs` (backend/tests) and `frontend/src/lib/calc.ts`
  (frontend).
