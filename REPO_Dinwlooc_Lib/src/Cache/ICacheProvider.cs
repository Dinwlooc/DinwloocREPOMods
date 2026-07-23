using System;

namespace Dinwlooc.Common.Caching;

/// <summary>
/// 缓存提供者接口，定义缓存的增删改查及过期管理。
/// </summary>
/// <typeparam name="TKey">键类型</typeparam>
/// <typeparam name="TValue">值类型</typeparam>
public interface ICacheProvider<TKey, TValue> where TKey : notnull
{
    bool TryGet(TKey key, out TValue value);
    void Set(TKey key, TValue value, TimeSpan? expiration = null);
    bool Remove(TKey key);
    void Clear();
    void Refresh(TKey key);
}