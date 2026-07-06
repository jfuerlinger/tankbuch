using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tankbuch.Contracts;

namespace Tankbuch.Cli.Infrastructure;

/// <summary>
/// Dünner HTTP-Client für die Tankbuch-API. Die CLI greift AUSSCHLIESSLICH über diese API zu –
/// niemals direkt auf die Datenbank.
/// </summary>
public sealed class ApiClient(string baseUrl, string? token)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http = CreateHttp(baseUrl, token);

    private static HttpClient CreateHttp(string baseUrl, string? token)
    {
        var http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromMinutes(5) };
        if (!string.IsNullOrWhiteSpace(token))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    // ---------- Auth ----------
    public Task<RequestCodeResponse> RequestCodeAsync(string email) =>
        PostAsync<RequestCodeResponse>("api/auth/request-code", new RequestCodeRequest(email));
    public Task<VerifyCodeResponse> VerifyAsync(string email, string code) =>
        PostAsync<VerifyCodeResponse>("api/auth/verify", new VerifyCodeRequest(email, code));
    public Task<MeResponse> MeAsync() => GetAsync<MeResponse>("api/auth/me");

    // ---------- Fahrzeuge ----------
    public Task<List<FahrzeugDto>> ListFahrzeugeAsync() => GetAsync<List<FahrzeugDto>>("api/fahrzeuge");
    public Task<FahrzeugDto> CreateFahrzeugAsync(CreateFahrzeugRequest r) => PostAsync<FahrzeugDto>("api/fahrzeuge", r);
    public Task<FahrzeugDto> UpdateFahrzeugAsync(Guid id, UpdateFahrzeugRequest r) => PutAsync<FahrzeugDto>($"api/fahrzeuge/{id}", r);
    public Task DeleteFahrzeugAsync(Guid id) => DeleteAsync($"api/fahrzeuge/{id}");

    // ---------- Tankvorgänge ----------
    public Task<List<TankvorgangDto>> ListTankvorgaengeAsync(Guid? fahrzeugId, int? tage)
    {
        var q = new List<string>();
        if (fahrzeugId is Guid f) q.Add($"fahrzeugId={f}");
        if (tage is int t) q.Add($"tage={t}");
        var qs = q.Count > 0 ? "?" + string.Join("&", q) : "";
        return GetAsync<List<TankvorgangDto>>("api/tankvorgaenge" + qs);
    }
    public Task<TankvorgangDto> CreateTankvorgangAsync(CreateTankvorgangRequest r) => PostAsync<TankvorgangDto>("api/tankvorgaenge", r);
    public Task<TankvorgangDto> UpdateTankvorgangAsync(Guid id, UpdateTankvorgangRequest r) => PutAsync<TankvorgangDto>($"api/tankvorgaenge/{id}", r);
    public Task DeleteTankvorgangAsync(Guid id) => DeleteAsync($"api/tankvorgaenge/{id}");

    // ---------- Statistik ----------
    public Task<StatistikDto> GetStatistikAsync(Guid? fahrzeugId) =>
        GetAsync<StatistikDto>("api/statistik" + (fahrzeugId is Guid f ? $"?fahrzeugId={f}" : ""));

    // ---------- CSV ----------
    public async Task<(string FileName, byte[] Content)> ExportCsvAsync()
    {
        using var res = await _http.GetAsync("api/csv/export");
        await EnsureOkAsync(res);
        var name = res.Content.Headers.ContentDisposition?.FileNameStar ?? res.Content.Headers.ContentDisposition?.FileName ?? "tankbuch-export.csv";
        return (name.Trim('"'), await res.Content.ReadAsByteArrayAsync());
    }
    public async Task<CsvImportResult> ImportCsvAsync(byte[] content, string fileName)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", fileName);
        using var res = await _http.PostAsync("api/csv/import", form);
        return await ReadAsync<CsvImportResult>(res);
    }

    // ---------- OCR ----------
    public Task<PumpOcrResult> OcrPumpAsync(byte[] img, string fileName, string contentType) => UploadImageAsync<PumpOcrResult>("api/ocr/pump", img, fileName, contentType);
    public Task<TachoOcrResult> OcrTachoAsync(byte[] img, string fileName, string contentType) => UploadImageAsync<TachoOcrResult>("api/ocr/tacho", img, fileName, contentType);
    public Task<VisionStatus> OcrStatusAsync() => GetAsync<VisionStatus>("api/ocr/status");

    private async Task<T> UploadImageAsync<T>(string path, byte[] img, string fileName, string contentType)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(img);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "image", fileName);
        using var res = await _http.PostAsync(path, form);
        return await ReadAsync<T>(res);
    }

    // ---------- Helpers ----------
    private async Task<T> GetAsync<T>(string path) => await ReadAsync<T>(await _http.GetAsync(path));
    private async Task<T> PostAsync<T>(string path, object body) => await ReadAsync<T>(await _http.PostAsJsonAsync(path, body, Json));
    private async Task<T> PutAsync<T>(string path, object body) => await ReadAsync<T>(await _http.PutAsJsonAsync(path, body, Json));
    private async Task DeleteAsync(string path) => await EnsureOkAsync(await _http.DeleteAsync(path));

    private static async Task<T> ReadAsync<T>(HttpResponseMessage res)
    {
        await EnsureOkAsync(res);
        var result = await res.Content.ReadFromJsonAsync<T>(Json);
        return result ?? throw new ApiException("Leere Antwort vom Server.");
    }

    private static async Task EnsureOkAsync(HttpResponseMessage res)
    {
        if (res.IsSuccessStatusCode) return;
        var body = await res.Content.ReadAsStringAsync();
        if ((int)res.StatusCode == 401)
            throw new ApiException("Nicht angemeldet. Bitte zuerst `tb login --email <E-Mail>` ausführen.");
        var msg = ParseError(body) ?? $"Fehler {(int)res.StatusCode} ({res.ReasonPhrase}).";
        throw new ApiException(msg);
    }

    private static string? ParseError(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
            {
                var sb = new StringBuilder();
                foreach (var p in errs.EnumerateObject())
                    foreach (var v in p.Value.EnumerateArray())
                        sb.Append(sb.Length > 0 ? ", " : "").Append(v.GetString());
                if (sb.Length > 0) return sb.ToString();
            }
            if (root.TryGetProperty("message", out var m)) return m.GetString();
            if (root.TryGetProperty("title", out var t)) return t.GetString();
        }
        catch { /* kein JSON */ }
        return null;
    }
}

public sealed class ApiException(string message) : Exception(message);
