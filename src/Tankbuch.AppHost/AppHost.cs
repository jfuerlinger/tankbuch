var builder = DistributedApplication.CreateBuilder(args);

// ---------- PostgreSQL ----------
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()                              // Daten über Neustarts hinweg behalten
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();                                // Web-UI zur DB-Inspektion
var db = postgres.AddDatabase("tankbuchdb");

// ---------- Lokales Vision-Modell (Ollama) für die Foto-Erkennung (OCR) ----------
// llama3.2-vision:11b liest Liter/Gesamtpreis von der Zapfsäule bzw. den Kilometerstand vom Tacho.
// Erster Start lädt die Gewichte (~7,9 GB) herunter; das DataVolume verhindert erneutes Laden.
var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithOpenWebUI();                              // optionale Chat-UI zum Testen des Modells
var vision = ollama.AddModel("vision", "llama3.2-vision");

// ---------- Backend (.NET 10 WebApi + FastEndpoints) ----------
// Hinweis: bewusst kein WaitFor(vision) – der Stack startet sofort, das Modell lädt im
// Hintergrund. Bis es bereit ist, fällt die OCR sauber auf simulierte Werte zurück.
var api = builder.AddProject<Projects.Tankbuch_Api>("api")
    .WithReference(db).WaitFor(db)
    .WithReference(vision)
    .WithExternalHttpEndpoints();

// ---------- Frontend (Vite + React) ----------
builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api).WaitFor(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
