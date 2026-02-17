namespace JoshAuthorization.Models;

public interface IJwtAuthResult<out T> where T : IJwtResultData
{
    bool IsSuccess { get; }
    JwtError Error { get; }
    T? Data { get; }
}