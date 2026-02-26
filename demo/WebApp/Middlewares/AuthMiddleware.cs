using System.Net;
using JoshAuthorization;
using JoshAuthorization.Enums;
using JoshAuthorization.Extensions;
using JoshAuthorization.Models;
using JoshAuthorization.Objects;
using Microsoft.AspNetCore.Authorization;
using ZiggyCreatures.Caching.Fusion;

namespace WebApp.Middlewares;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IJwtAuthService _jwtAuth;
    private readonly IFusionCache _cache;

    public AuthMiddleware(
        RequestDelegate next,
        IJwtAuthService jwtAuth,
        IFusionCache cache)
    {
        _next = next;
        _jwtAuth = jwtAuth;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var isAnonymous = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null;
        
        if (endpoint == null)
        {
            throw new BadHttpRequestException("No Available Endpoint", (int)HttpStatusCode.NotFound);
        }
        
        if (isAnonymous)
        {
            // Proceed unless DPoP was provided and was INVALID
            if (await HandleAnonymousDPoP(context))
            {
                await _next(context);
            }
        }
        else
        {
            // Authorization: Proceed only if VALIDATED
            if (await HandleAuthorization(context))
            {
                await _next(context);
            }
        }
    }

    private async Task<bool> HandleAnonymousDPoP(HttpContext context)
    {
        // 1. Try to get the DPoP Proof header (Optional depending on scheme)
        var (_, _, dpopToken) = context.GetAuthScheme();

        // 2. Skip it if there ISN'T!
        if (string.IsNullOrEmpty(dpopToken))
            return true;
        
        // 3. Validate DPoP if there IS!
        var dpopResult = await _jwtAuth.Validate(dpopToken, context.Request);
        
        if (!dpopResult.IsSuccess)
        {
            throw new BadHttpRequestException($"Auth failed: {dpopResult.Error}", (int)HttpStatusCode.BadRequest);
        }
        
        var jwkObject = dpopResult.Data!.Jwk;
        var dpopPayload = dpopResult.Data!.DPoP;
        var jti = dpopPayload.jti;
        
        // 4. DPoP Anti-Replay Attack
        if(true == await _cache.GetOrDefaultAsync<bool?>($"dpop-jti:{jti}"))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync($"Auth failed: Replayed DPoP");
            return false;
        }
        await _cache.SetAsync($"dpop-jti:{jti}", true, TimeSpan.FromMinutes(10));
        
        context.SetItem(dpopPayload);
        context.SetItem(jwkObject);
            
        return true;
    }

    private async Task<bool> HandleAuthorization(HttpContext context)
    {
        // 0. Have Request Context Ready...
        var request = context.Request;
        
        // 1. Use a tuple to get the scheme and token cleanly
        var (scheme, authToken, dpopToken) = context.GetAuthScheme();

        // 2. Authorization Flow
        IJwtAuthResult<IJwtResultData> result = scheme switch
        {
            JwtAuthScheme.DPoP => await _jwtAuth.Validate(authToken, dpopToken, request),
            JwtAuthScheme.Bearer => await _jwtAuth.Validate(authToken),
            _ => new JwtAuthResult<IJwtResultData> { IsSuccess = false, Error = JwtError.MissingScheme }
        };

        if (!result.IsSuccess)
        {
            throw new BadHttpRequestException($"Auth failed: {result.Error}", (int)HttpStatusCode.Unauthorized);
        }

        if (scheme == JwtAuthScheme.DPoP && result is JwtAuthResult<AccessData> authResult)
        {
            var jwkObject = authResult.Data!.Jwk;
            var dpopPayload = authResult.Data!.DPoP;
            var jti = dpopPayload.jti;
            var tokenPayload = authResult.Data!.Token;
            
            // DPoP Anti-Replay Attack
            if(true == await _cache.GetOrDefaultAsync<bool?>($"dpop-jti:{jti}"))
            {
                throw new BadHttpRequestException($"Auth failed: Replayed DPoP", (int)HttpStatusCode.Forbidden);
            }
            await _cache.SetAsync($"dpop-jti:{jti}", true, TimeSpan.FromMinutes(10));
            
            context.SetItem(jwkObject);
            context.SetItem(dpopPayload);
            context.SetItem(tokenPayload);
        }
        else if (scheme == JwtAuthScheme.Bearer && result is JwtAuthResult<TokenData> tokenResult)
        {
            var tokenPayload = tokenResult.Data!.Token;
            
            context.SetItem(tokenPayload);
        }

        return true;
    }
}