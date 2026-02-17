namespace WebAPI.Middlewares;

public class SpaMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public SpaMiddleware(
        RequestDelegate next, 
        IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        
        // Root fallback
        if (path == "/" || path == "/web" || path == "/web/")
        {
            await Redirect(context);
            return;
        }
    
        // Only handle requests starting with /Web
        if (path.StartsWith("/web", StringComparison.OrdinalIgnoreCase))
        {   
            var filePath = Path.Combine(_env.ContentRootPath, "WebPage", "v3", path.TrimStart('/'));

            // If the requested file does not exist, serve index.html
            if (!File.Exists(filePath) && !Directory.Exists(filePath))
            {
                await Redirect(context);
                return;
            }
        }

        await _next(context);
    }

    private async Task Redirect(HttpContext context)
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(
            Path.Combine(_env.ContentRootPath, "WebPage", "v3", "index.html"));
    }
}