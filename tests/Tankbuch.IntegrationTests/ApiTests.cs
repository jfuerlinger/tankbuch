using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Shouldly;
using Tankbuch.Contracts;

namespace Tankbuch.IntegrationTests;

[Collection("api")]
public class ApiTests(TankbuchApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private async Task<HttpClient> AuthedAsync(string email = "test@tankbuch.at")
    {
        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/verify", new VerifyCodeRequest(email, "123456"), Json);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var v = await resp.Content.ReadFromJsonAsync<VerifyCodeResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", v!.Token);
        return client;
    }

    [Fact]
    public async Task Ohne_Token_liefern_Datenendpunkte_401()
    {
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/fahrzeuge");
        resp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestCode_liefert_Demo_Code()
    {
        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/request-code", new RequestCodeRequest("a@b.at"), Json);
        var body = await resp.Content.ReadFromJsonAsync<RequestCodeResponse>(Json);
        body!.DemoCode.ShouldBe("123456");
    }

    [Fact]
    public async Task Fahrzeug_anlegen_und_auflisten()
    {
        var client = await AuthedAsync("veh@tankbuch.at");
        var create = await client.PostAsJsonAsync("/api/fahrzeuge",
            new CreateFahrzeugRequest("Polo", "W-1 AB", "Super 95", "#2DD4BF", 8000), Json);
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var v = await create.Content.ReadFromJsonAsync<FahrzeugDto>(Json);
        v!.Name.ShouldBe("Polo");

        var list = await client.GetFromJsonAsync<List<FahrzeugDto>>("/api/fahrzeuge", Json);
        list!.ShouldContain(x => x.Id == v.Id);
    }

    [Fact]
    public async Task Tankvorgaenge_berechnen_Verbrauch_und_Statistik()
    {
        var client = await AuthedAsync("stats@tankbuch.at");
        var v = await (await client.PostAsJsonAsync("/api/fahrzeuge",
            new CreateFahrzeugRequest("Octavia", "W-2 CD", "Diesel", "#3B82F6", 40000), Json))
            .Content.ReadFromJsonAsync<FahrzeugDto>(Json);

        await client.PostAsJsonAsync("/api/tankvorgaenge",
            new CreateTankvorgangRequest(v!.Id, new DateOnly(2025, 11, 1), 40m, null, 64m, 41000, "OMV", null, true), Json);
        await client.PostAsJsonAsync("/api/tankvorgaenge",
            new CreateTankvorgangRequest(v.Id, new DateOnly(2025, 11, 20), 30m, null, 48m, 41500, "BP", null, true), Json);

        var entries = await client.GetFromJsonAsync<List<TankvorgangDto>>($"/api/tankvorgaenge?fahrzeugId={v.Id}", Json);
        entries!.Count.ShouldBe(2);
        var zweiter = entries.Single(e => e.Kilometerstand == 41500);
        zweiter.Verbrauch.ShouldBe(6.00m); // 30 l / 500 km * 100
        zweiter.PreisProLiter.ShouldBe(1.6m); // 48 / 30, aus null berechnet

        var stat = await client.GetFromJsonAsync<StatistikDto>($"/api/statistik?fahrzeugId={v.Id}", Json);
        stat!.AnzahlTankvorgaenge.ShouldBe(2);
        stat.Gesamtkosten.ShouldBe(112m);
        stat.DurchschnittsVerbrauch.ShouldBe(6.00m);
    }

    [Fact]
    public async Task Csv_Export_und_Import_Roundtrip()
    {
        var client = await AuthedAsync("csv@tankbuch.at");
        var v = await (await client.PostAsJsonAsync("/api/fahrzeuge",
            new CreateFahrzeugRequest("CSV-Auto", "W-9 ZZ", "Diesel", "#F59E0B", 1000), Json))
            .Content.ReadFromJsonAsync<FahrzeugDto>(Json);
        await client.PostAsJsonAsync("/api/tankvorgaenge",
            new CreateTankvorgangRequest(v!.Id, new DateOnly(2025, 10, 5), 45.5m, null, 72.30m, 1500, "Shell", "Test", true), Json);

        // Export
        var export = await client.GetAsync("/api/csv/export");
        export.StatusCode.ShouldBe(HttpStatusCode.OK);
        var csv = await export.Content.ReadAsStringAsync();
        csv.ShouldContain("CSV-Auto");
        csv.ShouldContain("45,50");

        // Import in einen zweiten Mandanten (andere E-Mail → gleicher Demo-Mandant im Prototyp,
        // daher Duplikat-Erkennung: 0 importiert). Wir importieren stattdessen eine neue Zeile.
        var neu = "fahrzeug;kennzeichen;datum;liter;preis_pro_liter;gesamtpreis;kilometerstand;tankstelle;volltankung;notiz\r\n"
                + "Import-Auto;W-5 II;12.10.2025;30,00;1,600;48,00;2000;JET;ja;importiert\r\n";
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(neu));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", "import.csv");
        var import = await client.PostAsync("/api/csv/import", form);
        var result = await import.Content.ReadFromJsonAsync<CsvImportResult>(Json);
        result!.Importiert.ShouldBe(1);
        result.NeueFahrzeuge.ShouldBe(1);
    }

    [Fact]
    public async Task Ocr_Pump_liefert_simulierte_Werte_ohne_Modell()
    {
        var client = await AuthedAsync("ocr@tankbuch.at");
        using var form = new MultipartFormDataContent();
        var img = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3 }); // Dummy-JPEG-Header
        img.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(img, "image", "pump.jpg");
        var resp = await client.PostAsync("/api/ocr/pump", form);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var r = await resp.Content.ReadFromJsonAsync<PumpOcrResult>(Json);
        r!.Simuliert.ShouldBeTrue();
        r.Liter.ShouldNotBeNull();
        r.Gesamtpreis.ShouldNotBeNull();
    }
}
