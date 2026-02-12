using System.Net;
using System.Security.Claims;
using System.Text.Json;
using CrossCutting.Constants;
using Application.Interfaces;
using CrossCutting.Extensions;
using JoshAuthorization;
using JoshAuthorization.Models;
using JoshAuthorization.Objects;
using JoshFileCache;
using ZiggyCreatures.Caching.Fusion;

namespace WebAPI.Middlewares;

using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

public class JwtAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IJwtAuthService _jwtAuth;
    private readonly IFusionCache _cache;

    public JwtAuthMiddleware(
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
        var request = context.Request;
        var headers = request.Headers;
        var dpopToken = headers.TryGetValue(HttpHeaders.DPoP, out var dpopHeader) 
            ? dpopHeader.ToString() : string.Empty;

        // 2. Skip it if there ISN'T!
        if (string.IsNullOrEmpty(dpopToken))
            return true;
        
        // 3. Validate it if there IS!
        var dpopResult = await _jwtAuth.ValidateDPoP(dpopToken, request);
        if (dpopResult.IsSuccess)
        {
            context.Items[HttpHeaders.DPoP] = dpopToken;
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
        // Use a tuple to get the scheme and token cleanly
        var (scheme, authToken, dpopToken) = GetAuthContext(context.Request.Headers);

        // Authorization Flow
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
            var jti = authResult?.Data?.DPoPJti;
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

            var payload = authResult?.Data?.Payload;
            if (payload == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync($"Auth failed: Missing Payload");
                return false;
            }
            await _cache.SetAsync($"dpop-jti:{jti}", true, TimeSpan.FromMinutes(10));
            context.SetItem(payload);
        }
        else if (scheme == JwtAuthScheme.Bearer)
        {
            var tokenResult = result as JwtAuthResult<TokenData>;
            var payload = tokenResult?.Data?.Payload;
            if (payload == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync($"Auth failed: Missing Payload");
                return false;
            }
            context.SetItem(payload);
        }

        return true;
    }
    
    private (JwtAuthScheme Scheme, string AuthToken, string DpopToken) GetAuthContext(IHeaderDictionary headers)
    {
        // 1. Try to get the DPoP Proof header (Optional depending on scheme)
        var dpopToken = headers.TryGetValue(HttpHeaders.DPoP, out var dpopHeader) 
            ? dpopHeader.ToString() : string.Empty;

        // 2. Try to get the Authorization header
        if (!headers.TryGetValue(HttpHeaders.Authorization, out var authHeader))
            return (JwtAuthScheme.None, string.Empty, dpopToken);
        
        var authStr = authHeader.ToString();

        // 3. Resolve Scheme and Token
        if (authStr.StartsWith("DPoP ", StringComparison.OrdinalIgnoreCase))
            return (JwtAuthScheme.DPoP, authStr["DPoP ".Length..], dpopToken);
        
        if (authStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return (JwtAuthScheme.Bearer, authStr["Bearer ".Length..], dpopToken);

        return (JwtAuthScheme.None, string.Empty, dpopToken);
    }
}