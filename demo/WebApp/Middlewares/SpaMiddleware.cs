namespace WebApp.Middlewares;

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
        
        // 1. If it's an API call, let it go through the normal pipeline
        if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }
    
        // 2. If it's a physical file that exists (css, js, images), 
        // let StaticFileMiddleware handle it.
        if (Path.HasExtension(path)) 
        {
            await _next(context);
            return;
        }

        // 3. Otherwise, it's a SPA route. Serve index.html and STOP.
        await Redirect(context);
    }

    private async Task Redirect(HttpContext context)
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(
            Path.Combine(_env.ContentRootPath, "WebPage", "index.html"));
    }
}