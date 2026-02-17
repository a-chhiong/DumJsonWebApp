using CrossCutting.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebAPI.Swagger;

public class AuthHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check for [AllowAnonymous] on the method
        var hasAllowAnonymousOnMethod = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any();

        // Check for [AllowAnonymous] on the controller class itself
        var hasAllowAnonymousOnClass = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any() ?? false;

        // Initialize security requirements if null
        operation.Security.Clear();
        
        if (hasAllowAnonymousOnMethod || hasAllowAnonymousOnClass)
        {
            // Skip adding security requirement
            // Explicitly clear any inherited security requirements
            return;
        }

        operation.Security = new List<OpenApiSecurityRequirement>();

        // Allow Bearer... or DPoP...
        var bearerRequirement = new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme 
                { 
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme, 
                        Id = HeaderNames.Authorization
                    } 
                }
            ] = Array.Empty<string>(),
        };

        operation.Security.Add(bearerRequirement);
    }
}