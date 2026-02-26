using CrossCutting.Constants;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApp.Swagger.Filters.Schema;

public class DateTimeFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(DateTime) || context.Type == typeof(DateTime?))
        {
            schema.Format = "date-time";
            schema.Example = new Microsoft.OpenApi.Any.OpenApiString(
                DateTime.Now.ToString(DateTimeFormats.LOCAL_DATE_TIME)
            );
        }
        else if (context.Type == typeof(DateOnly) || context.Type == typeof(DateOnly?))
        {
            schema.Format = "date-only";
            schema.Example = new Microsoft.OpenApi.Any.OpenApiString(
                DateTime.Now.ToString(DateTimeFormats.LOCAL_DATE)
            );
        }
        else if (context.Type == typeof(TimeOnly) || context.Type == typeof(TimeOnly?))
        {
            schema.Format = "time-only";
            schema.Example = new Microsoft.OpenApi.Any.OpenApiString(
                DateTime.Now.ToString(DateTimeFormats.LOCAL_TIME)
            );
        }
    }
}