namespace JoshAuthorization.Models;

public enum JwtError
{
    Successful = 0,
    InvalidToken,
    InvalidIssuer,
    InvalidAudience,
    ExpiredToken,
    UntimelyToken,
    UnsyncToken,
    InvalidHtm,
    InvalidHtu,
    InvalidAth,
    InvalidBinding,
    MissingToken,
    MissingScheme,
    UnexpectedError
}