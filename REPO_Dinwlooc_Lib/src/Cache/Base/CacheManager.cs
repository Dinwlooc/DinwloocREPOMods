using System;
using System.Collections.Concurrent;
using Dinwlooc.Common.Sync;

namespace Dinwlooc.Common.Caching
{
    /// <summary>
    /// 缓存管理器。
    /// 设计意图：允许不同模组共享同一份缓存数据，避免重复构建和内存浪费。
    /// 当一个模组已经构建了某种类型的缓存（如远程配置、玩家状态等），
    /// 其他模组可以直接通过名称获取并使用该缓存，甚至可以协助更新其中的数据，
    /// 从而实现跨模组的数据协作。
    /// 同时提供同步缓存（SyncCache）的创建，支持跨玩家数据同步。
    /// 
    /// </summary>
    public static class CacheManager
    {
        private static readonly ConcurrentDictionary<string, object> _caches = new();

        /// <summary>
        /// 注册一个普通缓存实例。
        /// <para>如果缓存名称已存在，则忽略注册（不会覆盖），并输出警告。</para>
        /// </summary>
        public static void RegisterCache<TKey, TValue>(string cacheName, ICacheProvider<TKey, TValue> provider)
            where TKey : notnull
        {
            if (string.IsNullOrEmpty(cacheName))
                throw new ArgumentException("缓存名称不能为空", nameof(cacheName));

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
        /// 获取已注册的普通缓存，若不存在返回 null。
        /// </summary>
        public static ICacheProvider<TKey, TValue>? GetCache<TKey, TValue>(string cacheName)
            where TKey : notnull
        {
            if (_caches.TryGetValue(cacheName, out var provider) && provider is ICacheProvider<TKey, TValue> typed)
                return typed;
            return null;
        }

        /// <summary>
        /// 获取或创建普通缓存。若不存在则使用 factory 创建并注册。
        /// <para>如果缓存已存在但类型不匹配，会抛出异常。</para>
        /// </summary>
        public static ICacheProvider<TKey, TValue> GetOrCreateCache<TKey, TValue>(
            string cacheName,
            Func<ICacheProvider<TKey, TValue>> factory)
            where TKey : notnull
        {
            if (_caches.TryGetValue(cacheName, out var provider))
            {
                if (provider is ICacheProvider<TKey, TValue> typed)
                    return typed;
                throw new InvalidOperationException($"缓存 '{cacheName}' 已存在但类型不匹配。");
            }

            var newCache = factory();
            _caches[cacheName] = newCache;
            Core.CommonPlugin.Logger.LogInfo($"缓存 '{cacheName}' 已创建并注册。");
            return newCache;
        }

        /// <summary>
        /// 获取或创建同步缓存（支持自动网络同步）。
        /// 首次调用时将自动初始化 SyncRegionManager（懒加载）。
        /// </summary>
        /// <remarks>
        /// 警告：请勿在 Awake 中调用此方法！因为此时 Photon/GameDirector 可能未就绪。
        /// 推荐在 Start() 或 CommonService 延迟回调中使用。
        /// </remarks>
        public static ISyncCache<TKey, TValue> GetOrCreateSyncCache<TKey, TValue>(
            string cacheName,
            SyncMode mode,
            Func<TValue, TValue, TValue>? mergeFunc = null)
            where TKey : notnull
        {
            if (_caches.TryGetValue(cacheName, out var provider))
            {
                if (provider is ISyncCache<TKey, TValue> syncCache)
                    return syncCache;
                throw new InvalidOperationException($"缓存 '{cacheName}' 已存在但类型不匹配（非同步缓存）。");
            }

            // 懒加载：首次调用时创建 SyncRegionManager 实例
            var syncCacheInstance = SyncRegionManager.Instance.GetOrCreateSyncCache<TKey, TValue>(cacheName, mode, mergeFunc);
            _caches[cacheName] = syncCacheInstance;
            Core.CommonPlugin.Logger.LogInfo($"同步缓存 '{cacheName}' 已创建并注册（模式：{mode}）。");
            return syncCacheInstance;
        }

        /// <summary>
        /// 移除缓存。
        /// </summary>
        public static bool RemoveCache(string cacheName) => _caches.TryRemove(cacheName, out _);

        /// <summary>
        /// 清空所有缓存。
        /// </summary>
        public static void ClearAll() => _caches.Clear();
    }
}