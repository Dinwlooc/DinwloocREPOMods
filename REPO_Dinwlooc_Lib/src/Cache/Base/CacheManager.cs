using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Dinwlooc.Common.Core;
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

        /// <summary>
        /// 获取或创建同步缓存。若缓存已存在，会验证其序列化策略是否与传入参数一致，不一致则抛出异常。
        /// </summary>
        public static ISyncCache<TKey, TValue> GetOrCreateSyncCache<TKey, TValue>(
    string cacheName,
    SyncMode mode,
    Action<BinaryWriter, TValue>? serialize = null,
    Func<BinaryReader, TValue>? deserialize = null,
    Func<TValue, TValue, TValue>? mergeFunc = null)
    where TKey : notnull
        {
            var cache = (ISyncCache<TKey, TValue>)_caches.GetOrAdd(cacheName, key =>
            {
                int factoryDictHash = _caches.GetHashCode();
                var newCache = SyncManager.Instance.CreateSyncCache<TKey, TValue>(
                    key, mode, mergeFunc, serialize, deserialize);
                return newCache;
            });
            int afterDictHash = _caches.GetHashCode();
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

        public static ISyncCache[] GetAllSyncCaches()
        {
            List<ISyncCache> list = new List<ISyncCache>();
            foreach (object obj in _caches.Values)
            {
                if (obj is ISyncCache sync)
                    list.Add(sync);
            }
            return list.ToArray();
        }

        public static bool TryGetSyncCache(string cacheName, out ISyncCache? cache)
        {
            if (_caches.TryGetValue(cacheName, out object obj) && obj is ISyncCache sync)
            {
                cache = sync;
                return true;
            }
            cache = null;
            return false;
        }
    }
}