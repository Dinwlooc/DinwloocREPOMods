using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 纯本地缓存，无任何网络感知。
    /// </summary>
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

        public bool TryGet(TKey key, out TValue outputVal)
        {
            return _cache.TryGetValue(key, out outputVal);
        }

        public void Set(TKey key, TValue inputVal, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out TValue oldVal) &&
                EqualityComparer<TValue>.Default.Equals(oldVal, inputVal))
            {
                return;
            }

            _cache[key] = inputVal;
            OnDataChanged?.Invoke(key, inputVal);
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

        internal void ApplyMerge(TKey key, TValue incomingVal)
        {
            if (_cache.TryGetValue(key, out TValue currentVal))
            {
                if (_mergeFunc != null)
                {
                    incomingVal = _mergeFunc(currentVal, incomingVal);
                }
            }
            _cache[key] = incomingVal;
            OnDataChanged?.Invoke(key, incomingVal);
        }

        internal byte[] SerializeToBinary(TValue targetVal)
        {
            MemoryStream ms = ByteBufferPool.Rent();
            try
            {
                using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    if (_serialize != null)
                    {
                        _serialize(writer, targetVal);
                    }
                    else
                    {
                        _serializationStrategy.SerializeToBinary(targetVal);
                    }
                }
                return ms.ToArray();
            }
            finally
            {
                ByteBufferPool.Return(ms);
            }
        }

        internal TValue DeserializeFromBinary(byte[] rawData)
        {
            MemoryStream ms = ByteBufferPool.Rent();
            try
            {
                ms.Write(rawData, 0, rawData.Length);
                ms.Position = 0;
                using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8, true))
                {
                    if (_deserialize != null)
                    {
                        return _deserialize(reader);
                    }
                    return _serializationStrategy.DeserializeFromBinary(rawData);
                }
            }
            finally
            {
                ByteBufferPool.Return(ms);
            }
        }

        internal object SerializeToObject(TValue targetVal)
        {
            return _serializationStrategy.SerializeToObject(targetVal);
        }

        internal TValue DeserializeFromObject(object serializedObj)
        {
            return _serializationStrategy.DeserializeFromObject(serializedObj);
        }

        // ---- 实现 ISyncCache.GetSnapshot ----
        public PhotonHashtable GetSnapshot()
        {
            PhotonHashtable snapshot = new PhotonHashtable();
            foreach (KeyValuePair<TKey, TValue> kv in _cache)
            {
                snapshot[kv.Key] = kv.Value;
            }
            return snapshot;
        }

        // 内部保留，以备后用（但不再用于快照）
        internal ConcurrentDictionary<TKey, TValue> GetAllData()
        {
            return _cache;
        }

        // ---- ISyncCache 显式实现 ----
        void ISyncCache.ApplyRemoteSetObject(object keyObj, object valObj)
        {
            try
            {
                if (keyObj is TKey typedKey && valObj is TValue typedVal)
                {
                    ApplyRemoteSet(typedKey, typedVal);
                    return;
                }
                TKey convertedKey = (TKey)Convert.ChangeType(keyObj, typeof(TKey));
                TValue convertedVal = (TValue)Convert.ChangeType(valObj, typeof(TValue));
                ApplyRemoteSet(convertedKey, convertedVal);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ApplyRemoteSetObject 类型转换失败: {ex}");
            }
        }

        void ISyncCache.ApplyRemoteSetBinary(object keyObj, byte[] rawData)
        {
            try
            {
                TValue val;
                if (keyObj is TKey typedKey)
                {
                    val = DeserializeFromBinary(rawData);
                    ApplyRemoteSet(typedKey, val);
                    return;
                }
                TKey convertedKey = (TKey)Convert.ChangeType(keyObj, typeof(TKey));
                val = DeserializeFromBinary(rawData);
                ApplyRemoteSet(convertedKey, val);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ApplyRemoteSetBinary 类型转换失败: {ex}");
            }
        }

        void ISyncCache.ApplyRemoteRemove(object keyObj)
        {
            try
            {
                if (keyObj is TKey typedKey)
                {
                    ApplyRemoteRemove(typedKey);
                    return;
                }
                TKey convertedKey = (TKey)Convert.ChangeType(keyObj, typeof(TKey));
                ApplyRemoteRemove(convertedKey);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ApplyRemoteRemove 类型转换失败: {ex}");
            }
        }

        void ISyncCache.ApplyRemoteClear() => ApplyRemoteClear();

        void ISyncCache.ProcessMergeObject(object keyObj, object valObj)
        {
            try
            {
                if (keyObj is TKey typedKey && valObj is TValue typedVal)
                {
                    ApplyMerge(typedKey, typedVal);
                    return;
                }
                TKey convertedKey = (TKey)Convert.ChangeType(keyObj, typeof(TKey));
                TValue convertedVal = (TValue)Convert.ChangeType(valObj, typeof(TValue));
                ApplyMerge(convertedKey, convertedVal);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ProcessMergeObject 类型转换失败: {ex}");
            }
        }

        void ISyncCache.ProcessMergeBinary(object keyObj, byte[] rawData)
        {
            try
            {
                TValue incomingVal;
                if (keyObj is TKey typedKey)
                {
                    incomingVal = DeserializeFromBinary(rawData);
                    ApplyMerge(typedKey, incomingVal);
                    return;
                }
                TKey convertedKey = (TKey)Convert.ChangeType(keyObj, typeof(TKey));
                incomingVal = DeserializeFromBinary(rawData);
                ApplyMerge(convertedKey, incomingVal);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ProcessMergeBinary 类型转换失败: {ex}");
            }
        }

        void ISyncCache.SyncNow() => SyncNow();

        internal void ApplyRemoteSet(TKey key, TValue newVal)
        {
            _cache[key] = newVal;
            OnDataChanged?.Invoke(key, newVal);
        }

        internal void ApplyRemoteRemove(TKey key) => _cache.TryRemove(key, out _);
        internal void ApplyRemoteClear() => _cache.Clear();

        internal void SyncNow()
        {
        }
    }
}