namespace Tankbuch.Api.Services;

/// <summary>Liest das Bearer-Token und füllt den scoped <see cref="ITenantContext"/>.</summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext ctx, TokenService tokens, ITenantContext tenant)
    {
        var auth = ctx.Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = auth["Bearer ".Length..].Trim();
            if (tokens.TryValidate(token, out var tid, out var uid, out var email))
                tenant.Set(tid, uid, email);
        }
        await next(ctx);
    }
}
