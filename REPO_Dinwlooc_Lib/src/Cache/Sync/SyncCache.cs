using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Photon.Pun;

namespace Dinwlooc.Common.Sync
{
    internal class SyncCache<TKey, TValue> : ISyncCache<TKey, TValue> where TKey : notnull
    {
        private const int DEFAULT_EXPIRATION_SECONDS = 0;

        private readonly ConcurrentDictionary<TKey, TValue> _cache = new ConcurrentDictionary<TKey, TValue>();
        private readonly SyncMode _mode;
        private readonly string _cacheName;
        private readonly Func<TValue, TValue, TValue>? _mergeFunc;
        private readonly ISerializationStrategy<TValue> _serializationStrategy;

        public event Action<TKey, TValue>? OnDataChanged;
        public SyncMode Mode => _mode;
        public bool UseBinarySerialization => _serializationStrategy is BinaryStrategy<TValue>;

        internal SyncCache(
            string cacheName,
            SyncMode mode,
            Func<TValue, TValue, TValue>? mergeFunc = null,
            Action<BinaryWriter, TValue>? serialize = null,
            Func<BinaryReader, TValue>? deserialize = null)
        {
            _cacheName = cacheName;
            _mode = mode;
            _mergeFunc = mergeFunc;

            if (serialize != null && deserialize != null)
            {
                _serializationStrategy = new BinaryStrategy<TValue>(serialize, deserialize);
            }
            else
            {
                _serializationStrategy = new HashtableStrategy<TValue>();
            }
        }

        public bool TryGet(TKey key, out TValue value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void Set(TKey key, TValue value, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out TValue existingValue))
            {
                if (EqualityComparer<TValue>.Default.Equals(existingValue, value))
                {
                    return;
                }
            }

            bool isHost = PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom;
            bool canWrite = isHost || (_mode == SyncMode.ClientSnapshot) || (_mode == SyncMode.Merge);
            if (!canWrite)
            {
                return;
            }

            _cache[key] = value;
            OnDataChanged?.Invoke(key, value);

            if (isHost && ((_mode == SyncMode.HostAuthority) || (_mode == SyncMode.Merge)))
            {
                if (UseBinarySerialization)
                {
                    byte[] data = _serializationStrategy.SerializeToBinary(value);
                    SyncRpcModule.BroadcastDataBinary<TKey>(_cacheName, key, data);
                }
                else
                {
                    object data = _serializationStrategy.SerializeToObject(value);
                    SyncRpcModule.BroadcastData<TKey, object>(_cacheName, key, data);
                }
            }
            else if ((_mode == SyncMode.ClientSnapshot) && !isHost)
            {
                if (UseBinarySerialization)
                {
                    byte[] data = _serializationStrategy.SerializeToBinary(value);
                    SyncRpcModule.SendSnapshotBinary<TKey>(_cacheName, key, data);
                }
                else
                {
                    object data = _serializationStrategy.SerializeToObject(value);
                    SyncRpcModule.SendSnapshot<TKey, object>(_cacheName, key, data);
                }
            }
            else if ((_mode == SyncMode.Merge) && !isHost)
            {
                if (UseBinarySerialization)
                {
                    byte[] data = _serializationStrategy.SerializeToBinary(value);
                    SyncRpcModule.SendMergeRequestBinary<TKey>(_cacheName, key, data);
                }
                else
                {
                    object data = _serializationStrategy.SerializeToObject(value);
                    SyncRpcModule.SendMergeRequest<TKey, object>(_cacheName, key, data);
                }
            }
        }

        public bool Remove(TKey key)
        {
            bool removed = _cache.TryRemove(key, out _);
            if (removed && PhotonNetwork.IsMasterClient && ((_mode == SyncMode.HostAuthority) || (_mode == SyncMode.Merge)))
            {
                SyncRpcModule.BroadcastRemove<TKey>(_cacheName, key);
            }
            return removed;
        }

        public void Clear()
        {
            _cache.Clear();
            if (PhotonNetwork.IsMasterClient && ((_mode == SyncMode.HostAuthority) || (_mode == SyncMode.Merge)))
            {
                SyncRpcModule.BroadcastClear(_cacheName);
            }
        }

        public void Refresh(TKey key) { }

        public void SyncNow()
        {
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                return;
            }

            ConcurrentDictionary<TKey, TValue> snapshot = new ConcurrentDictionary<TKey, TValue>(_cache);
            if (UseBinarySerialization)
            {
                Dictionary<object, byte[]> data = new Dictionary<object, byte[]>();
                foreach (KeyValuePair<TKey, TValue> kv in snapshot)
                {
                    byte[] serialized = _serializationStrategy.SerializeToBinary(kv.Value);
                    data[kv.Key!] = serialized;
                }
                SyncRpcModule.BroadcastFullSnapshotBinary<TKey>(_cacheName, data);
            }
            else
            {
                ConcurrentDictionary<TKey, object> data = new ConcurrentDictionary<TKey, object>();
                foreach (KeyValuePair<TKey, TValue> kv in snapshot)
                {
                    data[kv.Key] = _serializationStrategy.SerializeToObject(kv.Value);
                }
                SyncRpcModule.BroadcastFullSnapshot<TKey, object>(_cacheName, data);
            }
        }

        internal void ApplyRemoteSet(TKey key, TValue value)
        {
            _cache[key] = value;
            OnDataChanged?.Invoke(key, value);
        }

        internal void ApplyRemoteSetBinary(TKey key, byte[] binaryData)
        {
            TValue value = _serializationStrategy.DeserializeFromBinary(binaryData);
            _cache[key] = value;
            OnDataChanged?.Invoke(key, value);
        }

        internal void ApplyRemoteSetObject(TKey key, object objectData)
        {
            TValue value = _serializationStrategy.DeserializeFromObject(objectData);
            _cache[key] = value;
            OnDataChanged?.Invoke(key, value);
        }

        internal void ApplyRemoteRemove(TKey key)
        {
            _cache.TryRemove(key, out _);
        }

        internal void ApplyRemoteClear()
        {
            _cache.Clear();
        }

        internal Dictionary<object, object> GetAllDataAsObjects()
        {
            Dictionary<object, object> result = new Dictionary<object, object>();
            foreach (KeyValuePair<TKey, TValue> kv in _cache)
            {
                result[kv.Key!] = _serializationStrategy.SerializeToObject(kv.Value);
            }
            return result;
        }

        internal Dictionary<object, byte[]> GetAllDataAsBinary()
        {
            Dictionary<object, byte[]> result = new Dictionary<object, byte[]>();
            foreach (KeyValuePair<TKey, TValue> kv in _cache)
            {
                result[kv.Key!] = _serializationStrategy.SerializeToBinary(kv.Value);
            }
            return result;
        }
    }
}