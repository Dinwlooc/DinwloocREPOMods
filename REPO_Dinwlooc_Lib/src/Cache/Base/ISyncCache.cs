using System;
using System.Collections.Generic;
using Dinwlooc.Common.Caching;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace Dinwlooc.Common.Sync
{
    public interface ISyncCache
    {
        string CacheName { get; }
        SyncMode Mode { get; }
        bool UseBinarySerialization { get; }
        bool AllowFullUpdateRequest { get; }
        object? Version { get; set; }

        void ApplyRemoteSetObject(object key, object value);
        void ApplyRemoteSetBinary(object key, byte[] data);
        void ApplyRemoteRemove(object key);
        void ApplyRemoteClear();
        void ProcessMergeObject(object key, object value);
        void ProcessMergeBinary(object key, byte[] data);
        void SyncNow();
        PhotonHashtable GetSnapshot();

        /// <summary>
        /// 尝试获取二进制格式的快照（若支持二进制序列化则返回 true）。
        /// </summary>
        bool TryGetSnapshotBinary(out Dictionary<object, byte[]> snapshot);

        void RequestFullUpdate(object? version = null);
    }

    public interface ISyncCache<TKey, TValue> : ICacheProvider<TKey, TValue>, ISyncCache
        where TKey : notnull
    {
        event Action<TKey, TValue> OnDataChanged;
        event Action<TKey> OnDataRemoved;
        event Action OnDataCleared;
    }
}