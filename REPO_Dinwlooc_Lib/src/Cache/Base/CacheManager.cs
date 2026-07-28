using System;
using System.Collections.Concurrent;
using System.IO;
using Dinwlooc.Common.Sync;

namespace Dinwlooc.Common.Caching
{
    public static class CacheManager
    {
        private static readonly ConcurrentDictionary<string, object> _caches = new ConcurrentDictionary<string, object>();

        public static void RegisterCache<TKey, TValue>(string cacheName, ICacheProvider<TKey, TValue> provider)
            where TKey : notnull
        {
            if (string.IsNullOrEmpty(cacheName))
                throw new ArgumentException("缓存名称不能为空", nameof(cacheName));

            if (_caches.TryAdd(cacheName, provider))
                Core.CommonPlugin.Logger.LogInfo($"缓存 '{cacheName}' 已注册。");
            else
                Core.CommonPlugin.Logger.LogWarning($"缓存 '{cacheName}' 已存在，忽略注册。");
        }

        public static ICacheProvider<TKey, TValue>? GetCache<TKey, TValue>(string cacheName)
            where TKey : notnull
        {
            if (_caches.TryGetValue(cacheName, out object provider) && provider is ICacheProvider<TKey, TValue> typed)
                return typed;
            return null;
        }

        public static ICacheProvider<TKey, TValue> GetOrCreateCache<TKey, TValue>(
            string cacheName,
            Func<ICacheProvider<TKey, TValue>> factory)
            where TKey : notnull
        {
            if (_caches.TryGetValue(cacheName, out object provider))
            {
                if (provider is ICacheProvider<TKey, TValue> typed)
                    return typed;
                throw new InvalidOperationException($"缓存 '{cacheName}' 已存在但类型不匹配。");
            }

            ICacheProvider<TKey, TValue> newCache = factory();
            _caches[cacheName] = newCache;
            Core.CommonPlugin.Logger.LogInfo($"缓存 '{cacheName}' 已创建并注册。");
            return newCache;
        }

        public static ISyncCache<TKey, TValue> GetOrCreateSyncCache<TKey, TValue>(
            string cacheName,
            SyncMode mode,
            Action<BinaryWriter, TValue>? serialize = null,
            Func<BinaryReader, TValue>? deserialize = null,
            Func<TValue, TValue, TValue>? mergeFunc = null) where TKey : notnull
        {
            ISyncCache<TKey, TValue> cache = SyncManager.Instance.GetOrCreateSyncCache<TKey, TValue>(
                cacheName, mode, mergeFunc, serialize, deserialize);

            if (!_caches.ContainsKey(cacheName))
            {
                _caches.TryAdd(cacheName, cache);
                Core.CommonPlugin.Logger.LogInfo($"同步缓存 '{cacheName}' 已注册到缓存中心。");
            }

            return cache;
        }

        public static bool RemoveCache(string cacheName)
        {
            return _caches.TryRemove(cacheName, out _);
        }

        public static void ClearAll()
        {
            _caches.Clear();
        }
    }
}