namespace Tankbuch.Api.Domain;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<User> Users { get; set; } = new();
    public List<Fahrzeug> Fahrzeuge { get; set; } = new();
}

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
}

public class Fahrzeug
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Name { get; set; } = "";
    public string Kennzeichen { get; set; } = "";
    public string Kraftstoffart { get; set; } = "Diesel";
    public string Farbe { get; set; } = "#3B82F6";
    public long AnfangsKilometer { get; set; }
    public List<Tankvorgang> Tankvorgaenge { get; set; } = new();
}

public class Tankvorgang
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FahrzeugId { get; set; }
    public Fahrzeug? Fahrzeug { get; set; }
    public DateOnly Datum { get; set; }
    public decimal Liter { get; set; }
    public decimal PreisProLiter { get; set; }
    public decimal Gesamtpreis { get; set; }
    public long Kilometerstand { get; set; }
    public string? Tankstelle { get; set; }
    public string? Notiz { get; set; }
    public bool Volltankung { get; set; }
}
