using System.Security.Cryptography;
using System.Text;

namespace Tankbuch.Api.Services;

/// <summary>
/// Kompaktes, HMAC-signiertes Session-Token (Prototyp – kein vollwertiges JWT).
/// Format: base64url(payload) "." base64url(hmac). payload = "tenantId|userId|email".
/// </summary>
public sealed class TokenService(IConfiguration config)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(
        config["Auth:TokenSecret"] ?? "tankbuch-dev-secret-please-override-in-production-0123456789");

    public string Create(Guid tenantId, Guid userId, string email)
    {
        var payload = $"{tenantId}|{userId}|{email}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var sig = HMACSHA256.HashData(_key, payloadBytes);
        return $"{B64(payloadBytes)}.{B64(sig)}";
    }

    public bool TryValidate(string? token, out Guid tenantId, out Guid userId, out string email)
    {
        tenantId = Guid.Empty;
        userId = Guid.Empty;
        email = "";
        if (string.IsNullOrWhiteSpace(token)) return false;

        var parts = token.Split('.');
        if (parts.Length != 2) return false;

        try
        {
            var payloadBytes = UnB64(parts[0]);
            var sig = UnB64(parts[1]);
            var expected = HMACSHA256.HashData(_key, payloadBytes);
            if (!CryptographicOperations.FixedTimeEquals(sig, expected)) return false;

            var payload = Encoding.UTF8.GetString(payloadBytes).Split('|');
            if (payload.Length != 3) return false;
            if (!Guid.TryParse(payload[0], out tenantId) || !Guid.TryParse(payload[1], out userId)) return false;
            email = payload[2];
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] UnB64(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }
}
