using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CrossCutting.Extensions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebAPI.Swagger.Filters.Schema;

public class EnumFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Type) ?? context.Type;
        
        // Skip non-enum types
        if (!type.IsEnum)
            return;
        
        // Check if this enum has JsonStringEnumConverter applied
        var hasStringEnumConverter = type
            .GetCustomAttributes(typeof(JsonConverterAttribute), false)
            .OfType<JsonConverterAttribute>()
            .Any(attr => attr.ConverterType == typeof(JsonStringEnumConverter));
        
        List<string> enumDescriptions;
        
        if (hasStringEnumConverter)
        {
            schema.Type = "string";
            schema.Enum = Enum.GetNames(type)
                .Select(n => new OpenApiString(n))
                .Cast<IOpenApiAny>()
                .ToList();
            
            enumDescriptions = Enum.GetValues(type)
                .Cast<Enum>()
                .Select(e =>
                {
                    var member = type.GetMember(e.ToString()).First();
                    var display = member.GetCustomAttributes(typeof(DisplayAttribute), false)
                        .Cast<DisplayAttribute>()
                        .FirstOrDefault();

                    var name = display?.Name ?? e.ToString();
                    var desc = display?.Description ?? string.Empty;
                    var value = e.GetMember();

                    return $"{value} : {name} - {desc}";
                }).ToList();
        }
        else
        {
            schema.Type = "integer";
            schema.Format = "int32";
            schema.Enum = Enum.GetValues(type)
                .Cast<object>()
                .Select(v => new OpenApiInteger((int)v))
                .Cast<IOpenApiAny>()
                .ToList();
            
            enumDescriptions = Enum.GetValues(type)
                .Cast<Enum>()
                .Select(e =>
                {
                    var member = type.GetMember(e.ToString()).First();
                    var display = member.GetCustomAttributes(typeof(DisplayAttribute), false)
                        .Cast<DisplayAttribute>()
                        .FirstOrDefault();

                    var name = display?.Name ?? e.ToString();
                    var desc = display?.Description ?? string.Empty;
                    var value = Convert.ToInt32(e);

                    return $"{value} : {name} - {desc}";
                }).ToList();
        }

        schema.Description += "<br/>" + (type.Namespace ?? "Global") + "<hr>" + string.Join("<br/>", enumDescriptions) + "<hr>";
    }
}