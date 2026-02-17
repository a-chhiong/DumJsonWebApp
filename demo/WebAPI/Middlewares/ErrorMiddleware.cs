namespace WebAPI.Middlewares;

public class ErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorMiddleware> _logger;

    public ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception occurred");
        
        var statusCode = StatusCodes.Status500InternalServerError;
        var message = string.IsNullOrEmpty(exception.Message) 
            ? "An unhandled exception occurred" : exception.Message;

        if (exception is BadHttpRequestException badHttp)
        {
            statusCode = badHttp.StatusCode;
        }

        var response = new
        {
            code = exception.HResult,
            name = exception.GetType().Name,
            message = message,
            data = exception.Data
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response);
    }
}
