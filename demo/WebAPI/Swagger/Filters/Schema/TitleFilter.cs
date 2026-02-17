using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebAPI.Swagger.Filters.Schema;

public class TitleFilter : ISchemaFilter
{
    private static readonly HashSet<Type> SimpleTypes =
    [
        typeof(string), typeof(decimal),
        typeof(DateTime), typeof(DateOnly), typeof(TimeOnly)
    ];
    
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Type) ?? context.Type;
        
        // Skip primitive/simple types
        if (type.IsPrimitive || SimpleTypes.Contains(type))
            return;

        schema.Title = BuildName(type);
    }
    
    private static string BuildName(Type t)
    {
        if (!t.IsGenericType)
            return t.Name;
            
        var genericName = t.GetGenericTypeDefinition().Name;
        genericName = genericName[..genericName.IndexOf('`')]; // strip `1, `2, etc.
        var genericArgs = string.Join("_", t.GetGenericArguments().Select(BuildName));
        return $"{genericName}<{genericArgs}>";
    }
}