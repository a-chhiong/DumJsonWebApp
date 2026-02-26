using CrossCutting.Extensions;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using WebApp.Swagger.Filters.Document;
using WebApp.Swagger.Filters.Operation;
using WebApp.Swagger.Filters.Schema;

namespace WebApp.Swagger;

/// <summary>
/// Swagger 打造工廠
/// </summary>
public static class SwaggerFactory
{
    /// <summary>
    /// SwaggerGen
    /// </summary>
    /// <param name="options"></param>
    /// <param name="versionInfo"></param>
    public static void Config(SwaggerGenOptions options, string versionInfo)
    {
        var descriptionFilePath = Path.Combine(AppContext.BaseDirectory, "Swagger/docs", "Description.md");
        var descriptionText = File.Exists(descriptionFilePath) 
            ? File.ReadAllText(descriptionFilePath) 
            : "Test API";
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Side Project",
            Version = versionInfo.SafeFirst(7),
            Description = descriptionText,
            Contact = new OpenApiContact
            {
                Name = "SoftMobile",
                Url = new Uri("https://www.softmobile.com.tw/"),
            }
        });
    
        // THIS ENSURE NOTHING WILL BREAK ON SWAGGER DOC-GEN
        options.CustomSchemaIds(SchemaIdBuilder);   
        
        // Get all loaded assemblies in the current AppDomain
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var xmlFile = $"{assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                // Register XML comments for this assembly
                options.IncludeXmlComments(xmlPath, true);
            }
        }
        
        // options.UseInlineDefinitionsForEnums();
        
        options.AddSecurityDefinition(HeaderNames.Authorization, new OpenApiSecurityScheme
        {
            Name = HeaderNames.Authorization,
            In = ParameterLocation.Header,
            Description = "Access/DPoP Token. Enter: 'Bearer {token}' or 'DPoP {token}'"
        });

        options.OperationFilter<ApiResponseFilter>();
        options.OperationFilter<AuthHeaderFilter>();
        options.OperationFilter<HeaderRegistryFilter>();
        options.DocumentFilter<SchemaRegistryFilter>();
        options.SchemaFilter<TitleFilter>();
        options.SchemaFilter<DateTimeFilter>();
        options.SchemaFilter<EnumFilter>();
        options.SchemaFilter<DescriptionFilter>();
    }

    /// <summary>
    /// SwaggerUI
    /// </summary>
    /// <param name="options"></param>
    public static void Config(SwaggerUIOptions options)
    {
        options.DocumentTitle = "Josh Side Project";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Josh Side Project");
    }

    private static string SchemaIdBuilder(Type type)
    {
        var baseName = BuildName(type);
        var npName = type.Namespace?.Replace(".", "") ?? "Global";
        return $"{baseName}_{npName}";

        string BuildName(Type t)
        {
            if (!t.IsGenericType)
                return t.Name;
            
            var genericName = t.GetGenericTypeDefinition().Name;
            genericName = genericName[..genericName.IndexOf('`')]; // strip `1, `2, etc.
            var genericArgs = string.Join("_", t.GetGenericArguments().Select(BuildName));
            return $"{genericName}_{genericArgs}";
        }
    }
}