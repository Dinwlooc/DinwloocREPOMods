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
        private readonly Action<BinaryWriter, TValue>? _serialize;
        private readonly Func<BinaryReader, TValue>? _deserialize;
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
            _serialize = serialize;
            _deserialize = deserialize;

            if (serialize != null && deserialize != null)
            {
                _serializationStrategy = new BinaryStrategy<TValue>(serialize, deserialize);
            }
            else
            {
                _serializationStrategy = new HashtableStrategy<TValue>();
            }
        }

        // ---------- 泛型核心方法 ----------
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
                    byte[] data = SerializeToBinary(value);
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
                    byte[] data = SerializeToBinary(value);
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
                    byte[] data = SerializeToBinary(value);
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
                    byte[] serialized = SerializeToBinary(kv.Value);
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

        // ---------- 二进制序列化辅助（使用对象池） ----------
        private byte[] SerializeToBinary(TValue value)
        {
            MemoryStream ms = ByteBufferPool.Rent();
            try
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    if (_serialize != null)
                    {
                        _serialize(writer, value);
                    }
                    else
                    {
                        _serializationStrategy.SerializeToBinary(value);
                    }
                }
                return ms.ToArray();
            }
            finally
            {
                ByteBufferPool.Return(ms);
            }
        }

        private TValue DeserializeFromBinary(byte[] data)
        {
            MemoryStream ms = ByteBufferPool.Rent();
            try
            {
                ms.Write(data, 0, data.Length);
                ms.Position = 0;
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    if (_deserialize != null)
                    {
                        return _deserialize(reader);
                    }
                    return _serializationStrategy.DeserializeFromBinary(data);
                }
            }
            finally
            {
                ByteBufferPool.Return(ms);
            }
        }

        // ---------- 合并模式内部处理（强类型，无 DynamicInvoke） ----------
        private void ProcessMergeInternal(TKey key, TValue incoming)
        {
            if (_cache.TryGetValue(key, out TValue current))
            {
                if (_mergeFunc != null)
                {
                    incoming = _mergeFunc(current, incoming);
                }
            }
            // 直接调用 Set（内部会处理网络广播和存储）
            Set(key, incoming);
        }

        // ---------- ISyncCache 显式实现（供 RPC 处理器调用） ----------
        void ISyncCache.ApplyRemoteSetObject(object key, object value)
        {
            if (key is TKey typedKey && value is TValue typedValue)
            {
                ApplyRemoteSet(typedKey, typedValue);
            }
            else
            {
                try
                {
                    TKey convertedKey = (TKey)Convert.ChangeType(key, typeof(TKey));
                    TValue convertedValue = (TValue)Convert.ChangeType(value, typeof(TValue));
                    ApplyRemoteSet(convertedKey, convertedValue);
                }
                catch (Exception ex)
                {
                    Core.CommonPlugin.Logger.LogError($"ApplyRemoteSetObject 类型转换失败: {ex}");
                }
            }
        }

        void ISyncCache.ApplyRemoteSetBinary(object key, byte[] data)
        {
            if (key is TKey typedKey)
            {
                TValue value = DeserializeFromBinary(data);
                ApplyRemoteSet(typedKey, value);
            }
            else
            {
                try
                {
                    TKey convertedKey = (TKey)Convert.ChangeType(key, typeof(TKey));
                    TValue value = DeserializeFromBinary(data);
                    ApplyRemoteSet(convertedKey, value);
                }
                catch (Exception ex)
                {
                    Core.CommonPlugin.Logger.LogError($"ApplyRemoteSetBinary 类型转换失败: {ex}");
                }
            }
        }

        void ISyncCache.ApplyRemoteRemove(object key)
        {
            if (key is TKey typedKey)
            {
                ApplyRemoteRemove(typedKey);
            }
            else
            {
                try
                {
                    TKey convertedKey = (TKey)Convert.ChangeType(key, typeof(TKey));
                    ApplyRemoteRemove(convertedKey);
                }
                catch (Exception ex)
                {
                    Core.CommonPlugin.Logger.LogError($"ApplyRemoteRemove 类型转换失败: {ex}");
                }
            }
        }

        void ISyncCache.ApplyRemoteClear()
        {
            ApplyRemoteClear();
        }

        void ISyncCache.ProcessMergeObject(object key, object value)
        {
            if (key is TKey typedKey && value is TValue typedValue)
            {
                ProcessMergeInternal(typedKey, typedValue);
            }
            else
            {
                try
                {
                    TKey convertedKey = (TKey)Convert.ChangeType(key, typeof(TKey));
                    TValue convertedValue = (TValue)Convert.ChangeType(value, typeof(TValue));
                    ProcessMergeInternal(convertedKey, convertedValue);
                }
                catch (Exception ex)
                {
                    Core.CommonPlugin.Logger.LogError($"ProcessMergeObject 类型转换失败: {ex}");
                }
            }
        }

        void ISyncCache.ProcessMergeBinary(object key, byte[] data)
        {
            if (key is TKey typedKey)
            {
                TValue incoming = DeserializeFromBinary(data);
                ProcessMergeInternal(typedKey, incoming);
            }
            else
            {
                try
                {
                    TKey convertedKey = (TKey)Convert.ChangeType(key, typeof(TKey));
                    TValue incoming = DeserializeFromBinary(data);
                    ProcessMergeInternal(convertedKey, incoming);
                }
                catch (Exception ex)
                {
                    Core.CommonPlugin.Logger.LogError($"ProcessMergeBinary 类型转换失败: {ex}");
                }
            }
        }

        // ---------- 内部应用方法（供 RPC 直接使用） ----------
        internal void ApplyRemoteSet(TKey key, TValue value)
        {
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
                result[kv.Key!] = SerializeToBinary(kv.Value);
            }
            return result;
        }
    }
}