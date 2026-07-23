using System;
using System.Collections.Concurrent;

namespace Dinwlooc.Common.Caching;

/// <summary>
/// 内存缓存实现，基于 ConcurrentDictionary，支持过期时间。
/// </summary>
public class MemoryCache<TKey, TValue> : ICacheProvider<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new();

    private class CacheEntry
    {
        public TValue Value { get; set; } = default!;
        public DateTime? ExpiresAt { get; set; }

        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }

    public bool TryGet(TKey key, out TValue value)
    {
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            value = entry.Value;
            return true;
        }

        if (entry != null && entry.IsExpired)
            _cache.TryRemove(key, out _);

        value = default!;
        return false;
    }

    public void Set(TKey key, TValue value, TimeSpan? expiration = null)
    {
        var entry = new CacheEntry
        {
            Value = value,
            ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : (DateTime?)null
        };
        _cache[key] = entry;
    }

    public bool Remove(TKey key) => _cache.TryRemove(key, out _);

    public void Clear() => _cache.Clear();

    public void Refresh(TKey key) => _cache.TryRemove(key, out _);
}