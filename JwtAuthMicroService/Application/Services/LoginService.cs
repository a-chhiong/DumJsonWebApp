using Application.Interfaces;
using Application.Models;
using CrossCutting.Constants;
using Domain.ValueObjects.DummyJson;
using JoshAuthorization;
using JoshAuthorization.Models;
using JoshFileCache;
using Microsoft.AspNetCore.Http;
using ZiggyCreatures.Caching.Fusion;

namespace Application.Services;

public interface ITokenService
{
    public Task<object> Login(HttpRequest request, string username, string password);
    public Task<object> Refresh(HttpRequest request, string refreshToken);
    public Task<object> Logout(HttpRequest request);
}

public class TokenService : ITokenService
{
    private readonly IFusionCache _cache;
    private readonly IDummyJsonAdapter _dummy;
    private readonly IJwtAuthService _jwtAuth;

    public TokenService( 
        IFusionCache cache,
        IDummyJsonAdapter dummy,
        IJwtAuthService  jwtAuth)
    {
        _cache = cache;
        _dummy = dummy;
        _jwtAuth = jwtAuth;
    }
    
    public async Task<object> Login(HttpRequest request, string username, string password)
    {
        // 1. Extract DPoP header from the request (Optional but recommended for binding)
        var dpopToken = request.Headers[HttpHeaders.DPoP].ToString();
        
        // 2. Authenticate the user (standard DB check)
        var response = await _dummy.FetchUser(username);
        var user = response.users.FirstOrDefault(u => u.password == password);
        if (user == null)
        {
            throw new ArgumentException("Password is not matched!");
        }
        
        var dpopResult = await _jwtAuth.ValidateDPoP(dpopToken, request);
        var jwk = dpopResult.IsSuccess ? dpopResult.Data?.Jwk : null;
        
        // 4. Create tokens bound to the client's public key
        var tokenWrapper = _jwtAuth.Create(new JwtMetadata
        {
            Id = $"{user.id}",
            LastName = user.lastName,
            FirstName = user.firstName,
        }, jwk);
        
        await _cache.SetAsync(
            $"token-jti:{tokenWrapper.Jti}", 
            new TokenCacheEntry
            {
                jti = tokenWrapper.Jti,
                token_type = tokenWrapper.TokenType,
                refresh_token = tokenWrapper.RefreshToken,
                jwk = jwk,
            }, 
            TimeSpan.FromSeconds(JwtAuthConstant.REFRESH_EXPIRY_IN_SECONDS + JwtAuthConstant.CLOCK_SKEW_IN_SECONDS));

        return new
        {
            token_type = tokenWrapper.TokenType,
            access_token = tokenWrapper.AccessToken,
            refresh_token = tokenWrapper.RefreshToken
        };
    }

    public async Task<object> Refresh(HttpRequest request, string refreshToken)
    {
        var refreshResult = await _jwtAuth.ValidateRefresh(refreshToken);
        if (!refreshResult.IsSuccess)
        {
            throw new BadHttpRequestException("Refresh token is not valid!");
        }

        var jti = refreshResult.Data?.Payload?.jti;
        var tokenCacheEntry = await _cache.GetOrDefaultAsync<TokenCacheEntry?>($"token-jti:{jti}");
        var entryRefreshToken = tokenCacheEntry?.refresh_token;
        if (string.IsNullOrEmpty(entryRefreshToken))
        {
            throw new BadHttpRequestException("Refresh token is not found!");
        }

        if (!string.Equals(entryRefreshToken, refreshToken))
        {
            throw new BadHttpRequestException("Refresh token is not matched!");
        }
        
        var entryTokenType = tokenCacheEntry?.token_type;
        var entryJwk = tokenCacheEntry?.jwk;   
        var dpopToken = request.Headers[HttpHeaders.DPoP].ToString();
        var dpopResult = await _jwtAuth.ValidateDPoP(dpopToken, request);
        if (string.Equals(entryTokenType, "DPoP", StringComparison.OrdinalIgnoreCase))
        {
            if (!dpopResult.IsSuccess 
                || entryJwk == null 
                || dpopResult.Data?.Jwk?.Equals(entryJwk) != true)
            {
                throw new BadHttpRequestException("Refresh token is not bound!");
            }
        }

        var metadata = refreshResult.Data?.Payload?.meta;
        if (metadata == null)
        {
            throw new BadHttpRequestException("Metadata is not found!");
        }
        var tokenWrapper = _jwtAuth.Create(new JwtMetadata
        {
            Id = metadata.Id,
            LastName = metadata.LastName,
            FirstName = metadata.FirstName,
        }, entryJwk);
        
        return new
        {
            token_type = tokenWrapper.TokenType,
            access_token = tokenWrapper.AccessToken,
            refresh_token = tokenWrapper.RefreshToken
        };
    }
    
    public Task<object> Logout(HttpRequest request)
    {   
        throw new NotImplementedException();
    }
}