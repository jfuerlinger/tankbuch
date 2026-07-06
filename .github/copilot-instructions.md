# Copilot Instructions for tankbuch

## What this repository is

**Tankbuch** ("Fuel logbook") is a full-stack app to record and analyse refuelling
(vehicles, fill-ups, consumption, cost history), orchestrated with **.NET Aspire**.
See `README.md` for the full picture.

## Layout

- `src/Tankbuch.AppHost` — Aspire AppHost: PostgreSQL (+pgAdmin), Ollama + `llama3.2-vision`
  (photo OCR, +Open WebUI), the API and the Vite frontend. Entry point: `AppHost.cs`.
- `src/Tankbuch.Api` — .NET 10 WebApi, **FastEndpoints** (`Features/**/*Endpoints.cs`),
  EF Core + Npgsql. Domain/calc in `Domain/` (pure, unit-tested), services in `Services/`.
- `src/Tankbuch.Contracts` — DTOs shared by API **and** CLI.
- `src/Tankbuch.Cli` — .NET 10 console app (`tb`), a thin HTTP client of the API.
- `frontend/` — Vite + React 19 + TypeScript SPA. State in `src/store.ts` (zustand),
  ported design calc in `src/lib/calc.ts`, screens in `src/screens/`.
- `tests/` — Unit (xUnit+Shouldly), Integration (Testcontainers Postgres + WebApplicationFactory),
  E2E (Playwright, guarded by `TANKBUCH_E2E_URL`).
- `assets/Tankbuch - Standalone.html` — the **authoritative design template** (Claude Design
  export). The React UI must match it 1:1 (tokens, layout, screens).

## Build / run / test

```bash
aspire run                                   # start the whole stack
dotnet build Tankbuch.slnx                   # build all .NET projects
dotnet test tests/Tankbuch.UnitTests         # fast, no Docker
dotnet test tests/Tankbuch.IntegrationTests  # needs Docker
cd frontend && npm install && npm run build  # frontend
```

## Conventions (important)

- **The CLI mirrors the API and uses only the HTTP API** — every new API endpoint needs a
  matching `tb` command, and the CLI must never touch the DB/EF layer. Keep the Skill at
  `.claude/skills/tankbuch-cli/SKILL.md` in sync.
- German UI strings and domain terms (Fahrzeug, Tankvorgang, Volltankung, Liter, `€/l`).
- Locale `de-AT`: decimal comma, thousands dot, dates `TT.MM.JJJJ`. Rounding: `€/l` 3 dp,
  liters/amounts 2 dp. Consumption (l/100 km) is computed between consecutive full tanks.
- Money/liters/ppl are `decimal`; km is `long`; dates are `DateOnly`.
- Prototype auth: e-mail + 6-digit OTP, demo code `123456`, single demo tenant with seed data.
