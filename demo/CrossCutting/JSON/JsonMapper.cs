using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace CrossCutting.JSON;

public class JsonMapper : IJsonMapper
{
    private readonly ILogger<JsonMapper> _logger;

    public JsonMapper(ILogger<JsonMapper> logger)
    {
        _logger = logger;
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters =
        {
            new EnumConverterFactory(),
            new DateTimeConverterFactory(),
        }
    };

    public TTarget? Map<TSource, TTarget>(TSource? src)
    {
        if (src is null) return default;

        try
        {
            var json = JsonSerializer.Serialize(src, Options);
            return JsonSerializer.Deserialize<TTarget>(json, Options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map {SourceType} → {TargetType}", 
                typeof(TSource).Name, typeof(TTarget).Name);
            return default;
        }
    }

    public List<TTarget>? MapList<TSource, TTarget>(IEnumerable<TSource>? src)
    {
        if (src is null) return [];

        try
        {
            var json = JsonSerializer.Serialize(src, Options);
            return JsonSerializer.Deserialize<List<TTarget>>(json, Options) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map list {SourceType} → {TargetType}", 
                typeof(TSource).Name, typeof(TTarget).Name);
            return [];
        }
    }
}