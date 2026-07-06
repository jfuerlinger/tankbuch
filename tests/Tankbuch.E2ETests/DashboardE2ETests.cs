using Microsoft.Playwright;

namespace Tankbuch.E2ETests;

/// <summary>
/// End-to-End-Test mit Playwright gegen das laufende Frontend.
/// Läuft nur, wenn die Umgebungsvariable TANKBUCH_E2E_URL gesetzt ist (z. B. die vom
/// Aspire-AppHost vergebene Frontend-URL). Voraussetzung einmalig:
///   dotnet build &amp;&amp; pwsh tests/Tankbuch.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium
/// Ausführen:
///   TANKBUCH_E2E_URL=http://localhost:5173 dotnet test tests/Tankbuch.E2ETests
/// </summary>
public class DashboardE2ETests : IAsyncLifetime
{
    private static readonly string? BaseUrl = Environment.GetEnvironmentVariable("TANKBUCH_E2E_URL");
    private IPlaywright _pw = null!;
    private IBrowser _browser = null!;

    public async Task InitializeAsync()
    {
        if (BaseUrl is null) return; // ohne URL wird der Test übersprungen (No-Op)
        _pw = await Playwright.CreateAsync();
        _browser = await _pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _pw?.Dispose();
    }

    [Fact]
    public async Task Login_per_Demo_Code_zeigt_Dashboard_mit_Kennzahlen()
    {
        if (BaseUrl is null)
        {
            // TANKBUCH_E2E_URL nicht gesetzt → E2E im Standardlauf überspringen.
            return;
        }

        var page = await _browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new() { Width = 1280, Height = 900 } });
        await page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Schritt 1: E-Mail eingeben und Code anfordern
        await page.GetByPlaceholder("name@beispiel.at").FillAsync("e2e@tankbuch.at");
        await page.GetByRole(AriaRole.Button, new() { Name = "Code senden" }).ClickAsync();

        // Schritt 2: 6-stelligen Demo-Code eingeben (jede beliebige 6-stellige Zahl wird akzeptiert)
        var fields = page.GetByLabel("Code-Ziffer");
        await Assertions.Expect(fields).ToHaveCountAsync(6);
        var code = "123456";
        for (var i = 0; i < 6; i++)
            await fields.Nth(i).FillAsync(code[i].ToString());

        // Erwartung: Dashboard erscheint mit Kennzahlen (Seed-Daten)
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Übersicht" })).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Assertions.Expect(page.GetByText("Ø Verbrauch").First).ToBeVisibleAsync();
    }
}
