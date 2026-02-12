using System.Security.Cryptography;
using System.Text;
using JoshAuthorization.Models;
using JoshAuthorization.Objects;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace JoshAuthorization;

public class JwtAuthService : IJwtAuthService, IDisposable
{
    private readonly string _baseUrl;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly RSA _publicRsa;
    private readonly RSA _privateRsa;

    public JwtAuthService(
        IOptions<JwtAuthEnvironmentOption> options)
    {
        _baseUrl = options.Value.BaseUrl.TrimEnd('/');
        _issuer = options.Value.Issuer;
        _audience = options.Value.Audience;
        
        // Get the folder where the DLLs are running
        var baseDir = AppContext.BaseDirectory;

        // Combine with the paths from config (e.g., "RsaKey/PublicKey.pem")
        var publicPath = Path.Combine(baseDir, options.Value.PublicKey);
        var privatePath = Path.Combine(baseDir, options.Value.PrivateKey);
        
        // Validate existence before attempting to read (The "Fail Fast" principle)
        if (!File.Exists(publicPath)) throw new FileNotFoundException($"Public key not found at {publicPath}");
        if (!File.Exists(privatePath)) throw new FileNotFoundException($"Private key not found at {privatePath}");

        _publicRsa = RSA.Create();
        _publicRsa.ImportFromPem(File.ReadAllText(publicPath));

        _privateRsa = RSA.Create();
        _privateRsa.ImportFromPem(File.ReadAllText(privatePath));
    }

    #region Private Helpers

    private string CreateJti()
    {
        // Using Base64Url to keep the JTI header-friendly
        var input = $"{_issuer}|{Guid.NewGuid():N}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Jose.Base64Url.Encode(hashBytes);
    }

    #endregion

    public JwtWrapper Create(JwtMetadata metadata, JwkObject? clientJwk = null)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var jti = CreateJti();

        var payloadAccess = new TokenPayload
        {
            iss = _issuer,
            aud = _audience,
            sub = metadata.Id,
            iat = now,
            exp = now + JwtAuthConstant.ACCESS_EXPIRY_IN_SECONDS,
            jti = jti,
            cnf = CnfObject.From(clientJwk),
            meta = metadata
        };

        var payloadRefresh = new TokenPayload
        {
            iss = _issuer,
            aud = _audience,
            sub = metadata.Id,
            iat = now,
            exp = now + JwtAuthConstant.REFRESH_EXPIRY_IN_SECONDS,
            nbf = now + JwtAuthConstant.REFRESH_NOT_BEFORE_IN_SECONDS,
            jti = jti,
        };

        return new JwtWrapper
        {
            Jti = jti,
            TokenType =  clientJwk == null ? "Bearer" : "DPoP",
            AccessToken = Jose.JWT.Encode(payloadAccess, _privateRsa, Jose.JwsAlgorithm.RS256),
            RefreshToken = Jose.JWT.Encode(payloadRefresh, _privateRsa, Jose.JwsAlgorithm.RS256),
        };
    }

    public async Task<JwtAuthResult<TokenData>> ValidateAccess(string? token)
    {
        return await Validate(token);
    }
    
    public async Task<JwtAuthResult<TokenData>> ValidateRefresh(string? token)
    {
        return await Validate(token, true);
    }

    private async Task<JwtAuthResult<TokenData>> Validate(string? token, bool isNotBefore = false)
    {
        if (string.IsNullOrEmpty(token)) 
            return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.InvalidToken };

        try
        {
            var payload = Jose.JWT.Decode<TokenPayload>(token, _publicRsa, Jose.JwsAlgorithm.RS256);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (payload.iss != _issuer) return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.InvalidIssuer };
            if (payload.aud != _audience) return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.InvalidAudience };

            // Expiry logic
            if (now > payload.exp)
                return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.ExpiredToken };

            // NBF logic
            if (isNotBefore)
            {
                if (!payload.nbf.HasValue)
                    return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.InvalidNBF };
                if (now < payload.nbf.Value)
                    return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.UntimelyToken };
            }

            return new JwtAuthResult<TokenData> { IsSuccess = true, Data = new TokenData { Payload = payload } };
        }
        catch
        {
            return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.UnexpectedError };
        }
    }

    public async Task<JwtAuthResult<DPoPData>> ValidateDPoP(string? token, HttpRequest request)
    {
        if (string.IsNullOrEmpty(token)) 
            return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.InvalidToken };

        var expectedMethod = request.Method;
        var expectedUrl = $"{_baseUrl}{request.Path.Value}";

        try
        {
            var headers = Jose.JWT.Headers(token);

            // Check typ header per RFC 9449
            if (headers["typ"]?.ToString() != "dpop+jwt")
                return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.InvalidToken };

            JwkObject jwkObj = null;
            // Safely map header jwk to our JwkObject
            if (headers.TryGetValue("jwk", out var jwk) && jwk is IDictionary<string, object> dict)
            {
                jwkObj = JwkObject.From(dict);
            }
            if (jwkObj == null) return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.InvalidToken };
            
            var payload = Jose.JWT.Decode<DPoPPayload>(token, jwkObj.ToECDsa(), Jose.JwsAlgorithm.ES256);

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            if (!string.Equals(payload.htm, expectedMethod, StringComparison.OrdinalIgnoreCase))
                return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.InvalidHtm };

            if (!string.Equals(payload.htu, expectedUrl, StringComparison.OrdinalIgnoreCase))
                return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.InvalidHtu };

            // DPoP tokens are usually very short-lived; we check 'iat'
            if (Math.Abs(now - payload.iat) > JwtAuthConstant.CLOCK_SKEW_IN_SECONDS)
                return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.UnsyncToken };

            return new JwtAuthResult<DPoPData> { IsSuccess = true, Data = new DPoPData { Payload = payload, Jwk = jwkObj } };
        }
        catch
        {
            return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.UnexpectedError };
        }
    }
    
    public async Task<JwtAuthResult<AccessData>> ValidateAccess(string? token, string? dpop, HttpRequest request)
    {   
        // 0. Mandatory Field for AccessToken and DPoPToken
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(dpop))
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = JwtError.MissingToken };
        
        // 1. Validate the Access Token (The Bearer/DPoP part)
        var accessResult = await this.ValidateAccess(token);
        if (!accessResult.IsSuccess) 
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = accessResult.Error };
        
        // 2. Validate the DPoP Proof
        var dpopResult = await this.ValidateDPoP(dpop, request);
        if (!dpopResult.IsSuccess) 
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = dpopResult.Error };
    
        // 3. PERFORM THE ATH CHECK (Access Token Hash)
        // RFC 9449: ath = base64url(sha256(ASCII(access_token)))
        var dpopPayload = dpopResult.Data?.Payload;
        var hashBytes = SHA256.HashData(Encoding.ASCII.GetBytes(token));
        var expectedAth = Jose.Base64Url.Encode(hashBytes);
        if (string.IsNullOrEmpty(dpopPayload?.ath) || !string.Equals(dpopPayload?.ath, expectedAth))
        {
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = JwtError.InvalidAth };
        }

        // 4. Perform the Binding Check (Sender Constraining)
        var tokenPayload = accessResult.Data?.Payload;
        var boundJkt = tokenPayload?.cnf?.jkt;
        var calculatedJkt = CnfObject.From(dpopResult.Data?.Jwk)?.jkt;
        // We need the JWK from the DPoP Header to check against the Refresh Token's CNF
        if (string.IsNullOrEmpty(boundJkt) 
            || string.IsNullOrEmpty(calculatedJkt) 
            || !string.Equals(boundJkt, calculatedJkt, StringComparison.OrdinalIgnoreCase)) 
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = JwtError.InvalidBinding };

        // If everything passes, return the Normal Token payload
        return new JwtAuthResult<AccessData> { IsSuccess = true , Data = new AccessData { Payload = tokenPayload, DPoPJti = dpopPayload?.jti } };
    }

    public void Dispose()
    {
        _publicRsa.Dispose();
        _privateRsa.Dispose();
    }
}