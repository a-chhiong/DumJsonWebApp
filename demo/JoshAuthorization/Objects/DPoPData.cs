using JoshAuthorization.Models;

namespace JoshAuthorization.Objects;

public class DPoPData : IJwtResultData
{
    public required JwkObject Jwk { get; init; }
    public required DPoPPayload DPoP { get; init; }
}