using JoshAuthorization.Enums;
using Microsoft.AspNetCore.Http;

namespace JoshAuthorization.Extensions;

public static class HttpContextExtensions
{
    // Generic setter
    public static void SetItem<T>(this HttpContext context, T value)
    {
        context.Items[typeof(T)] = value;
    }

    // Generic getter
    public static T? GetItem<T>(this HttpContext context)
    {
        if (context.Items.TryGetValue(typeof(T), out var value) && value is T typed)
        {
            return typed;
        }
        return default;
    }
    
    public static (JwtAuthScheme Scheme, string AuthToken, string DpopToken) GetAuthScheme(this HttpContext context)
    {
        var headers = context.Request.Headers;
        
        // 1. Try to get the DPoP Proof header (Optional depending on scheme)
        var dpop = headers.TryGetValue("DPoP", out var dpopHeader) 
            ? dpopHeader.ToString() : string.Empty;

        // 2. Try to get the Authorization header
        var authorization = headers.TryGetValue("Authorization", out var authHeader) 
            ? authHeader.ToString() : string.Empty;
        if (string.IsNullOrEmpty(authorization))
            return (JwtAuthScheme.None, string.Empty, dpop);
        
        // 3. Resolve Scheme and Token
        if (authorization.StartsWith("DPoP ", StringComparison.OrdinalIgnoreCase))
            return (JwtAuthScheme.DPoP, authorization["DPoP ".Length..], dpop);
        
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return (JwtAuthScheme.Bearer, authorization["Bearer ".Length..], dpop);

        return (JwtAuthScheme.None, string.Empty, dpop);
    }
}