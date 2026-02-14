using CrossCutting.JSON;
using CrossCutting.Logger;
using JoshAuthorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.FileProviders;
using NeoSmart.Caching.Sqlite;
using WebAPI.Swagger;
using NLog.Web;
using SQLitePCL;
using WebAPI.Attributes;
using WebAPI.Formatters;
using WebAPI.Middlewares;
using WebAPI.ServiceCollection;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

// Load version.info written by MSBuild target
var versionInfo = "unknown";
try
{
    var path = Path.Combine(AppContext.BaseDirectory, "version.info");
    if (File.Exists(path))
    {
        versionInfo = File.ReadAllText(path).Trim();
    }
}
catch (Exception ex)
{
    versionInfo = $"error: {ex.Message}";
}
// Make it available via configuration as well singleton
builder.Configuration["VersionInfo"] = versionInfo;

// CorsPolicy
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
Console.WriteLine($"Cors:AllowedOrigins: {string.Join(",", corsOrigins)}");
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        if (corsOrigins.Contains("*")) // 'Wildcard' mode
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromHours(1));
        }
    });
});

// Initialise JwtAuth Environment Variables
var jwtAuthEnv = builder.Configuration["JWT_AUTH_ENVIRONMENT"];
builder.Services.Configure<JwtAuthEnvironmentOption>( 
    builder.Configuration.GetSection($"JwtAuthEnvironment:{jwtAuthEnv}")
);

// Register SQLite-based IDistributedCache
builder.Services.AddSqliteCache(options => {
    options.CachePath = "fusioncache.db"; // This sets the path correctly
    // Deletes expired items from the .db file every 60 minutes
    options.CleanupInterval = TimeSpan.FromMinutes(60);
}, new SQLite3Provider_e_sqlite3());

// Register FusionCache with L1 + L2
builder.Services.AddFusionCache()
    .WithDefaultEntryOptions(options =>
    {
        options.SetDuration(TimeSpan.FromMinutes(10));
        options.SetFailSafe(true,TimeSpan.FromHours(1));
    })
    .WithSerializer(new FusionCacheSystemTextJsonSerializer()) // Required for L2
    .WithDistributedCache(sp => sp.GetRequiredService<IDistributedCache>());

// Clear default providers and plug in NLog
builder.Logging.ClearProviders();
// Environment‑specific minimum level
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Trace);       // everything for debugging
}
else if (builder.Environment.IsStaging())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);       // detailed but less noisy
}
else if (builder.Environment.IsProduction())
{
    builder.Logging.SetMinimumLevel(LogLevel.Information); // clean, readable logs
}
builder.Host.UseNLog();

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiResponseFilterAttribute>();
    options.InputFormatters.Insert(0, new PlainTextInputFormatter());
    
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new EnumConverterFactory());
    options.JsonSerializerOptions.Converters.Add(new DateTimeConverterFactory());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(x => SwaggerFactory.Config(x, versionInfo));

// Register Adapter (DI resolves correct generic interface)
builder.Services.AddSingleton<IJsonMapper, JsonMapper>();
builder.Services.AddJwtAuthService();
builder.Services.AddAdapterService();

builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();

builder.Services.AddApplicationValidators();
builder.Services.AddApplicationServices();

/*
 * End right here
 */

var app = builder.Build();

// Serve static files from wwwroot
app.UseStaticFiles();

// Serve static files from /Web
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Web")),
    RequestPath = "/Web"
});

// Add SPA fallback for /Web
app.Use(async (context, next) =>
{
    // Root fallback
    if (context.Request.Path == "/")
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(
            Path.Combine(builder.Environment.ContentRootPath, "Web", "index.html")
        );
        return;
    }
    
    // Only handle requests starting with /Web
    if (context.Request.Path.StartsWithSegments("/Web", StringComparison.OrdinalIgnoreCase))
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var filePath = Path.Combine(builder.Environment.ContentRootPath, "Web", path.TrimStart('/'));

        // If the requested file does not exist, serve index.html
        if (!File.Exists(filePath) && !Directory.Exists(filePath))
        {
            context.Response.Redirect("/");
            return;
        }
    }

    await next();
});

app.UseCors("CorsPolicy");

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(SwaggerFactory.Config);
}

app.UseHttpsRedirection();
app.UseMiddleware<AuthMiddleware>();
app.UseMiddleware<TraceMiddleware>(); 

app.MapControllers();

// Initialize AppLog once with a resolver, and resolver reads from AsyncLocal
AppLog.Initialize(
    app.Services.GetRequiredService<ILoggerFactory>(),
    () => TraceContextHolder.CurrentTraceId.Value);

app.Run();

// Ensure NLog flushes/shuts down on exit
NLog.LogManager.Shutdown();