using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Tankbuch.Api.Domain;
using Tankbuch.Contracts;

namespace Tankbuch.Api.Services;

public interface IVisionService
{
    Task<PumpOcrResult> ReadPumpAsync(byte[] image, string mediaType, CancellationToken ct = default);
    Task<TachoOcrResult> ReadTachoAsync(byte[] image, string mediaType, CancellationToken ct = default);

    /// <summary>Diagnose-Status für die Fehlersuche (siehe GET /api/ocr/status) – zeigt ob ein
    /// echtes Vision-Modell aktiv ist und was beim letzten Aufruf passiert ist.</summary>
    VisionStatus GetStatus();
}

/// <summary>Hält den Status des letzten Vision-Aufrufs prozessweit vor, damit GET /api/ocr/status
/// auch dann Auskunft geben kann, wenn IVisionService pro Request neu instanziiert wird (Scoped).</summary>
internal static class VisionDiagnostics
{
    private static readonly object Lock = new();
    public static DateTimeOffset? LetzterAufruf { get; private set; }
    public static bool? LetzterAufrufErfolgreich { get; private set; }
    public static string? LetzteMeldung { get; private set; }
    public static string? LetzteRohantwort { get; private set; }

    public static void Report(bool erfolgreich, string? meldung, string? rohantwort = null)
    {
        lock (Lock)
        {
            LetzterAufruf = DateTimeOffset.UtcNow;
            LetzterAufrufErfolgreich = erfolgreich;
            LetzteMeldung = meldung;
            if (rohantwort is not null) LetzteRohantwort = VisionParsing.Truncate(rohantwort, 1000);
        }
    }
}

/// <summary>Fallback ohne Modell: plausible, editierbare Vorschlagswerte (Simuliert = true).</summary>
public sealed class SimulatedVisionService(ILogger<SimulatedVisionService> logger) : IVisionService
{
    private const string SimulationsMeldung = "Kein Vision-Modell konfiguriert (Simulationsmodus) – Zufallswerte, bitte prüfen.";

    public Task<PumpOcrResult> ReadPumpAsync(byte[] image, string mediaType, CancellationToken ct = default)
    {
        logger.LogInformation("OCR (Zapfsäule): kein Vision-Modell konfiguriert – simulierte Werte.");
        return Task.FromResult(Simulate.Pump(SimulationsMeldung));
    }

    public Task<TachoOcrResult> ReadTachoAsync(byte[] image, string mediaType, CancellationToken ct = default)
    {
        logger.LogInformation("OCR (Tacho): kein Vision-Modell konfiguriert – simulierte Werte.");
        return Task.FromResult(Simulate.Tacho(SimulationsMeldung));
    }

    public VisionStatus GetStatus() => new(
        Aktiv: false, Modell: null, LetzterAufruf: null, LetzterAufrufErfolgreich: null,
        LetzteMeldung: SimulationsMeldung, LetzteRohantwort: null);
}

internal static class Simulate
{
    public static PumpOcrResult Pump(string? meldung = null)
    {
        double ppl = Math.Round(1.539 + (Random.Shared.NextDouble() - 0.5) * 0.16, 3);
        double liter = Math.Round(22 + Random.Shared.NextDouble() * 30, 2);
        double total = Math.Round(liter * ppl, 2);
        return new PumpOcrResult((decimal)liter, (decimal)total, (decimal)ppl, Simuliert: true, Meldung: meldung);
    }

    public static TachoOcrResult Tacho(string? meldung = null)
        => new(30000 + Random.Shared.Next(0, 60000) + 350L, Simuliert: true, Meldung: meldung);
}

/// <summary>Echte Bild-Erkennung via lokal gehostetem Vision-Modell (Ollama / llama3.2-vision).
/// Bei Fehler oder unplausiblem Ergebnis wird sauber auf simulierte Werte zurückgefallen – der Grund
/// dafür wird als Meldung mitgegeben und zusätzlich in VisionDiagnostics für GET /api/ocr/status
/// festgehalten, damit Fehlerursachen ohne Log-Zugriff nachvollziehbar sind.</summary>
public sealed class OllamaVisionService(IChatClient chat, ILogger<OllamaVisionService> logger, IConfiguration config) : IVisionService
{
    private const string PumpPrompt =
        "Auf dem Foto ist die Anzeige einer Zapfsäule. Lies die getankten Liter und den Gesamtpreis in Euro ab. " +
        "Antworte NUR mit JSON: {\"liter\": <Zahl>, \"gesamtpreis\": <Zahl>} (Punkt als Dezimaltrennzeichen). " +
        "Wenn nicht erkennbar: {\"liter\": null, \"gesamtpreis\": null}";

    private const string TachoPrompt =
        "Auf dem Foto ist ein Auto-Tacho / Kombiinstrument. Lies den Gesamtkilometerstand (Odometer) ab. " +
        "Antworte NUR mit JSON: {\"kilometerstand\": <Zahl>}. Wenn nicht erkennbar: {\"kilometerstand\": null}";

    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(config.GetValue("Vision:TimeoutSeconds", 45));

    public async Task<PumpOcrResult> ReadPumpAsync(byte[] image, string mediaType, CancellationToken ct = default)
    {
        var ask = await AskAsync("Zapfsäule", PumpPrompt, image, mediaType, ct);
        if (ask.Json is not { } json)
            return Simulate.Pump(ask.Fehler ?? "Bilderkennung fehlgeschlagen – Zufallswerte verwendet.");

        var literOk = VisionParsing.TryGetFlexibleNumber(json, "liter", out var liter);
        var totalOk = VisionParsing.TryGetFlexibleNumber(json, "gesamtpreis", out var total);
        if (!literOk || !totalOk || liter is not decimal l || total is not decimal t)
        {
            var meldung = "Vision-Modell konnte Liter/Gesamtpreis nicht erkennen – Zufallswerte verwendet.";
            logger.LogWarning("Zapfsäulen-Erkennung: liter/gesamtpreis fehlen oder null. JSON: {Json}", json.GetRawText());
            VisionDiagnostics.Report(false, meldung, ask.RawResponse);
            return Simulate.Pump(meldung);
        }

        if (l is <= 0.5m or >= 200m || t is <= 0.5m or >= 1000m)
        {
            var meldung = $"Erkannte Werte unplausibel (liter={l.ToString(System.Globalization.CultureInfo.InvariantCulture)}, gesamtpreis={t.ToString(System.Globalization.CultureInfo.InvariantCulture)}) – Zufallswerte verwendet.";
            logger.LogWarning("Zapfsäulen-Erkennung: unplausible Werte liter={Liter} gesamtpreis={Gesamtpreis}.", l, t);
            VisionDiagnostics.Report(false, meldung, ask.RawResponse);
            return Simulate.Pump(meldung);
        }

        logger.LogInformation("Zapfsäulen-Erkennung erfolgreich: {Liter} l, {Gesamtpreis} €.", l, t);
        VisionDiagnostics.Report(true, null, ask.RawResponse);
        return new PumpOcrResult(Math.Round(l, 2), Math.Round(t, 2), Berechnung.PreisProLiter(t, l), Simuliert: false);
    }

    public async Task<TachoOcrResult> ReadTachoAsync(byte[] image, string mediaType, CancellationToken ct = default)
    {
        var ask = await AskAsync("Tacho", TachoPrompt, image, mediaType, ct);
        if (ask.Json is not { } json)
            return Simulate.Tacho(ask.Fehler ?? "Bilderkennung fehlgeschlagen – Zufallswerte verwendet.");

        if (!VisionParsing.TryGetFlexibleInteger(json, "kilometerstand", out var km) || km is not long value)
        {
            var meldung = "Vision-Modell konnte den Kilometerstand nicht erkennen – Zufallswerte verwendet.";
            logger.LogWarning("Tacho-Erkennung: kilometerstand fehlt oder null. JSON: {Json}", json.GetRawText());
            VisionDiagnostics.Report(false, meldung, ask.RawResponse);
            return Simulate.Tacho(meldung);
        }

        if (value is <= 10 or >= 3_000_000)
        {
            var meldung = $"Erkannter Kilometerstand unplausibel ({value}) – Zufallswerte verwendet.";
            logger.LogWarning("Tacho-Erkennung: unplausibler Kilometerstand {Kilometerstand}.", value);
            VisionDiagnostics.Report(false, meldung, ask.RawResponse);
            return Simulate.Tacho(meldung);
        }

        logger.LogInformation("Tacho-Erkennung erfolgreich: {Kilometerstand} km.", value);
        VisionDiagnostics.Report(true, null, ask.RawResponse);
        return new TachoOcrResult(value, Simuliert: false);
    }

    public VisionStatus GetStatus()
    {
        string? modell = null;
        try
        {
            modell = chat.GetService<ChatClientMetadata>()?.DefaultModelId;
        }
        catch
        {
            // Metadaten sind optional – bei fehlender Unterstützung einfach ohne Modellnamen weitermachen.
        }

        return new VisionStatus(
            Aktiv: true,
            Modell: modell ?? "llama3.2-vision",
            LetzterAufruf: VisionDiagnostics.LetzterAufruf,
            LetzterAufrufErfolgreich: VisionDiagnostics.LetzterAufrufErfolgreich,
            LetzteMeldung: VisionDiagnostics.LetzteMeldung,
            LetzteRohantwort: VisionDiagnostics.LetzteRohantwort);
    }

    private readonly record struct AskResult(JsonElement? Json, string? RawResponse, string? Fehler);

    private async Task<AskResult> AskAsync(string art, string prompt, byte[] image, string mediaType, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);
        try
        {
            var message = new ChatMessage(ChatRole.User,
            [
                new TextContent(prompt),
                new DataContent(image, string.IsNullOrWhiteSpace(mediaType) ? "image/jpeg" : mediaType),
            ]);
            var response = await chat.GetResponseAsync([message], new ChatOptions { Temperature = 0f, MaxOutputTokens = 300 }, timeoutCts.Token);
            var text = response.Text ?? "";
            logger.LogDebug("Vision-Antwort ({Art}, {ElapsedMs} ms, {Bytes} Bytes Bild): {Text}",
                art, sw.ElapsedMilliseconds, image.Length, VisionParsing.Truncate(text, 500));

            if (!VisionParsing.TryExtractJson(text, out var json))
            {
                var meldung = "Antwort des Vision-Modells enthielt kein gültiges JSON.";
                logger.LogWarning("Vision-Erkennung ({Art}): {Meldung} Rohtext: {Text}", art, meldung, VisionParsing.Truncate(text, 500));
                VisionDiagnostics.Report(false, meldung, text);
                return new AskResult(null, text, meldung);
            }
            return new AskResult(json, text, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            var meldung = $"Zeitüberschreitung beim Vision-Modell (> {_timeout.TotalSeconds:0} s).";
            logger.LogWarning("Vision-Erkennung ({Art}): Zeitüberschreitung nach {ElapsedMs} ms.", art, sw.ElapsedMilliseconds);
            VisionDiagnostics.Report(false, meldung);
            return new AskResult(null, null, meldung);
        }
        catch (Exception ex)
        {
            var meldung = $"Fehler bei der Bilderkennung: {ex.Message}";
            logger.LogWarning(ex, "Vision-Erkennung ({Art}) fehlgeschlagen nach {ElapsedMs} ms – Fallback auf simulierte Werte.", art, sw.ElapsedMilliseconds);
            VisionDiagnostics.Report(false, meldung);
            return new AskResult(null, null, meldung);
        }
    }
}
