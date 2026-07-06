using FastEndpoints;
using FastEndpoints.Swagger;
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
