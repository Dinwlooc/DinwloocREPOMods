using System;
using System.IO;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 非泛型同步缓存接口，用于无类型操作的 RPC 处理器。
    /// </summary>
    public interface ISyncCache
    {
        SyncMode Mode { get; }
        bool UseBinarySerialization { get; }

        void ApplyRemoteSetObject(object key, object value);
        void ApplyRemoteSetBinary(object key, byte[] data);
        void ApplyRemoteRemove(object key);
        void ApplyRemoteClear();
        void ProcessMergeObject(object key, object value);
        void ProcessMergeBinary(object key, byte[] data);
        void SyncNow();
    }

    /// <summary>
    /// 泛型同步缓存接口，提供类型安全的读写操作。
    /// </summary>
    public interface ISyncCache<TKey, TValue> : ISyncCache where TKey : notnull
    {
        event Action<TKey, TValue> OnDataChanged;
        bool TryGet(TKey key, out TValue value);
        void Set(TKey key, TValue value, TimeSpan? expiration = null);
        bool Remove(TKey key);
        void Clear();
        void Refresh(TKey key);
    }
}