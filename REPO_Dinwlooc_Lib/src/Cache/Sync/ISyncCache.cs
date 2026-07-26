using System;
using Dinwlooc.Common.Caching;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 同步缓存接口，用于在多人游戏中跨客户端同步键值数据。
    /// 支持多种同步模式，并可通过事件监听数据变更。
    /// </summary>
    public interface ISyncCache<TKey, TValue> : ICacheProvider<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// 数据变更时触发（包括本地修改和远程同步）。
        /// 可用于更新 UI 或触发业务逻辑。
        /// </summary>
        event Action<TKey, TValue> OnDataChanged;

        /// <summary>
        /// 强制立即将当前所有数据同步给其他客户端（仅房主有效）。
        /// 通常用于确保新加入玩家获得最新数据。
        /// </summary>
        void SyncNow();

        /// <summary>
        /// 当前缓存的同步模式，决定写入权限和同步策略。
        /// </summary>
        SyncMode Mode { get; }

        /// <summary>
        /// 是否使用二进制流式序列化（开启后将使用自定义序列化委托，可提升性能）。
        /// </summary>
        bool UseBinarySerialization { get; }
    }
}