using System.Security.Cryptography;

namespace JoshAuthorization.Extensions;

public static class StringExtensions
{
    public static ECDsa ToPublicKeyFromHex(this string hex)
    {
        byte[] keyBytes = HexToBytes(hex);

        if (keyBytes.Length != 65 || keyBytes[0] != 0x04)
            throw new ArgumentException("Not a valid uncompressed P-256 EC point");

        byte[] x = keyBytes.AsSpan(1, 32).ToArray();
        byte[] y = keyBytes.AsSpan(33, 32).ToArray();

        var ecParams = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = x, Y = y }
        };

        return ECDsa.Create(ecParams);
    }
    
    public static ECDsa ToPrivateKeyFromHex(this string hex)
    {
        byte[] keyBytes = HexToBytes(hex);

        if (keyBytes.Length != 32)
            throw new ArgumentException("Expected 32 bytes for P-256 private key");

        var ecParams = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = keyBytes
        };

        return ECDsa.Create(ecParams);
    }

    private static byte[] HexToBytes(string hex)
    {
        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have even length");

        return Enumerable.Range(0, hex.Length / 2)
            .Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16))
            .ToArray();
    }
}