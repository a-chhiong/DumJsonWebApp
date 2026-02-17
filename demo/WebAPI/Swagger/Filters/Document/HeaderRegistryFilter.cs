using CrossCutting.Constants;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebAPI.Swagger.Filters.Document;

public class HeaderRegistryFilter : IOperationFilter
{
    /// <summary>
    /// 想要顯示在 Header 的名稱
    /// </summary>
    private static readonly string[] IncludedHeaders =
    [
        CustomHeaderNames.DPoP,
        CustomHeaderNames.TRACE_ID,
    ];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        foreach (var includedHeader in IncludedHeaders)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = includedHeader,
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema { Type = "string" },
                Description = ""
            });
        }
    }
}