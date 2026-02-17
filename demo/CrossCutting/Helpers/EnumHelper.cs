namespace CrossCutting.Helpers;

public class EnumHelper
{
    public static TEnum? CastTo<TEnum>(object? code) where TEnum : struct, Enum
    {
        if (code == null) return null;

        if (code is TEnum e) return e;

        if (code is int iCode && Enum.IsDefined(typeof(TEnum), iCode))
            return (TEnum)(object)iCode;

        if (code is string strCode && Enum.TryParse<TEnum>(strCode, true, out var parsed))
            return parsed;

        if (code is Enum eCode)
        {
            var underlying = Convert.ToInt32(eCode);
            if (Enum.IsDefined(typeof(TEnum), underlying))
                return (TEnum)(object)underlying;
        }
        
        return null;
    }
}