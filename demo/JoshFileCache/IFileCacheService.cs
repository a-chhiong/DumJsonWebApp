namespace JoshFileCache;

public interface IFileCacheService
{
    public Task SetAsync(string key, object? value, TimeSpan? ttl = null);
    public Task ExpireAsync(string key);
    public Task<object?> GetAsync(string key);
    public Task CleanupExpiredAsync();
}