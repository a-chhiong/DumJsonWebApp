namespace JoshAuthorization;

public class JwtAuthConstant
{
    public const long ACCESS_EXPIRY_IN_SECONDS = 2 * 60;
    public const long REFRESH_EXPIRY_IN_SECONDS = 30 * 60;
    public const long REFRESH_NOT_BEFORE_IN_SECONDS = 1 * 60;
    public const long CLOCK_SKEW_IN_SECONDS = 5 * 60;
}