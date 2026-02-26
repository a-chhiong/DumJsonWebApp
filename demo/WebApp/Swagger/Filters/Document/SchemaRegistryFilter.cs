using CrossCutting.Enums;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApp.Swagger.Filters.Document;

public class SchemaRegistryFilter : IDocumentFilter
{
    /// <summary>
    /// 想要顯示在 Schema 的類別
    /// </summary>
    private static readonly Type[] IncludedTypes =
    [
        typeof(ErrorCodeEnum), 
        typeof(ActionCodeEnum),
    ];

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var type in IncludedTypes)
        {
            var schemaId = context.SchemaGenerator
                .GenerateSchema(type, context.SchemaRepository)
                .Reference?.Id ?? type.Name;

            if (swaggerDoc.Components.Schemas.ContainsKey(schemaId))
                continue;
            
            var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
            swaggerDoc.Components.Schemas.Add(schemaId, schema);
        }
    }
}