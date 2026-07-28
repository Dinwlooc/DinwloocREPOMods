using System;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace Dinwlooc.Common.Sync
{
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

        /// <summary>
        /// 获取当前缓存的全量快照，用于网络同步。
        /// </summary>
        PhotonHashtable GetSnapshot();
    }

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