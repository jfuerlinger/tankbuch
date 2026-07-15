using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.Extensions.Http.Resilience;
using Tankbuch.Api.Data;
using Tankbuch.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Aspire ServiceDefaults (Telemetry, Health, Service Discovery, Resilience)
builder.AddServiceDefaults();

// PostgreSQL via Aspire (Connection-Name = Ressourcenname im AppHost)
builder.AddNpgsqlDbContext<TankbuchDbContext>("tankbuchdb");

// FastEndpoints + OpenAPI/Swagger
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Tankbuch API";
        s.Version = "v1";
        s.Description = "Backend für das digitale Tankbuch (Fahrzeuge, Tankvorgänge, Statistik, CSV, OCR).";
    };
});

// Mandanten-/Auth-Dienste
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddSingleton<TokenService>();

// CORS für das Vite-Frontend (Dev: beliebiger Origin, kein Cookie-Credential nötig – Bearer-Token)
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition")));

// Vision-/OCR-Dienst: lokal gehostetes Modell (Ollama) falls verfügbar, sonst simuliert.
var hasVision = !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("vision"));
if (hasVision)
{
    builder.AddOllamaApiClient("vision").AddChatClient();
    // AddServiceDefaults() ruft ConfigureHttpClientDefaults(http => { http.AddStandardResilienceHandler(); ... })
    // auf. Dieser Aufruf verwendet intern einen HttpClientBuilder mit Name = null (siehe
    // Microsoft.Extensions.Http.DefaultHttpClientBuilder – gilt für ALLE HttpClients der App, nicht
    // pro Client), wodurch die resultierenden benannten Options via PipelineNameHelper.GetName(null,
    // "standard") = "-standard" heißen (nicht "{ClientName}-standard"!). Ein zusätzlicher, clientspezifisch
    // benannter AddStandardResilienceHandler()-Aufruf würde nur einen ZWEITEN, verschachtelten Handler
    // registrieren – die äußere globale 30s-Grenze bliebe trotzdem bestehen. Foto-Erkennung per
    // CPU-Inferenz (kein GPU-Durchgriff) kann deutlich länger dauern als 30s – ohne dieses Override
    // killt der globale Standard-Handler den Ollama-Request, bevor OllamaVisionService.AskAsync
    // (Vision:TimeoutSeconds, Default 45s) sein eigenes, sauber abgefangenes Timeout mit verständlicher
    // Meldung greifen lassen kann. Da die App außer Ollama/OTLP keine weiteren latenzkritischen
    // Außenverbindungen hat, ist eine app-weite Anhebung hier unkritisch.
    builder.Services.Configure<HttpStandardResilienceOptions>("-standard", o =>
    {
        o.AttemptTimeout.Timeout = TimeSpan.FromMinutes(2);
        o.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(4);
    });
    builder.Services.AddScoped<IVisionService, OllamaVisionService>();
}
else
{
    builder.Services.AddScoped<IVisionService, SimulatedVisionService>();
}

var app = builder.Build();

app.UseCors();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseFastEndpoints(c =>
{
    c.Serializer.Options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
app.UseSwaggerGen();

app.MapDefaultEndpoints();

// Datenbank anlegen + Demo-Daten seeden
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TankbuchDbContext>();
    var seed = app.Configuration.GetValue("SeedDemoData", true);
    await DbInitializer.InitializeAsync(db, seed);
}

app.Run();

// Für WebApplicationFactory in den Integrationstests
public partial class Program;
