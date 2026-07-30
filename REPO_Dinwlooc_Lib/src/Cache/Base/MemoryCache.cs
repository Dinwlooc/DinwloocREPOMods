using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Dinwlooc.Common.Caching
{
    /// <summary>
    /// 内存缓存实现，基于 ConcurrentDictionary，支持过期时间。
    /// 数据存储与业务逻辑分离，可被 <see cref="SyncCache{TKey,TValue}"/> 包装复用。
    /// </summary>
    public class MemoryCache<TKey, TValue> : ICacheProvider<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new ConcurrentDictionary<TKey, CacheEntry>();

        private class CacheEntry
        {
            public TValue Value { get; set; } = default!;
            public DateTime? ExpiresAt { get; set; }
            public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
        }

        public bool TryGet(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out CacheEntry entry) && !entry.IsExpired)
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
            CacheEntry entry = new CacheEntry
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
            if (_cache.TryGetValue(key, out CacheEntry entry))
            {
                entry.ExpiresAt = null;
            }
        }

        /// <summary>
        /// 获取当前缓存中所有项的只读快照（用于网络同步）。
        /// </summary>
        public IReadOnlyDictionary<TKey, TValue> GetAllItems()
        {
            // 返回快照副本，避免并发修改
            Dictionary<TKey, TValue> snapshot = new Dictionary<TKey, TValue>();
            foreach (KeyValuePair<TKey, CacheEntry> kv in _cache)
            {
                if (!kv.Value.IsExpired)
                    snapshot[kv.Key] = kv.Value.Value;
            }
            return snapshot;
        }
    }
}