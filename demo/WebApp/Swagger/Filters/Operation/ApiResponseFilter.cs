using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApp.ViewModels;

namespace WebApp.Swagger.Filters.Operation;

public class ApiResponseFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodInfo = context.MethodInfo;

        // Get task inner type
        var declaredReturn = methodInfo.ReturnType;
        if (declaredReturn.IsGenericType && declaredReturn.GetGenericTypeDefinition() == typeof(Task<>))
        {
            declaredReturn = declaredReturn.GetGenericArguments()[0];
        }

        // Skip void, skip IActionResult
        if (declaredReturn == typeof(void) ||
            typeof(IActionResult).IsAssignableFrom(declaredReturn))
            return;

        // Skip if already ApiResponse<T>
        if (typeof(IApiResponse).IsAssignableFrom(declaredReturn))
            return;

        // This is the REAL return type from the controller
        var realType = declaredReturn;

        // Wrap it: ApiResponseDto<realType>
        var wrapperType = typeof(ApiResponse<>).MakeGenericType(realType);

        // Replace swagger schema
        operation.Responses["200"] = new OpenApiResponse
        {
            Description = "Success",
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = context.SchemaGenerator.GenerateSchema(wrapperType, context.SchemaRepository)
                }
            }
        };
    }
}