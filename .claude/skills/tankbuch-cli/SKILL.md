---
name: tankbuch-cli
description: Bedienung der Tankbuch-Kommandozeile (`tb`) zum Verwalten von Fahrzeugen, Tankvorgängen, Statistik, CSV-Backup und Foto-Erkennung (OCR). Verwenden, wenn Tankbuch vom Terminal aus automatisiert/gescriptet werden soll oder wenn Fragen zur `tb`-CLI auftauchen.
---

# Tankbuch CLI (`tb`)

`tb` ist ein dünner Client der Tankbuch-Backend-API. **Die CLI greift ausschließlich über die HTTP-API zu – niemals direkt auf die Datenbank.** Jede API-Methode hat ein entsprechendes CLI-Kommando; wird die API erweitert, muss auch die CLI erweitert werden.

## Bauen / Ausführen

```bash
# Projekt: src/Tankbuch.Cli  →  Binärname: tb
dotnet build src/Tankbuch.Cli            # erzeugt .../bin/Debug/net10.0/tb
# direkt:
./src/Tankbuch.Cli/bin/Debug/net10.0/tb <kommando> [optionen]
# oder ohne Build:
dotnet run --project src/Tankbuch.Cli -- <kommando> [optionen]
```

Im Folgenden wird `tb` als Kürzel für die Binärdatei verwendet.

## Backend-URL festlegen

Die API-Basis-URL wird in dieser Reihenfolge aufgelöst:
1. `--api <URL>` (pro Aufruf), 2. Umgebungsvariable `TANKBUCH_API_URL`, 3. gespeicherte Konfiguration (`~/.config/tankbuch/config.json`, wird beim Login gesetzt), 4. Standard `http://localhost:5080`.

Beim Start über den Aspire-AppHost (`aspire run`) vergibt Aspire dem `api`-Dienst einen Port – die konkrete URL steht im Aspire-Dashboard. Beispiel:

```bash
tb login --email demo@tankbuch.at --api http://localhost:5080
```

## Anmeldung (OTP – Prototyp)

Der Prototyp versendet keine echte E-Mail und akzeptiert jeden 6-stelligen Code (Demo: `123456`). Das Token wird lokal gespeichert.

```bash
tb login --email demo@tankbuch.at            # Code-Standard: 123456
tb login --email demo@tankbuch.at --code 123456
tb whoami                                     # angemeldeten Nutzer/Mandanten zeigen
tb logout                                     # lokales Token löschen
```

## Fahrzeuge

```bash
tb vehicles list
tb vehicles add --name "VW Polo" --kennzeichen "W-312 TE" --fuel "Super 95" --color "#2DD4BF" --start-km 8120
tb vehicles update <fahrzeug-id> --name "VW Polo GTI" --fuel "Super Plus 98"
tb vehicles delete <fahrzeug-id>
```
`list` zeigt die vollständigen Fahrzeug-IDs, die für `update`/`delete`/`entries --vehicle` benötigt werden.

## Tankvorgänge

```bash
tb entries list
tb entries list --vehicle <fahrzeug-id> --days 90
# Erfassen: --liter, --total, --km, --vehicle sind Pflicht; --ppl optional (wird sonst berechnet)
tb entries add --vehicle <fahrzeug-id> --date 05.11.2025 --liter 45,50 --total 72,30 --km 48230 \
               --station "OMV Wien Nord" --note "Steiermark" --voll ja
tb entries update <tankvorgang-id> --total 70,00 --voll nein
tb entries delete <tankvorgang-id>
```
Zahlen dürfen deutsch (`45,50`) oder mit Punkt (`45.50`) angegeben werden. Datum: `TT.MM.JJJJ` oder `JJJJ-MM-TT` (Standard: heute). `--voll ja|nein`.

## Statistik

```bash
tb stats                          # gesamt (alle Fahrzeuge)
tb stats --vehicle <fahrzeug-id>  # einzelnes Fahrzeug
```
Zeigt Gesamtliter, Gesamtkosten, Ø Verbrauch (l/100 km), Ø Preis (€/l), gefahrene km, Kosten/km, Kosten/Monat, Anzahl und den letzten Tankvorgang.

## CSV-Backup

```bash
tb csv export                     # speichert die vom Server benannte Datei im aktuellen Ordner
tb csv export --out backup.csv
tb csv import backup.csv          # importiert/merged (Duplikate werden übersprungen)
```
Format: Semikolon-getrennt, Dezimal-Komma, UTF-8 mit BOM. Spalten:
`fahrzeug;kennzeichen;datum;liter;preis_pro_liter;gesamtpreis;kilometerstand;tankstelle;volltankung;notiz`.

## Foto-Erkennung (OCR)

Nutzt das per Aspire gehostete Vision-Modell (Ollama, `llama3.2-vision`). Ist es nicht verfügbar oder nichts erkennbar, liefert die API plausible **simulierte** Werte (Hinweis wird ausgegeben).

```bash
tb ocr pump zapfsaeule.jpg        # → erkannte Liter + Gesamtpreis (+ €/l)
tb ocr tacho tacho.jpg            # → erkannter Kilometerstand
```

## Globale Optionen & Exit-Codes

- `--api <URL>` bei jedem Kommando möglich.
- `-h|--help` an jedem Punkt (`tb --help`, `tb vehicles --help`, `tb entries add --help`).
- Exit-Code `0` = Erfolg, `1` = Fehler (Meldung wird rot ausgegeben; `401` → zuerst `tb login`).

## Typischer Ablauf

```bash
tb login --email demo@tankbuch.at
VID=$(tb vehicles list)                       # ID ablesen
tb entries add --vehicle <ID> --liter 50 --total 82,45 --km 41890
tb stats --vehicle <ID>
tb csv export --out tankbuch-backup.csv
```
