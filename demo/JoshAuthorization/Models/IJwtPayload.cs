namespace JoshAuthorization.Models;

public interface IJwtPayload
{
    /// <summary>
    /// JWT ID
    /// </summary>
    public string jti { get; init; }
}