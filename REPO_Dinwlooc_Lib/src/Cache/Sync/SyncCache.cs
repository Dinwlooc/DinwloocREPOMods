using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dinwlooc.Common.Sync
{
    internal class SyncCache<TKey, TValue> : ISyncCache<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _cache = new ConcurrentDictionary<TKey, TValue>();
        private readonly SyncMode _mode;
        private readonly string _cacheName;
        private readonly Func<TValue, TValue, TValue>? _mergeFunc;
        private readonly Action<BinaryWriter, TValue>? _serialize;
        private readonly Func<BinaryReader, TValue>? _deserialize;
        private readonly ISerializationStrategy<TValue> _serializationStrategy;

        public event Action<TKey, TValue>? OnDataChanged;
        public event Action<TKey>? OnDataRemoved;
        public event Action? OnDataCleared;

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

        public bool TryGet(TKey key, out TValue value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void Set(TKey key, TValue value, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out TValue existingValue) &&
                EqualityComparer<TValue>.Default.Equals(existingValue, value))
            {
                return;
            }

            bool isHost = Photon.Pun.PhotonNetwork.IsMasterClient || !Photon.Pun.PhotonNetwork.InRoom;
            bool canWrite = isHost || (_mode == SyncMode.ClientSnapshot) || (_mode == SyncMode.Merge);
            if (!canWrite)
            {
                return;
            }

            _cache[key] = value;
            OnDataChanged?.Invoke(key, value);
        }

        public bool Remove(TKey key)
        {
            bool removed = _cache.TryRemove(key, out _);
            if (removed)
            {
                OnDataRemoved?.Invoke(key);
            }
            return removed;
        }

        public void Clear()
        {
            _cache.Clear();
            OnDataCleared?.Invoke();
        }

        public void Refresh(TKey key) { }

        internal byte[] SerializeToBinary(TValue value)
        {
            MemoryStream ms = ByteBufferPool.Rent();
            try
            {
                using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8, true))
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

        internal TValue DeserializeFromBinary(byte[] data)
        {
            MemoryStream ms = ByteBufferPool.Rent();
            try
            {
                ms.Write(data, 0, data.Length);
                ms.Position = 0;
                using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8, true))
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

        internal object SerializeToObject(TValue value)
        {
            return _serializationStrategy.SerializeToObject(value);
        }

        internal TValue DeserializeFromObject(object data)
        {
            return _serializationStrategy.DeserializeFromObject(data);
        }

        internal ConcurrentDictionary<TKey, TValue> GetAllData()
        {
            return _cache;
        }

        void ISyncCache.ApplyRemoteSetObject(object key, object value)
        {
            if (key is TKey typedKey && value is TValue typedValue)
            {
                _cache[typedKey] = typedValue;
                OnDataChanged?.Invoke(typedKey, typedValue);
            }
            else
            {
                try
                {
                    TKey convertedKey = (TKey)Convert.ChangeType(key, typeof(TKey));
                    TValue convertedValue = (TValue)Convert.ChangeType(value, typeof(TValue));
                    _cache[convertedKey] = convertedValue;
                    OnDataChanged?.Invoke(convertedKey, convertedValue);
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
                _cache[typedKey] = value;
                OnDataChanged?.Invoke(typedKey, value);
            }
            else
            {
                try
                {
                    TKey convertedKey = (TKey)Convert.ChangeType(key, typeof(TKey));
                    TValue value = DeserializeFromBinary(data);
                    _cache[convertedKey] = value;
                    OnDataChanged?.Invoke(convertedKey, value);
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
                _cache.TryRemove(typedKey, out _);
                OnDataRemoved?.Invoke(typedKey);
            }
            else
            {
                try
                {
                    TKey convertedKey = (TKey)Convert.ChangeType(key, typeof(TKey));
                    _cache.TryRemove(convertedKey, out _);
                    OnDataRemoved?.Invoke(convertedKey);
                }
                catch (Exception ex)
                {
                    Core.CommonPlugin.Logger.LogError($"ApplyRemoteRemove 类型转换失败: {ex}");
                }
            }
        }

        void ISyncCache.ApplyRemoteClear()
        {
            _cache.Clear();
            OnDataCleared?.Invoke();
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

        void ISyncCache.SyncNow()
        {
            // 由同步器实现全量推送
        }

        private void ProcessMergeInternal(TKey key, TValue incoming)
        {
            if (_cache.TryGetValue(key, out TValue current))
            {
                if (_mergeFunc != null)
                {
                    incoming = _mergeFunc(current, incoming);
                }
            }
            Set(key, incoming);
        }
    }
}