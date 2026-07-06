using FastEndpoints;
using Tankbuch.Api.Endpoints;
using Tankbuch.Api.Services;
using Tankbuch.Contracts;

namespace Tankbuch.Api.Features.Ocr;

/// <summary>Foto der Zapfsäule → erkannte Liter + Gesamtpreis (Vision-Modell, Fallback: simuliert).</summary>
public sealed class PumpOcrEndpoint(IVisionService vision) : ApiEndpoint<PumpOcrResult>
{
    public override void Configure()
    {
        Post("/api/ocr/pump");
        AllowAnonymous();
        AllowFileUploads();
        Summary(s => s.Summary = "Zapfsäulen-Foto auslesen (Liter + Gesamtpreis)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        var (bytes, mediaType) = await ReadImageAsync(ct);
        if (bytes is null) { ThrowError("Bitte ein Bild hochladen"); return; }
        await Send.OkAsync(await vision.ReadPumpAsync(bytes, mediaType, ct), ct);
    }

    private async Task<(byte[]? Bytes, string MediaType)> ReadImageAsync(CancellationToken ct)
    {
        if (!HttpContext.Request.HasFormContentType) return (null, "");
        var form = await HttpContext.Request.ReadFormAsync(ct);
        var file = form.Files.GetFile("image") ?? form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0) return (null, "");
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return (ms.ToArray(), file.ContentType);
    }
}

/// <summary>Foto des Tachos → erkannter Gesamtkilometerstand (Vision-Modell, Fallback: simuliert).</summary>
public sealed class TachoOcrEndpoint(IVisionService vision) : ApiEndpoint<TachoOcrResult>
{
    public override void Configure()
    {
        Post("/api/ocr/tacho");
        AllowAnonymous();
        AllowFileUploads();
        Summary(s => s.Summary = "Tacho-Foto auslesen (Kilometerstand)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!await AuthAsync(ct)) return;
        if (!HttpContext.Request.HasFormContentType) { ThrowError("Bitte ein Bild hochladen"); return; }
        var form = await HttpContext.Request.ReadFormAsync(ct);
        var file = form.Files.GetFile("image") ?? form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0) { ThrowError("Bitte ein Bild hochladen"); return; }
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        await Send.OkAsync(await vision.ReadTachoAsync(ms.ToArray(), file.ContentType, ct), ct);
    }
}
