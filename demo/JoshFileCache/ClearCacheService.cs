using CrossCutting.Logger;
using Microsoft.Extensions.Hosting;

namespace JoshFileCache;

public class CleanCacheService : BackgroundService
{
    private static readonly TimeSpan CleanUpPeriod = TimeSpan.FromMinutes(10);
    
    private readonly IFileCacheService _cache;
    
    public CleanCacheService(IFileCacheService cache)
    {
        _cache = cache;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _cache.CleanupExpiredAsync();
            }
            catch (Exception ex)
            {
                AppLog.Warn(ex.Message);
            }

            await Task.Delay(CleanUpPeriod, stoppingToken);
        }
    }
}