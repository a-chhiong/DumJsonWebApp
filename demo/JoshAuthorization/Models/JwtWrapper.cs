namespace JoshAuthorization.Models;

public class JwtWrapper
{
    public required string Jti { get; init; }
    public required string TokenType { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    
}