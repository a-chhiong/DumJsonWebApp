using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace JoshAuthorization.Objects;

public class JwkObject : IEquatable<JwkObject>
{
    /// <summary>
    /// Key Type
    /// </summary>
    public string? kty { get; init; }
    /// <summary>
    /// Curve
    /// </summary>
    public string? crv { get; init; }
    /// <summary>
    /// EC X Coordinate
    /// </summary>
    public string? x { get; init; }
    /// <summary>
    /// EC Y Coordinate
    /// </summary>
    public string? y { get; init; }
    
    public static JwkObject From(IDictionary<string, object> dict) => new()
    {
        kty = dict["kty"].ToString() ?? "",
        crv = dict["crv"].ToString() ?? "",
        x = dict["x"].ToString() ?? "",
        y = dict["y"].ToString() ?? ""
    };

    public ECDsa ToECDsa()
    {
        if (kty != "EC" || string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y))
            throw new ArgumentException("Invalid EC JWK components for ES256.");

        return ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = Jose.Base64Url.Decode(x),
                Y = Jose.Base64Url.Decode(y)
            }
        });
    }

    public bool Equals(JwkObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return kty == other.kty && crv == other.crv && x == other.x && y == other.y;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((JwkObject)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(kty, crv, x, y);
    }
}