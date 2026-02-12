using JoshAuthorization.Models;
using JoshAuthorization.Objects;

namespace Application.Models;

public class TokenCacheEntry
{
    public string jti { get; init; }
    public string token_type { get; init; }
    public string refresh_token { get; init; }
    public JwkObject? jwk { get; init; }
    public JwtMetadata? meta { get; init; }
}