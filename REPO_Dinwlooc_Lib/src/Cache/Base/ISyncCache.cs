using System;
using Dinwlooc.Common.Caching;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 非泛型同步缓存接口，仅包含同步特有操作，不包含缓存读写方法。
    /// </summary>
    public interface ISyncCache
    {
        string CacheName { get; }
        SyncMode Mode { get; }
        bool UseBinarySerialization { get; }

        void ApplyRemoteSetObject(object key, object value);
        void ApplyRemoteSetBinary(object key, byte[] data);
        void ApplyRemoteRemove(object key);
        void ApplyRemoteClear();
        void ProcessMergeObject(object key, object value);
        void ProcessMergeBinary(object key, byte[] data);
        void SyncNow();
        PhotonHashtable GetSnapshot();
    }

    /// <summary>
    /// 泛型同步缓存接口，继承标准缓存接口 <see cref="ICacheProvider{TKey,TValue}"/> 和非泛型 <see cref="ISyncCache"/>。
    /// </summary>
    public interface ISyncCache<TKey, TValue> : ICacheProvider<TKey, TValue>, ISyncCache
        where TKey : notnull
    {
        event Action<TKey, TValue> OnDataChanged;
        event Action<TKey> OnDataRemoved;
        event Action OnDataCleared;
    }
}