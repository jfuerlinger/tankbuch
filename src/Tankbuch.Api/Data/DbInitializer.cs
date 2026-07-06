using Microsoft.EntityFrameworkCore;
using Tankbuch.Api.Domain;

namespace Tankbuch.Api.Data;

public static class DbInitializer
{
    /// <summary>Fester Demo-Mandant. Im Prototyp werden alle Logins auf diesen Mandanten geführt,
    /// damit die Seed-Daten sofort sichtbar sind. Das Schema ist dennoch sauber mandantengetrennt.</summary>
    public static readonly Guid DemoTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string DemoTenantName = "Demo-Haushalt";
    public const string DemoEmail = "demo@tankbuch.at";

    public static async Task InitializeAsync(TankbuchDbContext db, bool seedDemo, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        if (await db.Tenants.AnyAsync(ct)) return;

        var tenant = new Tenant { Id = DemoTenantId, Name = DemoTenantName };
        tenant.Users.Add(new User { Id = Guid.NewGuid(), Email = DemoEmail, TenantId = tenant.Id });
        db.Tenants.Add(tenant);

        if (seedDemo)
        {
            var (fahrzeuge, tankvorgaenge) = DemoSeed.Generate(tenant.Id);
            db.Fahrzeuge.AddRange(fahrzeuge);
            db.Tankvorgaenge.AddRange(tankvorgaenge);
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Findet den Nutzer per E-Mail im Demo-Mandanten oder legt ihn an (Prototyp-OTP-Login).</summary>
    public static async Task<User> UpsertDemoUserAsync(TankbuchDbContext db, string email, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.TenantId == DemoTenantId && u.Email == email, ct);
        if (user is null)
        {
            user = new User { Id = Guid.NewGuid(), Email = email, TenantId = DemoTenantId };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }
        return user;
    }
}
