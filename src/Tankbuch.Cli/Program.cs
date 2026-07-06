using Spectre.Console.Cli;
using Tankbuch.Cli.Commands;

var app = new CommandApp();
app.Configure(cfg =>
{
    cfg.SetApplicationName("tb");
    cfg.UseStrictParsing();

    cfg.AddCommand<LoginCommand>("login").WithDescription("Anmelden (E-Mail + OTP; Prototyp-Code 123456).");
    cfg.AddCommand<LogoutCommand>("logout").WithDescription("Abmelden (löscht das lokale Token).");
    cfg.AddCommand<WhoAmICommand>("whoami").WithDescription("Angemeldeten Nutzer/Mandanten anzeigen.");

    cfg.AddBranch("vehicles", b =>
    {
        b.SetDescription("Fahrzeuge verwalten.");
        b.AddCommand<VehiclesListCommand>("list").WithDescription("Fahrzeuge auflisten.");
        b.AddCommand<VehiclesAddCommand>("add").WithDescription("Fahrzeug anlegen.");
        b.AddCommand<VehiclesUpdateCommand>("update").WithDescription("Fahrzeug bearbeiten.");
        b.AddCommand<VehiclesDeleteCommand>("delete").WithDescription("Fahrzeug löschen.");
    });

    cfg.AddBranch("entries", b =>
    {
        b.SetDescription("Tankvorgänge verwalten.");
        b.AddCommand<EntriesListCommand>("list").WithDescription("Tankvorgänge auflisten.");
        b.AddCommand<EntriesAddCommand>("add").WithDescription("Tankvorgang erfassen.");
        b.AddCommand<EntriesUpdateCommand>("update").WithDescription("Tankvorgang bearbeiten.");
        b.AddCommand<EntriesDeleteCommand>("delete").WithDescription("Tankvorgang löschen.");
    });

    cfg.AddCommand<StatsCommand>("stats").WithDescription("Statistik-Kennzahlen (Fahrzeug oder gesamt).");

    cfg.AddBranch("csv", b =>
    {
        b.SetDescription("CSV-Backup exportieren/importieren.");
        b.AddCommand<CsvExportCommand>("export").WithDescription("Alle Tankvorgänge als CSV exportieren.");
        b.AddCommand<CsvImportCommand>("import").WithDescription("CSV importieren/zusammenführen.");
    });

    cfg.AddBranch("ocr", b =>
    {
        b.SetDescription("Fotos per Vision-Modell auslesen.");
        b.AddCommand<OcrPumpCommand>("pump").WithDescription("Zapfsäulen-Foto → Liter + Gesamtpreis.");
        b.AddCommand<OcrTachoCommand>("tacho").WithDescription("Tacho-Foto → Kilometerstand.");
    });
});

return await app.RunAsync(args);
