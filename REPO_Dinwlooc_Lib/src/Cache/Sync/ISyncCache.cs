using System;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 同步缓存接口，提供同步相关事件
    /// </summary>
    public interface ISyncCache<TKey, TValue> : Dinwlooc.Common.Caching.ICacheProvider<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// 当数据发生变化时触发（包括本地修改和远程同步）
        /// </summary>
        event Action<TKey, TValue> OnDataChanged;

        /// <summary>
        /// 强制立即同步当前所有数据（仅房主有效）
        /// </summary>
        void SyncNow();

        /// <summary>
        /// 获取当前同步模式
        /// </summary>
        SyncMode Mode { get; }
    }
}