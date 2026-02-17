namespace JoshAuthorization;

public class JwtAuthEnvironmentOption
{
    public string BaseUrl { get; init; }
    public string Issuer { get; init; }
    public string Audience { get; init; }
    public string PrivateKey { get; init; }
    public string PublicKey { get; init; }
    public long AccessExpiryInSeconds { get; init; } = 1 * 60;
    public long RefreshExpiryInSeconds { get; init; } = 10 * 60;
    public long RefreshNotBeforeInSeconds { get; init; } = 1 * 30;
    public long ClockSkewInSeconds { get; init; } = 5 * 60;
}