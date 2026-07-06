using Microsoft.EntityFrameworkCore;
using Tankbuch.Api.Domain;

namespace Tankbuch.Api.Data;

public class TankbuchDbContext(DbContextOptions<TankbuchDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Fahrzeug> Fahrzeuge => Set<Fahrzeug>();
    public DbSet<Tankvorgang> Tankvorgaenge => Set<Tankvorgang>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Tenant>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        b.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            e.HasOne(x => x.Tenant).WithMany(t => t.Users).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Fahrzeug>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Kennzeichen).HasMaxLength(32);
            e.Property(x => x.Kraftstoffart).HasMaxLength(40);
            e.Property(x => x.Farbe).HasMaxLength(20);
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Tenant).WithMany(t => t.Fahrzeuge).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Tankvorgang>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Liter).HasPrecision(8, 2);
            e.Property(x => x.PreisProLiter).HasPrecision(8, 3);
            e.Property(x => x.Gesamtpreis).HasPrecision(10, 2);
            e.Property(x => x.Tankstelle).HasMaxLength(200);
            e.Property(x => x.Notiz).HasMaxLength(1000);
            e.HasIndex(x => new { x.TenantId, x.FahrzeugId });
            e.HasOne(x => x.Fahrzeug).WithMany(f => f.Tankvorgaenge).HasForeignKey(x => x.FahrzeugId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
