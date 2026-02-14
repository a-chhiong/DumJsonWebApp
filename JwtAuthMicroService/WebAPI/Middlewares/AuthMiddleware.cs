using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using JoshAuthorization;
using JoshAuthorization.Enums;
using JoshAuthorization.Extensions;
using JoshAuthorization.Models;
using JoshAuthorization.Objects;
using Microsoft.AspNetCore.Authorization;
using ZiggyCreatures.Caching.Fusion;

namespace WebAPI.Middlewares;

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
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }
        else if (isAnonymous)
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
        var (scheme, authToken, dpopToken) = context.GetAuthScheme();

        // 2. Skip it if there ISN'T!
        if (string.IsNullOrEmpty(dpopToken))
            return true;
        
        // 3. Validate it if there IS!
        var dpopResult = await _jwtAuth.ValidateDPoP(dpopToken, context.Request);
        if (dpopResult.IsSuccess)
        {
            var dpopPayload = dpopResult.Data?.DPoP;
            var jwkObject = dpopResult.Data?.Jwk;
            context.SetItem(dpopPayload);
            context.SetItem(jwkObject);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync($"DPoP Validation failed: {dpopResult.Error}");
            return false;
        }
        
        return true;
    }

    private async Task<bool> HandleAuthorization(HttpContext context)
    {
        // 1. Use a tuple to get the scheme and token cleanly
        var (scheme, authToken, dpopToken) = context.GetAuthScheme();

        // 2. Authorization Flow
        if (scheme == JwtAuthScheme.None)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync($"Auth failed: Missing Scheme");
            return false;
        }

        var request = context.Request;
        
        // Call the service based on the resolved scheme
        IJwtAuthResult<IJwtResultData> result = scheme switch
        {
            JwtAuthScheme.DPoP => await _jwtAuth.ValidateAccess(authToken, dpopToken, request),
            JwtAuthScheme.Bearer => await _jwtAuth.ValidateAccess(authToken),
            _ => new JwtAuthResult<IJwtResultData> { IsSuccess = false, Error = JwtError.MissingScheme }
        };

        if (!result.IsSuccess)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync($"Auth failed: {result.Error}");
            return false;
        }

        // For DPoP replay Attack
        if (scheme == JwtAuthScheme.DPoP)
        {
            var authResult = result as JwtAuthResult<AccessData>;
            var dpopPayload = authResult?.Data?.DPoP;
            var jti = dpopPayload?.jti;
            if (string.IsNullOrEmpty(jti))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync($"Auth failed: No Valid DPoP jti");
                return false;
            }
            var data = await _cache.GetOrDefaultAsync<bool?>($"dpop-jti:{jti}");
            if (data != null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync($"Auth failed: Replayed DPoP");
                return false;
            }

            var jwkObject = authResult?.Data?.Jwk;
            if (jwkObject == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync($"Auth failed: Missing Jwk");
                return false;
            }
            var tokenPayload = authResult?.Data?.Token;
            if (tokenPayload == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync($"Auth failed: Missing Payload");
                return false;
            }
            await _cache.SetAsync($"dpop-jti:{jti}", true, TimeSpan.FromMinutes(10));
            context.SetItem(jwkObject);
            context.SetItem(dpopPayload);
            context.SetItem(tokenPayload);
        }
        else if (scheme == JwtAuthScheme.Bearer)
        {
            var tokenResult = result as JwtAuthResult<TokenData>;
            var tokenPayload = tokenResult?.Data?.Token;
            if (tokenPayload == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync($"Auth failed: Missing Payload");
                return false;
            }
            context.SetItem(tokenPayload);
        }

        return true;
    }
}