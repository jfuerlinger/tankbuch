using System.Text.Json;
using Shouldly;
using Tankbuch.Api.Services;

namespace Tankbuch.UnitTests;

public class VisionParsingTests
{
    // ---------- TryExtractJson ----------

    [Fact]
    public void TryExtractJson_reines_Json()
    {
        VisionParsing.TryExtractJson("{\"liter\": 34.56, \"gesamtpreis\": 55.1}", out var el).ShouldBeTrue();
        el.GetProperty("liter").GetDouble().ShouldBe(34.56);
    }

    [Fact]
    public void TryExtractJson_mit_Markdown_Codefence_und_Fliesstext()
    {
        var text = "Klar, hier ist das Ergebnis:\n```json\n{\"liter\": 40.2, \"gesamtpreis\": 61.5}\n```\nBitte prüfen.";
        VisionParsing.TryExtractJson(text, out var el).ShouldBeTrue();
        el.GetProperty("liter").GetDouble().ShouldBe(40.2);
    }

    [Fact]
    public void TryExtractJson_mehrere_Objekte_nimmt_erstes_gueltiges()
    {
        var text = "Vorher: {ungueltig} dann {\"kilometerstand\": 48230} Ende.";
        VisionParsing.TryExtractJson(text, out var el).ShouldBeTrue();
        el.GetProperty("kilometerstand").GetInt64().ShouldBe(48230);
    }

    [Fact]
    public void TryExtractJson_kein_Json_liefert_false()
    {
        VisionParsing.TryExtractJson("Ich kann nichts erkennen.", out _).ShouldBeFalse();
    }

    [Fact]
    public void TryExtractJson_leerer_Text_liefert_false()
    {
        VisionParsing.TryExtractJson("", out _).ShouldBeFalse();
        VisionParsing.TryExtractJson(null, out _).ShouldBeFalse();
    }

    // ---------- TryGetFlexibleNumber / TryParseFlexibleDecimal ----------

    [Theory]
    [InlineData("34.56", 34.56)]
    [InlineData("34,56", 34.56)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("34,56 l", 34.56)]
    [InlineData("72,30 €", 72.30)]
    [InlineData(" 45.5 ", 45.5)]
    public void TryParseFlexibleDecimal_erkennt_deutsche_und_invariante_Formate(string raw, double expected)
    {
        VisionParsing.TryParseFlexibleDecimal(raw, out var value).ShouldBeTrue();
        value.ShouldBe((decimal)expected);
    }

    [Fact]
    public void TryParseFlexibleDecimal_leerer_string_liefert_false()
    {
        VisionParsing.TryParseFlexibleDecimal("", out _).ShouldBeFalse();
        VisionParsing.TryParseFlexibleDecimal(null, out _).ShouldBeFalse();
        VisionParsing.TryParseFlexibleDecimal("nicht erkennbar", out _).ShouldBeFalse();
    }

    [Fact]
    public void TryGetFlexibleNumber_json_zahl()
    {
        using var doc = JsonDocument.Parse("{\"liter\": 34.56}");
        VisionParsing.TryGetFlexibleNumber(doc.RootElement, "liter", out var value).ShouldBeTrue();
        value.ShouldBe(34.56m);
    }

    [Fact]
    public void TryGetFlexibleNumber_json_string_mit_komma()
    {
        using var doc = JsonDocument.Parse("{\"gesamtpreis\": \"55,10 €\"}");
        VisionParsing.TryGetFlexibleNumber(doc.RootElement, "gesamtpreis", out var value).ShouldBeTrue();
        value.ShouldBe(55.10m);
    }

    [Fact]
    public void TryGetFlexibleNumber_json_null_liefert_true_mit_null_value()
    {
        using var doc = JsonDocument.Parse("{\"liter\": null}");
        VisionParsing.TryGetFlexibleNumber(doc.RootElement, "liter", out var value).ShouldBeTrue();
        value.ShouldBeNull();
    }

    [Fact]
    public void TryGetFlexibleNumber_fehlendes_feld_liefert_false()
    {
        using var doc = JsonDocument.Parse("{\"liter\": 34.56}");
        VisionParsing.TryGetFlexibleNumber(doc.RootElement, "gesamtpreis", out _).ShouldBeFalse();
    }

    // ---------- TryGetFlexibleInteger / TryParseFlexibleInteger (Kilometerstand) ----------

    [Theory]
    [InlineData("48230", 48230)]
    [InlineData("48.230", 48230)]
    [InlineData("48,230", 48230)]
    [InlineData("48 230 km", 48230)]
    public void TryParseFlexibleInteger_ignoriert_tausendertrennzeichen_und_einheiten(string raw, long expected)
    {
        VisionParsing.TryParseFlexibleInteger(raw, out var value).ShouldBeTrue();
        value.ShouldBe(expected);
    }

    [Fact]
    public void TryGetFlexibleInteger_json_zahl()
    {
        using var doc = JsonDocument.Parse("{\"kilometerstand\": 48230}");
        VisionParsing.TryGetFlexibleInteger(doc.RootElement, "kilometerstand", out var value).ShouldBeTrue();
        value.ShouldBe(48230);
    }

    [Fact]
    public void TryGetFlexibleInteger_json_string_mit_tausenderpunkt()
    {
        using var doc = JsonDocument.Parse("{\"kilometerstand\": \"48.230 km\"}");
        VisionParsing.TryGetFlexibleInteger(doc.RootElement, "kilometerstand", out var value).ShouldBeTrue();
        value.ShouldBe(48230);
    }

    [Fact]
    public void TryGetFlexibleInteger_json_null_liefert_true_mit_null_value()
    {
        using var doc = JsonDocument.Parse("{\"kilometerstand\": null}");
        VisionParsing.TryGetFlexibleInteger(doc.RootElement, "kilometerstand", out var value).ShouldBeTrue();
        value.ShouldBeNull();
    }
}
