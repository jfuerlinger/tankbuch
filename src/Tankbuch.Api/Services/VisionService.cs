using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Tankbuch.Api.Domain;
using Tankbuch.Contracts;

namespace Tankbuch.Api.Services;

public interface IVisionService
{
    Task<PumpOcrResult> ReadPumpAsync(byte[] image, string mediaType, CancellationToken ct = default);
    Task<TachoOcrResult> ReadTachoAsync(byte[] image, string mediaType, CancellationToken ct = default);
}

/// <summary>Fallback ohne Modell: plausible, editierbare Vorschlagswerte (Simuliert = true).</summary>
public sealed class SimulatedVisionService : IVisionService
{
    public Task<PumpOcrResult> ReadPumpAsync(byte[] image, string mediaType, CancellationToken ct = default)
        => Task.FromResult(Simulate.Pump());

    public Task<TachoOcrResult> ReadTachoAsync(byte[] image, string mediaType, CancellationToken ct = default)
        => Task.FromResult(Simulate.Tacho());
}

internal static class Simulate
{
    public static PumpOcrResult Pump()
    {
        double ppl = Math.Round(1.539 + (Random.Shared.NextDouble() - 0.5) * 0.16, 3);
        double liter = Math.Round(22 + Random.Shared.NextDouble() * 30, 2);
        double total = Math.Round(liter * ppl, 2);
        return new PumpOcrResult((decimal)liter, (decimal)total, (decimal)ppl, Simuliert: true);
    }

    public static TachoOcrResult Tacho()
        => new(30000 + Random.Shared.Next(0, 60000) + 350L, Simuliert: true);
}

/// <summary>Echte Bild-Erkennung via lokal gehostetem Vision-Modell (Ollama / llama3.2-vision).
/// Bei Fehler oder unplausiblem Ergebnis wird sauber auf simulierte Werte zurückgefallen.</summary>
public sealed class OllamaVisionService(IChatClient chat, ILogger<OllamaVisionService> logger) : IVisionService
{
    private const string PumpPrompt =
        "Auf dem Foto ist die Anzeige einer Zapfsäule. Lies die getankten Liter und den Gesamtpreis in Euro ab. " +
        "Antworte NUR mit JSON: {\"liter\": <Zahl>, \"gesamtpreis\": <Zahl>} (Punkt als Dezimaltrennzeichen). " +
        "Wenn nicht erkennbar: {\"liter\": null, \"gesamtpreis\": null}";

    private const string TachoPrompt =
        "Auf dem Foto ist ein Auto-Tacho / Kombiinstrument. Lies den Gesamtkilometerstand (Odometer) ab. " +
        "Antworte NUR mit JSON: {\"kilometerstand\": <Zahl>}. Wenn nicht erkennbar: {\"kilometerstand\": null}";

    public async Task<PumpOcrResult> ReadPumpAsync(byte[] image, string mediaType, CancellationToken ct = default)
    {
        var json = await AskAsync(PumpPrompt, image, mediaType, ct);
        if (json is not null &&
            TryNum(json, "liter", out var liter) && TryNum(json, "gesamtpreis", out var total) &&
            liter is > 0.5m and < 200m && total is > 0.5m and < 1000m)
        {
            return new PumpOcrResult(
                Math.Round(liter.Value, 2),
                Math.Round(total.Value, 2),
                Berechnung.PreisProLiter(total.Value, liter.Value),
                Simuliert: false);
        }
        return Simulate.Pump();
    }

    public async Task<TachoOcrResult> ReadTachoAsync(byte[] image, string mediaType, CancellationToken ct = default)
    {
        var json = await AskAsync(TachoPrompt, image, mediaType, ct);
        if (json is not null && TryNum(json, "kilometerstand", out var km) && km is > 10m and < 3_000_000m)
            return new TachoOcrResult((long)Math.Round(km.Value), Simuliert: false);
        return Simulate.Tacho();
    }

    private async Task<JsonElement?> AskAsync(string prompt, byte[] image, string mediaType, CancellationToken ct)
    {
        try
        {
            var message = new ChatMessage(ChatRole.User,
            [
                new TextContent(prompt),
                new DataContent(image, string.IsNullOrWhiteSpace(mediaType) ? "image/jpeg" : mediaType),
            ]);
            var response = await chat.GetResponseAsync([message], new ChatOptions { Temperature = 0f, MaxOutputTokens = 300 }, ct);
            var match = Regex.Match(response.Text ?? "", "\\{[\\s\\S]*\\}");
            if (!match.Success) return null;
            using var doc = JsonDocument.Parse(match.Value);
            return doc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Vision-Erkennung fehlgeschlagen – Fallback auf simulierte Werte.");
            return null;
        }
    }

    private static bool TryNum(JsonElement? el, string prop, out decimal? value)
    {
        value = null;
        if (el is JsonElement e && e.TryGetProperty(prop, out var p))
        {
            if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d)) { value = d; return true; }
            if (p.ValueKind == JsonValueKind.Null) { value = null; return true; }
            if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var s)) { value = s; return true; }
        }
        return false;
    }
}
