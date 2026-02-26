using System.Reflection;
using System.Xml.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApp.Swagger.Extensions;

namespace WebApp.Swagger.Filters.Schema;

public class DescriptionFilter : ISchemaFilter
{
    private static readonly HashSet<Type> SimpleTypes =
    [
        typeof(string), typeof(decimal),
        typeof(DateTime), typeof(DateOnly), typeof(TimeOnly)
    ];
    
    private static readonly Dictionary<string, XDocument?> XmlCache = new();

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum || schema.Properties == null) return;

        foreach (var (key, propertySchema) in schema.Properties)
        {
            // 1. Get Property Info
            var propInfo = context.Type.GetProperty(key, 
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (propInfo == null) continue;
            
            // 2. Extract Data
            var summary = propInfo.GetSummary();
            var isCustom = propInfo.PropertyType.IsCustomClass();
            var isNullable = propInfo.IsNullable();
            
            if (!isCustom) continue;
            if (propertySchema.Reference == null) return;
            
            // 3. Apply Orchestration
            var originalRef = propertySchema.Reference;
            propertySchema.Reference = null;
            propertySchema.AllOf = new List<OpenApiSchema> { new() { Reference = originalRef } };

            propertySchema.Title = propInfo.Name;
            propertySchema.Type = "object";
            propertySchema.Description = summary;
            propertySchema.Nullable = isNullable;
        }
    }
}