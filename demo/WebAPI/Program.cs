using CrossCutting.JSON;
using CrossCutting.Logger;
using JoshAuthorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.FileProviders;
using NeoSmart.Caching.Sqlite;
using WebAPI.Swagger;
using NLog.Web;
using SQLitePCL;
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
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
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

app.UseCors("CorsPolicy");

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(SwaggerFactory.Config);
}

app.UseHttpsRedirection();
// Serve static files
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "WebPage")),
    RequestPath = "/web"
});
app.UseMiddleware<ErrorMiddleware>();
app.UseMiddleware<TraceMiddleware>(); 
app.UseMiddleware<SpaMiddleware>();
app.UseMiddleware<AuthMiddleware>();

app.MapControllers();

// Initialize AppLog once with a resolver, and resolver reads from AsyncLocal
AppLog.Initialize(
    app.Services.GetRequiredService<ILoggerFactory>(),
    () => TraceContextHolder.CurrentTraceId.Value);

app.Run();

// Ensure NLog flushes/shuts down on exit
NLog.LogManager.Shutdown();