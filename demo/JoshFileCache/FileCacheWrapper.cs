namespace JoshFileCache;

public class FileCacheWrapper
{
    // Unique identifier for the cached record
    public string Key { get; set; } = default!;

    // The actual payload; can be any JSON object/array
    // Using JsonElement is safer than object for arbitrary JSON
    public object? Value { get; set; }

    // When this record should expire
    public DateTime ExpiryUtc { get; set; }
}