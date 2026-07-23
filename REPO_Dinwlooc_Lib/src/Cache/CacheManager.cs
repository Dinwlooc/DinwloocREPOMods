using System;
using System.Collections.Concurrent;

namespace Dinwlooc.Common.Caching;

/// <summary>
/// 缓存管理器，管理所有已注册的缓存，支持跨模组共享。
/// </summary>
public static class CacheManager
{
    private static readonly ConcurrentDictionary<string, object> _caches = new();

    /// <summary>
    /// 注册一个缓存实例。
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
    /// 获取已注册的缓存，若不存在返回 null。
    /// </summary>
    public static ICacheProvider<TKey, TValue>? GetCache<TKey, TValue>(string cacheName)
        where TKey : notnull
    {
        if (_caches.TryGetValue(cacheName, out var provider) && provider is ICacheProvider<TKey, TValue> typed)
            return typed;

        return null;
    }

    /// <summary>
    /// 获取或创建缓存。若不存在则使用 factory 创建并注册。
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
    /// 移除缓存。
    /// </summary>
    public static bool RemoveCache(string cacheName) => _caches.TryRemove(cacheName, out _);

    /// <summary>
    /// 清空所有缓存。
    /// </summary>
    public static void ClearAll() => _caches.Clear();
}