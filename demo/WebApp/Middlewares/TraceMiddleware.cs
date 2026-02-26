using CrossCutting.Constants;
using CrossCutting.Extensions;

namespace WebApp.Middlewares;

public class TraceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public TraceMiddleware(
        RequestDelegate next,
        IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.Request.Headers[CustomHeaderNames.TRACE_ID].FirstOrDefault()
                      ?? Guid.NewGuid().ToString();
        var versionInfo = _configuration["VersionInfo"] ?? "unknown";
        
        TraceContextHolder.CurrentTraceId.Value = traceId;
        
        context.Response.Headers[CustomHeaderNames.TRACE_ID] = traceId;
        context.Response.Headers[CustomHeaderNames.VERSION_CODE] = versionInfo.SafeFirst(7);
        await _next(context);
    }
}

public static class TraceContextHolder
{
    public static AsyncLocal<string?> CurrentTraceId { get; } = new AsyncLocal<string?>();
}