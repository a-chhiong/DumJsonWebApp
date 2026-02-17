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
            cnf = CnfObject.From(clientJwk),
        };
        
        return new JwtWrapper
        {
            Jti = jti,
            TokenType =  clientJwk == null ? "Bearer" : "DPoP",
            AccessToken = Jose.JWT.Encode(payloadAccess, _privateKey, Jose.JwsAlgorithm.ES256),
            RefreshToken = Jose.JWT.Encode(payloadRefresh, _privateKey, Jose.JwsAlgorithm.ES256),
        };
    }

    public async Task<JwtAuthResult<TokenData>> Validate(string? token)
    {
        if (string.IsNullOrEmpty(token) || string.Equals(token, "null", StringComparison.OrdinalIgnoreCase)) 
            return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.MissingToken };
        
        try
        {
            var tokenPayload = Jose.JWT.Decode<TokenPayload>(token, _publicKey, Jose.JwsAlgorithm.ES256);
            
            // 1. Check Issuer
            if (tokenPayload.iss != _issuer)
                return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.InvalidIssuer };
            
            // .2 Check Audience
            if (tokenPayload.aud != _audience)
                return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.InvalidAudience };
            
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            // 3. Expiry logic
            if (now > tokenPayload.exp)
                return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.ExpiredToken };

            // 4. NBF logic
            if (tokenPayload.nbf.HasValue && now < tokenPayload.nbf.Value)
                return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.UntimelyToken };
            
            return new JwtAuthResult<TokenData> { IsSuccess = true, Data = new TokenData { Token = tokenPayload } };
        }
        catch
        {
            return new JwtAuthResult<TokenData> { IsSuccess = false, Error = JwtError.UnexpectedError };
        }
    }

    public async Task<JwtAuthResult<DPoPData>> Validate(string? token, HttpRequest request)
    {
        if (string.IsNullOrEmpty(token) || string.Equals(token, "null", StringComparison.OrdinalIgnoreCase)) 
            return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.MissingToken };

        try
        {
            var headers = Jose.JWT.Headers(token);

            // 0. Check typ header per RFC 9449
            if (headers["typ"]?.ToString() != "dpop+jwt")
                return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.InvalidToken };

            // 1. Safely map header jwk to our JwkObject
            JwkObject? jwkObj = null;
            if (headers.TryGetValue("jwk", out var jwk) && jwk is IDictionary<string, object> dict)
                jwkObj = JwkObject.From(dict);
            if (jwkObj == null)
                return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.InvalidToken };
            
            var dpopPayload = Jose.JWT.Decode<DPoPPayload>(token, jwkObj.ToECDsa(), Jose.JwsAlgorithm.ES256);
            
            // 2. Check HTM (Method)
            var expectedMethod = request.Method;
            if (!string.Equals(dpopPayload.htm, expectedMethod, StringComparison.OrdinalIgnoreCase))
                return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.InvalidHtm };

            // 3. Check HTU (URL)
            var expectedUrl = $"{_baseUrl}{request.Path.Value}";
            if (!string.Equals(dpopPayload.htu, expectedUrl, StringComparison.OrdinalIgnoreCase))
                return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.InvalidHtu };
            
            // 4. DPoP tokens are usually very short-lived; we check 'iat'
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(now - dpopPayload.iat) > _clockSkewInSeconds)
                return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.UnsyncToken };

            return new JwtAuthResult<DPoPData> { IsSuccess = true, Data = new DPoPData { Jwk = jwkObj, DPoP = dpopPayload } };
        }
        catch
        {
            return new JwtAuthResult<DPoPData> { IsSuccess = false, Error = JwtError.UnexpectedError };
        }
    }
    
    public async Task<JwtAuthResult<AccessData>> Validate(string? token, string? dpop, HttpRequest request)
    {   
        // 0. Mandatory Field for AccessToken and DPoPToken
        if (string.IsNullOrEmpty(token) || string.Equals(token, "null", StringComparison.OrdinalIgnoreCase))
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = JwtError.MissingToken };
        
        if (string.IsNullOrEmpty(dpop) || string.Equals(dpop, "null", StringComparison.OrdinalIgnoreCase))
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = JwtError.MissingToken };
        
        // 1. Validate the Access Token (The Bearer/DPoP part)
        var accessResult = await this.Validate(token);
        if (!accessResult.IsSuccess) 
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = accessResult.Error };
        
        // 2. Validate the DPoP Proof
        var dpopResult = await this.Validate(dpop, request);
        if (!dpopResult.IsSuccess) 
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = dpopResult.Error };
        
        // 3. PERFORM THE ATH CHECK (Access Token Hash)
        // RFC 9449: ath = base64url(sha256(ASCII(access_token)))
        var dpopPayload = dpopResult.Data!.DPoP;
        var hashBytes = SHA256.HashData(Encoding.ASCII.GetBytes(token));
        var expectedAth = Jose.Base64Url.Encode(hashBytes);
        if (string.IsNullOrEmpty(dpopPayload.ath) || !string.Equals(dpopPayload.ath, expectedAth))
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = JwtError.InvalidAth };
        
        // 4. Perform the Binding Check (Sender Constraining)
        var comingJwk = dpopResult.Data!.Jwk;
        var tokenPayload = accessResult.Data!.Token;
        var boundJkt = tokenPayload.cnf?.jkt;
        var calculatedJkt = CnfObject.From(comingJwk)?.jkt;
        
        // 4-1. We need the JWK from the DPoP Header to check against the Token's CNF
        if (string.IsNullOrEmpty(boundJkt) 
            || string.IsNullOrEmpty(calculatedJkt) 
            || !string.Equals(boundJkt, calculatedJkt, StringComparison.OrdinalIgnoreCase)) 
            return new JwtAuthResult<AccessData> { IsSuccess = false, Error = JwtError.InvalidBinding };

        // If everything passes, return the Normal Token payload
        return new JwtAuthResult<AccessData> { IsSuccess = true , Data = new AccessData { Jwk = comingJwk!, DPoP = dpopPayload!, Token = tokenPayload!} };
    }
}