using System.Net;
using Application.Interfaces;
using Application.Models;
using CrossCutting.Constants;
using CrossCutting.Extensions;
using Domain.ValueObjects.DummyJson;
using JoshAuthorization;
using JoshAuthorization.Models;
using JoshAuthorization.Objects;
using JoshFileCache;
using Microsoft.AspNetCore.Http;
using ZiggyCreatures.Caching.Fusion;

namespace Application.Services;

public interface ITokenService
{
    public Task<object> Login(HttpRequest request, string username, string password);
    public Task<object> Refresh(HttpRequest request, string refreshToken);
    public Task<object> Logout(HttpContext context);
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
        // 1. Extract DPoP header from the request (if there is)
        var dpopToken = request.Headers[HttpHeaders.DPoP].ToString();
        var dpopResult = await _jwtAuth.ValidateDPoP(dpopToken, request);
        var jwk = dpopResult.IsSuccess ? dpopResult.Data?.Jwk : null;
        
        // 2. Authenticate the user (standard DB check)
        var response = await _dummy.FetchUser(username);
        var user = response.users.FirstOrDefault(u => u.password == password);
        if (user == null)
        {
            throw new BadHttpRequestException("Wrong Authentication", (int)HttpStatusCode.BadRequest);
        }
        
        // 3. Create tokens bound to the client's public key
        var metadata = new JwtMetadata()
        {
            Id = $"{user.id}",
            LastName = user.lastName,
            FirstName = user.firstName,
        };
        var tokenWrapper = _jwtAuth.Create(metadata, jwk);
        
        // 4. Make it Stateful, stored on Server Cache or Persistence
        await _cache.SetAsync(
            $"token-jti:{tokenWrapper.Jti}", 
            new TokenCacheEntry
            {
                jti = tokenWrapper.Jti,
                token_type = tokenWrapper.TokenType,
                refresh_token = tokenWrapper.RefreshToken,
                jwk = jwk,
                meta = metadata
            }, 
            TimeSpan.FromSeconds(JwtAuthConstant.REFRESH_EXPIRY_IN_SECONDS + JwtAuthConstant.CLOCK_SKEW_IN_SECONDS));

        return new
        {
            TokenType = tokenWrapper.TokenType,
            AccessToken = tokenWrapper.AccessToken,
            RefreshToken = tokenWrapper.RefreshToken
        };
    }

    public async Task<object> Refresh(HttpRequest request, string refreshToken)
    {
        // 1. Validate the Refresh Token
        var refreshResult = await _jwtAuth.ValidateRefresh(refreshToken);
        if (!refreshResult.IsSuccess)
        {
            throw new BadHttpRequestException($"Refresh token is not valid: {refreshResult.Error}", (int)HttpStatusCode.Unauthorized);
        }

        // 2. Comparing with Refresh Token Cache
        var jti = refreshResult.Data?.Payload?.jti;
        var tokenCacheEntry = await _cache.GetOrDefaultAsync<TokenCacheEntry?>($"token-jti:{jti}");
        var existedRefreshToken = tokenCacheEntry?.refresh_token;
        if (string.IsNullOrEmpty(existedRefreshToken))
        {
            throw new BadHttpRequestException("Refresh token is not found!", (int)HttpStatusCode.Unauthorized);
        }
        if (!string.Equals(existedRefreshToken, refreshToken))
        {
            throw new BadHttpRequestException("Refresh token is not matched!", (int)HttpStatusCode.Unauthorized);
        }
        
        // 3. Validate the DPoP Token (if there is DPoP flow)
        var existedTokenType = tokenCacheEntry?.token_type;
        var existedJwk = tokenCacheEntry?.jwk;   
        if (string.Equals(existedTokenType, "DPoP", StringComparison.OrdinalIgnoreCase))
        {
            var dpopToken = request.Headers[HttpHeaders.DPoP].ToString();
            var dpopResult = await _jwtAuth.ValidateDPoP(dpopToken, request);
            var jwk = dpopResult.Data?.Jwk;
            if (!dpopResult.IsSuccess || jwk == null)
            {
                throw new BadHttpRequestException($"DPoP token is not valid: {dpopResult.Error}", (int)HttpStatusCode.Unauthorized);
            }
            if (existedJwk == null)
            {
                throw new BadHttpRequestException("JWK is not found!", (int)HttpStatusCode.Unauthorized);
            }
            if (!jwk.Equals(existedJwk))
            {
                throw new BadHttpRequestException("JWK is not matched!", (int)HttpStatusCode.Unauthorized);
            }
        }

        // 4. Reconstruct MetaData and Issue New Token Pair
        var existedMetadata = tokenCacheEntry?.meta;
        if (existedMetadata == null)
        {
            throw new BadHttpRequestException("Metadata is not found!", (int)HttpStatusCode.Unauthorized);
        }
        var tokenWrapper = _jwtAuth.Create(new JwtMetadata
        {
            Id = existedMetadata.Id,
            LastName = existedMetadata.LastName,
            FirstName = existedMetadata.FirstName,
        }, existedJwk);
        
        return new
        {
            TokenType = tokenWrapper.TokenType,
            AccessToken = tokenWrapper.AccessToken,
            RefreshToken = tokenWrapper.RefreshToken
        };
    }
    
    public async Task<object> Logout(HttpContext context)
    {
        // Remove the counterpart in Cache / Persistence (if there is)
        var jti = context.GetItem<TokenPayload>()?.jti;
        if (!string.IsNullOrEmpty(jti))
        {
            await _cache.RemoveAsync($"token-jti:{jti}");   
        }
        return new { };
    }
}