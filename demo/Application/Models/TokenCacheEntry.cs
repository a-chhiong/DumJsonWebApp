using JoshAuthorization.Models;
using JoshAuthorization.Objects;

namespace Application.Models;

public class TokenCacheEntry
{
    public string TokenType { get; init; }
    public string RefreshToken { get; init; }
    public string? Subject { get; init; }
    public object? Custom { get; init; }
}