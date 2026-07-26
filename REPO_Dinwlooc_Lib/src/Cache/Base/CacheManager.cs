using System;
using System.Collections.Concurrent;
using System.IO;
using Dinwlooc.Common.Sync;

namespace Dinwlooc.Common.Caching
{
    /// <summary>
    /// 缓存中心，提供普通缓存和同步缓存的创建与管理。
    /// 支持跨模组共享缓存数据，减少重复构建。
    /// </summary>
    public static class CacheManager
    {
        private static readonly ConcurrentDictionary<string, object> _caches = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// 注册普通缓存（非网络同步）。
        /// </summary>
        public static void RegisterCache<TKey, TValue>(string cacheName, ICacheProvider<TKey, TValue> provider)
            where TKey : notnull
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                throw new ArgumentException("缓存名称不能为空", nameof(cacheName));
            }

            if (_caches.TryAdd(cacheName, provider))
            {
                Core.CommonPlugin.Logger.LogInfo($"缓存 '{cacheName}' 已注册。");
            }
            else
            {
                Core.CommonPlugin.Logger.LogWarning($"缓存 '{cacheName}' 已存在，忽略注册。");
            }
        }

        /// <summary>
        /// 获取已注册的普通缓存。
        /// </summary>
        public static ICacheProvider<TKey, TValue>? GetCache<TKey, TValue>(string cacheName)
            where TKey : notnull
        {
            if (_caches.TryGetValue(cacheName, out object provider) && provider is ICacheProvider<TKey, TValue> typed)
            {
                return typed;
            }
            return null;
        }

        /// <summary>
        /// 获取或创建普通缓存。
        /// </summary>
        public static ICacheProvider<TKey, TValue> GetOrCreateCache<TKey, TValue>(
            string cacheName,
            Func<ICacheProvider<TKey, TValue>> factory)
            where TKey : notnull
        {
            if (_caches.TryGetValue(cacheName, out object provider))
            {
                if (provider is ICacheProvider<TKey, TValue> typed)
                {
                    return typed;
                }
                throw new InvalidOperationException($"缓存 '{cacheName}' 已存在但类型不匹配。");
            }

            ICacheProvider<TKey, TValue> newCache = factory();
            _caches[cacheName] = newCache;
            Core.CommonPlugin.Logger.LogInfo($"缓存 '{cacheName}' 已创建并注册。");
            return newCache;
        }

        /// <summary>
        /// 获取或创建同步缓存（自动网络同步）。
        /// 推荐使用二进制流式序列化（提供 serialize/deserialize 委托）以提升性能。
        /// 若不提供，将回退至通用序列化方式。
        /// </summary>
        public static ISyncCache<TKey, TValue> GetOrCreateSyncCache<TKey, TValue>(
            string cacheName,
            SyncMode mode,
            Action<BinaryWriter, TValue>? serialize = null,
            Func<BinaryReader, TValue>? deserialize = null,
            Func<TValue, TValue, TValue>? mergeFunc = null) where TKey : notnull
        {
            return SyncRegionManager.Instance.GetOrCreateSyncCache<TKey, TValue>(cacheName, mode, mergeFunc, serialize, deserialize);
        }

        /// <summary>
        /// 移除缓存（常用于模组卸载时清理）。
        /// </summary>
        public static bool RemoveCache(string cacheName)
        {
            return _caches.TryRemove(cacheName, out _);
        }

        /// <summary>
        /// 清空所有缓存（谨慎使用，会影响所有模组）。
        /// </summary>
        public static void ClearAll()
        {
            _caches.Clear();
        }
    }
}