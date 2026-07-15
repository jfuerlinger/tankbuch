var builder = DistributedApplication.CreateBuilder(args);

// ---------- Docker Compose Deployment ----------
// Aktiviert `aspire publish` / `aspire deploy`, um die gesamte Anwendung als
// Docker-Compose-Stack (docker-compose.yaml + .env) zu erzeugen und auszurollen.
builder.AddDockerComposeEnvironment("compose");

// ---------- Container-Registry (Docker Hub) ----------
// Selbst gebaute Images (api, frontend) werden bei `aspire publish`/`aspire deploy` nach
// Docker Hub gepusht, damit Ziel-Hosts (z. B. proxmox-01) sie nur pullen statt lokal aus dem
// Quellcode zu bauen. Nur in Publish-Mode aktiv (aspire run bleibt unberührt).
#pragma warning disable ASPIRECOMPUTE003
var dockerHub = builder.AddContainerRegistry("docker-hub", "docker.io", "jfuerlinger");
#pragma warning restore ASPIRECOMPUTE003

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
#pragma warning disable ASPIRECOMPUTE003
var api = builder.AddProject<Projects.Tankbuch_Api>("api")
    .WithReference(db).WaitFor(db)
    .WithReference(vision)
    .WithExternalHttpEndpoints()
    .WithContainerRegistry(dockerHub);
#pragma warning restore ASPIRECOMPUTE003

// ---------- Frontend (Vite + React) ----------
// PublishAsStaticWebsite: Aspire erzeugt für das Deployment einen eigenen Container,
// der die gebauten Vite-Assets über YARP ausliefert und `/api/*` per Service Discovery
// an das Backend weiterleitet. Wichtig: VITE_API_BASE_URL wird bewusst NICHT gesetzt,
// da Vite-Umgebungsvariablen zur Build-Zeit gebacken werden und zur Laufzeit im
// Deployment nicht mehr greifen würden (siehe frontend/vite.config.ts für den
// Dev-Proxy, der denselben /api-Pfad lokal über den Vite-Dev-Server nachbildet).
#pragma warning disable ASPIREJAVASCRIPT001, ASPIRECOMPUTE003
var frontend = builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api).WaitFor(api)
    .PublishAsStaticWebsite(apiPath: "/api", apiTarget: api)
    .WithExternalHttpEndpoints()
    .WithContainerRegistry(dockerHub);
#pragma warning restore ASPIREJAVASCRIPT001, ASPIRECOMPUTE003

// ---------- DevTunnel für das Frontend ----------
// Macht den lokalen Vite-Dev-Server über eine öffentliche https://*.devtunnels.ms-URL
// erreichbar (z. B. für Tests auf Mobilgeräten oder Demos). Nur ein Dev-Time-Feature,
// wird beim Publish/Deploy nicht berücksichtigt.
builder.AddDevTunnel("frontend-tunnel")
    .WithReference(frontend)
    .WithAnonymousAccess();

builder.Build().Run();
