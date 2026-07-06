using FastEndpoints;
using Tankbuch.Api.Data;
using Tankbuch.Api.Services;

namespace Tankbuch.Api.Endpoints;

// Alle Endpunkte laufen FE-seitig anonym; die Authentifizierung/Mandantenauflösung erfolgt
// über die TenantResolutionMiddleware + AuthAsync-Gate (Prototyp-Token statt ASP.NET-Auth-Schema).

public abstract class ApiEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse> where TRequest : notnull
{
    protected TankbuchDbContext Db => Resolve<TankbuchDbContext>();
    protected ITenantContext Tenant => Resolve<ITenantContext>();
    protected Guid TenantId => Tenant.TenantId;

    protected async Task<bool> AuthAsync(CancellationToken ct)
    {
        if (Tenant.IsAuthenticated) return true;
        await Send.UnauthorizedAsync(ct);
        return false;
    }
}

public abstract class ApiEndpoint<TResponse> : EndpointWithoutRequest<TResponse>
{
    protected TankbuchDbContext Db => Resolve<TankbuchDbContext>();
    protected ITenantContext Tenant => Resolve<ITenantContext>();
    protected Guid TenantId => Tenant.TenantId;

    protected async Task<bool> AuthAsync(CancellationToken ct)
    {
        if (Tenant.IsAuthenticated) return true;
        await Send.UnauthorizedAsync(ct);
        return false;
    }
}
