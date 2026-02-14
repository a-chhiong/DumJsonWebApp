using JoshAuthorization.Models;
using JoshAuthorization.Objects;
using Microsoft.AspNetCore.Http;

namespace JoshAuthorization;

public interface IJwtAuthService
{
    /// <summary>
    /// Generates a pair of Access and Refresh tokens. 
    /// If clientJwk is provided, the Access Token will be DPoP-bound (Sender Constrained).
    /// </summary>
    JwtWrapper Create(string? subject, object? custom, JwkObject? clientJwk = null);

    /// <summary>
    /// Validates a standard JWT (Access Token / Refresh Token).
    /// </summary>
    /// <param name="token">The JWT string.</param>
    /// <param name="isNotBefore">Force NBF checking</param>
    Task<JwtAuthResult<TokenData>> Validate(string? token, bool isNotBefore = false);
    
    /// <summary>
    /// Validates initial DPoP Proof
    /// </summary>
    /// <param name="dpop">The DPoP string.</param>
    /// <param name="request">The HTTP Request</param>
    Task<JwtAuthResult<DPoPData>> Validate(string? dpop, HttpRequest request);
    
    /// <summary>
    /// Validates both the Access Token and the DPoP Proof, ensuring they are bound together.
    /// </summary>
    /// <param name="token">The JWT string.</param>
    /// <param name="dpop">The DPoP string.</param>
    /// <param name="request">The HTTP Request</param>
    Task<JwtAuthResult<AccessData>> Validate(string? token, string? dpop, HttpRequest request);
}