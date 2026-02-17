using JoshAuthorization.Models;

namespace JoshAuthorization.Objects;

public class TokenData : IJwtResultData
{
    public required TokenPayload Token { get; init; }
}