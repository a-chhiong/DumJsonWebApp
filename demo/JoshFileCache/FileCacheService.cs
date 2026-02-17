using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrossCutting.Logger;
using Microsoft.Extensions.Configuration;

namespace JoshFileCache;

public class FileCacheService: IFileCacheService
{
    private const int MaxAllowedLength = 10_000; // 10000 chars
    private const int MaxConcurrency = 10;  // 10 concurrency unit
    
    private readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false, // compact storage
        PropertyNameCaseInsensitive = true, // tolerate casing drift
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        AllowTrailingCommas = true, // tolerate sloppy JSON
        ReadCommentHandling = JsonCommentHandling.Skip // ignore comments
    };
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(5);

    private readonly string _cacheDir;
    
    public FileCacheService(IConfiguration config)
    {
        var folderName = config.GetSection("Cache:FolderName").Value ?? "Cache";
        _cacheDir = Path.Combine(AppContext.BaseDirectory, folderName);
        Directory.CreateDirectory(_cacheDir);

        // Clean up any orphaned temp files left behind
        foreach (var tmp in Directory.EnumerateFiles(_cacheDir, "*.tmp", SearchOption.AllDirectories))
        {
            try { File.Delete(tmp); }
            catch (Exception ex) { AppLog.Warn($"Failed to delete tmp file {tmp}: {ex.Message}"); }
        }
    }
    
    public async Task ExpireAsync(string key)
    {
        var path = GetCachePath(CreateFileName(key));

        var fileLock = GetLock(path);
        await fileLock.WaitAsync();
        try
        {
            if (!File.Exists(path))
                return;

            var record = await TryLoadAsync(path);
            if (record != null)
            {
                record.ExpiryUtc = DateTime.UtcNow; // force expiry
                var content = JsonSerializer.Serialize(record, Options);
                await File.WriteAllTextAsync(path, content);
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Error expiring cache file {path}: {ex.Message}");
        }
        finally
        {
            fileLock.Release();
        }
    }
    
    public async Task SetAsync(string key, object? value, TimeSpan? ttl = null)
    {
        var path = GetCachePath(CreateFileName(key));
        var tempPath = path + ".tmp";
        
        var record = new FileCacheWrapper
        {
            Key = key,
            Value = value,
            ExpiryUtc = DateTime.UtcNow.Add(ttl ?? _defaultTtl)
        };

        string content;
        try
        {
            content = JsonSerializer.Serialize(record, Options);
            if (content.Length > MaxAllowedLength)
            {
                AppLog.Warn($"Payload too large for key {key}");
                return;
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Serialization failed for key {key}: {ex.Message}");
            return;
        }

        var fileLock = GetLock(path);
        await fileLock.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(tempPath, content);
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Error writing cache file {path}: {ex.Message}");
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* swallow */ }
            }
        }
        finally
        {
            fileLock.Release();
        }
    }

    public async Task<object?> GetAsync(string key)
    {
        var path = GetCachePath(CreateFileName(key));
        
        if (!File.Exists(path))
            return null;

        FileCacheWrapper? record = null;
        
        var fileLock = GetLock(path);
        await fileLock.WaitAsync();
        try
        {
            record = await TryLoadAsync(path);
        }
        finally
        {
            fileLock.Release();
        }

        if (record == null || IsExpired(record))
        {
            try
            {
                File.Delete(path); 
                _locks.TryRemove(path, out var removedLock);
                removedLock?.Dispose();
            } catch { /* swallow */ }
            return null;
        }

        return record.Value;
    }

    public async Task CleanupExpiredAsync()
    {
        var files = Directory.EnumerateFiles(_cacheDir, "*.json", SearchOption.AllDirectories);
        using var throttler = new SemaphoreSlim(MaxConcurrency);
        
        var tasks = files.Select(async path =>
        {
            await throttler.WaitAsync();
            try
            {
                FileCacheWrapper? record = null;

                var fileLock = GetLock(path);
                await fileLock.WaitAsync();
                try
                {
                    record = await TryLoadAsync(path);
                }
                finally
                {
                    fileLock.Release();
                }

                if (record != null && IsExpired(record))
                {
                    try
                    {
                        File.Delete(path);
                        _locks.TryRemove(path, out var removedLock);
                        removedLock?.Dispose();
                        AppLog.Info($"Deleted expired cache file {path}");
                    }
                    catch (Exception ex)
                    {
                        AppLog.Warn($"Failed to delete expired file {path}: {ex.Message}");
                    }
                }
            }
            finally
            {
                throttler.Release();
            }
        }).ToList();
        
        await Task.WhenAll(tasks);
    }

    private static bool IsExpired(FileCacheWrapper record)
        => DateTime.UtcNow > record.ExpiryUtc;

    private async Task<FileCacheWrapper?> TryLoadAsync(string path)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<FileCacheWrapper>(stream, Options);
        }
        catch (JsonException ex)
        {
            AppLog.Warn($"Corrupted JSON in {path}: {ex.Message}");
            try
            {
                File.Delete(path);
                _locks.TryRemove(path, out var removedLock);
                removedLock?.Dispose();
            }
            catch { /* swallow */ }
            return null;
        }
        catch (IOException ex)
        {
            AppLog.Warn($"IO error reading {path}: {ex.Message}");
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLog.Warn($"Access denied reading {path}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Unexpected error reading {path}: {ex.Message}");
            return null;
        }
    }
    
    private static string CreateFileName(string key)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(key);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
    
    private string GetCachePath(string fileName)
    {
        var subDir = Path.Combine(_cacheDir, fileName[..2]);
        Directory.CreateDirectory(subDir);
        return Path.Combine(subDir, $"{fileName}.json");
    }
    
    private SemaphoreSlim GetLock(string filePath)
    {
        return _locks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
    }
}