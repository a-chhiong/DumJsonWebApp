using JoshAuthorization.Models;

namespace JoshAuthorization.Objects;

public class TokenPayload: IJwtPayload
{
    /// <summary>
    /// JWT ID
    /// </summary>
    public required string jti { get; init; }

    /// <summary>
    /// Issuer of the JWT
    /// </summary>
    public required string iss { get; init; }
    
    /// <summary>
    /// Audience for which the JWT is intended
    /// </summary>
    public required string aud { get; init; }
    
    /// <summary>
    /// Expiration
    /// </summary>
    public required long exp { get; init; }
    
    /// <summary>
    /// Issued Time
    /// </summary>
    public required long iat { get; init; }
    
    /// <summary>
    /// Not Before
    /// </summary>
    public long? nbf { get; init; }

    /// <summary>
    /// Subject of the JWT
    /// </summary>
    public string? sub { get; init; }

    /// <summary>
    /// Custom Claims
    /// </summary>
    public object? custom { get; init; }
    
    /// <summary>
    /// DPop Confirmation
    /// </summary>
    public CnfObject? cnf { get; init; }
}