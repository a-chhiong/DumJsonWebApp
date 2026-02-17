using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace JoshAuthorization.Objects;

public class CnfObject
{
    /// <summary>
    /// JWK Thumbprint
    /// </summary>
    public string? jkt { get; init; }

    public static CnfObject? From(JwkObject? jwk)
    {
        if (jwk == null) return null;

        // RFC 7638: Mandatory fields for EC thumbprint in lexicographical order: crv, kty, x, y
        // RFC 7638 requires EXACTLY this format: no spaces, alphabetical order
        // Order: crv, kty, x, y
        var jsonString = $"{{\"crv\":\"{jwk.crv}\",\"kty\":\"{jwk.kty}\",\"x\":\"{jwk.x}\",\"y\":\"{jwk.y}\"}}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(jsonString));

        return new CnfObject { jkt = Jose.Base64Url.Encode(hash) };
    }
}