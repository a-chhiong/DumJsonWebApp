using System.Net;
using Application.Interfaces;
using Application.Models;
using JoshAuthorization;
using JoshAuthorization.Extensions;
using JoshAuthorization.Objects;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Application.Services;

public interface ITokenService
{
    public Task<object> Login(string username, string password);
    public Task<object> Refresh(string refreshToken);
    public Task<object> Logout(string refreshToken);
}

public class TokenService : ITokenService
{
    private readonly HttpContext? _context;
    private readonly IFusionCache _cache;
    private readonly IDummyJsonAdapter _dummy;
    private readonly IJwtAuthService _jwtAuth;
    private readonly IOptions<JwtAuthEnvironmentOption> _jwtOptions;

    public TokenService( 
        IHttpContextAccessor accessor,
        IFusionCache cache,
        IDummyJsonAdapter dummy,
        IJwtAuthService  jwtAuth,
        IOptions<JwtAuthEnvironmentOption> options)
    {
        _context = accessor.HttpContext;
        _cache = cache;
        _dummy = dummy;
        _jwtAuth = jwtAuth;
        _jwtOptions = options;
    }
    
    public async Task<object> Login(string username, string password)
    {
        // 1. Extract DPoP header from the request (if there is)
        var jwk = _context?.GetItem<JwkObject>();
        
        // 2. Authenticate the user (standard DB check)
        var response = await _dummy.FetchUser(username);
        var user = response.users.FirstOrDefault(u => u.password == password);
        if (user == null)
            throw new BadHttpRequestException("Wrong Authentication", (int)HttpStatusCode.BadRequest);
        
        // 3. Create tokens bound to the client's public key
        var custom = new
        {
            UserId = user.id,
            LastName = user.lastName,
            FirstName = user.firstName,
        };
        var tokenWrapper = _jwtAuth.Create($"{user.id}", custom, jwk);
        
        // 4. Make it Stateful, stored on Server Cache or Persistence
        var newJti = tokenWrapper.Jti;
        var duration = _jwtOptions.Value.RefreshExpiryInSeconds + _jwtOptions.Value.ClockSkewInSeconds;
        await _cache.SetAsync(
            $"token-jti:{newJti}",
            new TokenCacheEntry
            {
                TokenType = tokenWrapper.TokenType,
                RefreshToken = tokenWrapper.RefreshToken,
                Subject = $"{user.id}",
                Custom = custom
            },
            TimeSpan.FromSeconds(duration));

        return new
        {
            TokenType = tokenWrapper.TokenType,
            AccessToken = tokenWrapper.AccessToken,
            RefreshToken = tokenWrapper.RefreshToken
        };
    }

    public async Task<object> Refresh(string refreshToken)
    {
        // 1. Validate the Refresh Token
        var refreshResult = await _jwtAuth.Validate(refreshToken);
        if (!refreshResult.IsSuccess)
            throw new BadHttpRequestException($"Refresh token is not valid: {refreshResult.Error}", (int)HttpStatusCode.Unauthorized);
        
        // 2. Comparing with Refresh Token Cache
        var comingJti = refreshResult.Data!.Token.jti;
        var tokenCacheEntry = await _cache.GetOrDefaultAsync<TokenCacheEntry?>($"token-jti:{comingJti}");
        var existedRefreshToken = tokenCacheEntry?.RefreshToken;
        if (string.IsNullOrEmpty(existedRefreshToken) 
            || !string.Equals(existedRefreshToken, refreshToken))
            throw new BadHttpRequestException("Refresh token is not matched!", (int)HttpStatusCode.Unauthorized);
        
        // 3. Validate the DPoP Token (if there is DPoP flow)
        var comingJwk = _context?.GetItem<JwkObject>();
        var existedTokenType = tokenCacheEntry?.TokenType;
        if (string.Equals(existedTokenType, "DPoP", StringComparison.OrdinalIgnoreCase))
        {   
            var tokenPayload = refreshResult.Data!.Token;
            var boundJkt = tokenPayload.cnf?.jkt;
            var calculatedJkt = CnfObject.From(comingJwk)?.jkt;
            
            // 3-1. We need the JWK from the DPoP Header to check against the Token's CNF
            if (string.IsNullOrEmpty(boundJkt) 
                || string.IsNullOrEmpty(calculatedJkt) 
                || !string.Equals(boundJkt, calculatedJkt, StringComparison.OrdinalIgnoreCase)) 
                throw new BadHttpRequestException("JWK is not matched!", (int)HttpStatusCode.Unauthorized);
        }

        // 4. Reconstruct custom claims and Issue New Token Pair
        var subject = tokenCacheEntry?.Subject;
        var custom = tokenCacheEntry?.Custom;
        var tokenWrapper = _jwtAuth.Create(subject, custom, comingJwk);
        
        // 5. Make it Stateful, stored on Server Cache or Persistence
        var newJti = tokenWrapper.Jti;
        var duration = _jwtOptions.Value.RefreshExpiryInSeconds + _jwtOptions.Value.ClockSkewInSeconds;
        await _cache.SetAsync(
            $"token-jti:{newJti}",
            new TokenCacheEntry
            {
                TokenType = tokenWrapper.TokenType,
                RefreshToken = tokenWrapper.RefreshToken,
                Subject = subject,
                Custom = custom
            },
            TimeSpan.FromSeconds(duration));
        await _cache.RemoveAsync($"token-jti:{comingJti}");
        
        return new
        {
            TokenType = tokenWrapper.TokenType,
            AccessToken = tokenWrapper.AccessToken,
            RefreshToken = tokenWrapper.RefreshToken
        };
    }
    
    public async Task<object> Logout(string refreshToken)
    {
        // 1. Validate the Refresh Token
        var refreshResult = await _jwtAuth.Validate(refreshToken);
        if (refreshResult.IsSuccess)
        {
            // 2. Remove the counterpart in Cache / Persistence (if there is)
            var jti = refreshResult.Data!.Token.jti;
            await _cache.RemoveAsync($"token-jti:{jti}");
        }
        return new { };
    }
}