namespace JoshAuthorization.Models;

public class JwtAuthResult<T> : IJwtAuthResult<T> where T : IJwtResultData
{
    public bool IsSuccess { get; set; }
    public JwtError Error { get; set; }
    public T? Data { get; set; }
}


