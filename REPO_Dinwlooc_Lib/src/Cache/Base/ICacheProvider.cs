using System;

namespace Dinwlooc.Common.Caching
{
    /// <summary>
    /// 缓存提供者接口。
    /// <para>
    /// 设计意图：提供统一的缓存操作抽象，使得不同模组可以共享同一缓存实例，
    /// 并按照相同的接口进行增删改查和过期管理。
    /// 这样，当某个模组更新了缓存数据（如远程配置），其他模组可以立即感知到变化，
    /// 实现“一处更新，多处使用”的协作模式。
    /// </para>
    /// </summary>
    /// <typeparam name="TKey">键类型（必须可 null 比较）</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public interface ICacheProvider<TKey, TValue> where TKey : notnull
    {
        /// <summary>尝试获取键对应的值，若存在且未过期则返回 true。</summary>
        bool TryGet(TKey key, out TValue value);

        /// <summary>设置键值对，可指定过期时间（null 表示永不过期）。</summary>
        void Set(TKey key, TValue value, TimeSpan? expiration = null);

        /// <summary>移除指定键。</summary>
        bool Remove(TKey key);

        /// <summary>清空所有缓存项。</summary>
        void Clear();

        /// <summary>刷新指定键（若存在则重置其过期时间，相当于延长有效期）。</summary>
        void Refresh(TKey key);
    }
}