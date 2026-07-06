namespace Tankbuch.Api.Services;

/// <summary>Scoped: der aktuelle Mandant/Nutzer, aufgelöst aus dem Bearer-Token.</summary>
public interface ITenantContext
{
    bool IsAuthenticated { get; }
    Guid TenantId { get; }
    Guid UserId { get; }
    string Email { get; }
    void Set(Guid tenantId, Guid userId, string email);
}

public sealed class TenantContext : ITenantContext
{
    public bool IsAuthenticated { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = "";

    public void Set(Guid tenantId, Guid userId, string email)
    {
        TenantId = tenantId;
        UserId = userId;
        Email = email;
        IsAuthenticated = true;
    }
}
