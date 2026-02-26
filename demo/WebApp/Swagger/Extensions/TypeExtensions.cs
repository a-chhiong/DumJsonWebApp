using System.Reflection;

namespace WebApp.Swagger.Extensions;

public static class TypeExtensions
{
    private static readonly NullabilityInfoContext NullabilityContext = new();
    
    private static readonly HashSet<Type> SimpleTypes =
    [
        typeof(string), typeof(decimal), typeof(DateTime), 
        typeof(DateOnly), typeof(TimeOnly), typeof(Guid), typeof(Uri)
    ];

    /// <summary>
    /// Check if the property is allowed to be null.
    /// </summary>
    public static bool IsNullable(this PropertyInfo propInfo)
    {
        if (Nullable.GetUnderlyingType(propInfo.PropertyType) != null) return true;

        var info = NullabilityContext.Create(propInfo);
        return info.WriteState is NullabilityState.Nullable || 
               info.ReadState is NullabilityState.Nullable;
    }

    /// <summary>
    /// Identifies if a type is a "Plain Object" (your custom DTO/ViewModel).
    /// </summary>
    public static bool IsCustomClass(this Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;

        return !t.IsPrimitive && 
               !t.IsEnum && 
               !SimpleTypes.Contains(t) &&
               t.Namespace?.StartsWith("System") == false && 
               !typeof(System.Collections.IEnumerable).IsAssignableFrom(t);
    }
}