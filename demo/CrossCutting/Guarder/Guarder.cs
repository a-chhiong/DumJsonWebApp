namespace CrossCutting.Guarder;

public static class Guarder
{
    public static void Throw<TCode>(bool cond, TCode code)
        => Throw(cond, code, string.Empty);

    public static void Throw<TCode>(bool cond, TCode code, string? message)
        => Throw(cond, code, message, null);

    public static void Throw<TCode>(bool cond, TCode code, object? data)
        => Throw(cond, code, string.Empty, data);

    public static void Throw<TCode>(bool cond, TCode code, string? message, object? data)
    {
        if (cond)
        {
            throw new GuarderException(message, code, data);
        }
    }
}

public class GuarderException : Exception
{
    public object? Code { get; }
    public object? ExtraData { get; }

    public GuarderException(string? message, object? code, object? extraData = null) : base(message)
    {
        Code = code;
        ExtraData = extraData;
    }
}