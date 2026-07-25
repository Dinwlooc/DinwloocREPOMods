using System;
using System.Collections.Concurrent;

namespace Dinwlooc.Common.Caching
{
    /// <summary>
    /// 内存缓存实现，基于 ConcurrentDictionary，支持过期时间。
    /// <para>
    /// 这是一个基础的缓存实现，模组可以直接使用，也可以派生子类添加自定义逻辑。
    /// 由于其数据存储在内存中，适合存储少量、频繁访问的共享数据（如玩家配置、临时状态等）。
    /// 多个模组可以通过 <see cref="CacheManager"/> 获取同一个 <see cref="MemoryCache{TKey, TValue}"/> 实例，
    /// 从而实现数据的共享和协同更新。
    /// </para>
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

        public void Refresh(TKey key)
        {
            // 若键存在，重置其过期时间（延长有效期）
            if (_cache.TryGetValue(key, out var entry))
            {
                // 如果原有过期时间，则重新设置为当前时间+剩余有效期（简单处理：重置为永不过期，或按需）
                // 此处简单重置为永不过期，调用者可自行处理。
                entry.ExpiresAt = null;
            }
        }
    }
}