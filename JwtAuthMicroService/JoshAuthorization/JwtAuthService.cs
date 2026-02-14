using System.Security.Cryptography;
using System.Text;
using JoshAuthorization.Extensions;
using JoshAuthorization.Models;
using JoshAuthorization.Objects;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace JoshAuthorization;

public class JwtAuthService : IJwtAuthService
{
    private readonly string _baseUrl;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly ECDsa _privateKey;
    private readonly ECDsa _publicKey;
    private readonly long _accessExpiryInSeconds;
    private readonly long _refreshExpiryInSeconds;
    private readonly long _refreshNotBeforeInSeconds;
    private readonly long _clockSkewInSeconds;

    #region Private Helpers

    public JwtAuthService(
        IOptions<JwtAuthEnvironmentOption> options)
    {
        _baseUrl = options.Value.BaseUrl.TrimEnd('/');
        _issuer = options.Value.Issuer;
        _audience = options.Value.Audience;
        _privateKey =  options.Value.PrivateKey.ToPrivateKeyFromHex();
        _publicKey = options.Value.PublicKey.ToPublicKeyFromHex();
        _accessExpiryInSeconds = options.Value.AccessExpiryInSeconds;
        _refreshExpiryInSeconds = options.Value.RefreshExpiryInSeconds;
        _refreshNotBeforeInSeconds = options.Value.RefreshNotBeforeInSeconds;
        _clockSkewInSeconds = options.Value.ClockSkewInSeconds;
    }

    private string CreateJti()
    {
        // Using Base64Url to keep the JTI header-friendly
        var input = $"{_issuer}|{_audience}|{Guid.NewGuid():N}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Jose.Base64Url.Encode(hashBytes);
    }

    #endregion

    public JwtWrapper Create(string? subject, object? custom, JwkObject? clientJwk = null)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var jti = CreateJti();

        var payloadAccess = new TokenPayload
        {
            jti = jti,
            iss = _issuer,
            aud = _audience,
            exp = now + _accessExpiryInSeconds,
            iat = now,
            sub = subject,
            custom = custom,
            cnf = CnfObject.From(clientJwk),
        };

        var payloadRefresh = new TokenPayload
        {
            jti = jti,
            iss = _issuer,
            aud = _audience,
            exp = now + _refreshExpiryInSeconds,
            iat = now,
            nbf = now + _refreshNotBeforeInSeconds,
        };
        
        return new JwtWrapper
        {
            Jti = jti,
            TokenType =  clientJwk == null ? "Bearer" : "DPoP",
            AccessToken = Jose.JWT.Encode(payloadAccess, _privateKey, Jose.JwsAlgorithm.ES256),
            RefreshToken = Jose.JWT.Encode(payloadRefresh, _privateKey, Jose.JwsAlgorithm.ES256),
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
            var tokenPayload = Jose.JWT.Decode<TokenPayload>(token, _publicKey, Jose.JwsAlgorithm.ES256);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (tokenPayload.iss != _issuer) return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.InvalidIssuer };
            if (tokenPayload.aud != _audience) return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.InvalidAudience };

            // Expiry logic
            if (now > tokenPayload.exp)
                return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.ExpiredToken };

            // NBF logic
            if (isNotBefore)
            {
                if (!tokenPayload.nbf.HasValue)
                    return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.InvalidNBF };
                if (now < tokenPayload.nbf.Value)
                    return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.UntimelyToken };
            }

            return new JwtAuthResult<TokenData> { IsSuccess = true, Data = new TokenData { Token = tokenPayload } };
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

            JwkObject? jwkObj = null;
            // Safely map header jwk to our JwkObject
            if (headers.TryGetValue("jwk", out var jwk) && jwk is IDictionary<string, object> dict)
            {
                jwkObj = JwkObject.From(dict);
            }
            if (jwkObj == null) return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.InvalidToken };
            
            var dpopPayload = Jose.JWT.Decode<DPoPPayload>(token, jwkObj.ToECDsa(), Jose.JwsAlgorithm.ES256);

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            if (!string.Equals(dpopPayload.htm, expectedMethod, StringComparison.OrdinalIgnoreCase))
                return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.InvalidHtm };

            if (!string.Equals(dpopPayload.htu, expectedUrl, StringComparison.OrdinalIgnoreCase))
                return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.InvalidHtu };

            // DPoP tokens are usually very short-lived; we check 'iat'
            if (Math.Abs(now - dpopPayload.iat) > _clockSkewInSeconds)
                return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.UnsyncToken };

            return new JwtAuthResult<DPoPData> { IsSuccess = true, Data = new DPoPData { Jwk = jwkObj, DPoP = dpopPayload } };
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
        var jwk = dpopResult.Data?.Jwk;
    
        // 3. PERFORM THE ATH CHECK (Access Token Hash)
        // RFC 9449: ath = base64url(sha256(ASCII(access_token)))
        var dpopPayload = dpopResult.Data?.DPoP;
        var hashBytes = SHA256.HashData(Encoding.ASCII.GetBytes(token));
        var expectedAth = Jose.Base64Url.Encode(hashBytes);
        if (string.IsNullOrEmpty(dpopPayload?.ath) || !string.Equals(dpopPayload?.ath, expectedAth))
        {
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = JwtError.InvalidAth };
        }

        // 4. Perform the Binding Check (Sender Constraining)
        var tokenPayload = accessResult.Data?.Token;
        var boundJkt = tokenPayload?.cnf?.jkt;
        var calculatedJkt = CnfObject.From(dpopResult.Data?.Jwk)?.jkt;
        // We need the JWK from the DPoP Header to check against the Refresh Token's CNF
        if (string.IsNullOrEmpty(boundJkt) 
            || string.IsNullOrEmpty(calculatedJkt) 
            || !string.Equals(boundJkt, calculatedJkt, StringComparison.OrdinalIgnoreCase)) 
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = JwtError.InvalidBinding };

        // If everything passes, return the Normal Token payload
        return new JwtAuthResult<AccessData> { IsSuccess = true , Data = new AccessData { Jwk = jwk!, DPoP = dpopPayload!, Token = tokenPayload!} };
    }
}