using JoshAuthorization.Models;

namespace JoshAuthorization.Objects;

public class AccessData : IJwtResultData
{
    public required JwkObject Jwk { get; init; }
    public required DPoPPayload DPoP { get; init; }
    public required TokenPayload Token { get; init; }
}