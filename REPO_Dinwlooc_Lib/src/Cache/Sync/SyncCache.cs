using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using Dinwlooc.Common.Reflection; // 新增

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
                _serializationStrategy = new BinaryStrategy<TValue>(serialize, deserialize);
            else
                _serializationStrategy = new HashtableStrategy<TValue>();
        }

        public bool TryGet(TKey key, out TValue outputVal) => _cache.TryGetValue(key, out outputVal);

        public void Set(TKey key, TValue inputVal, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out TValue oldVal) &&
                EqualityComparer<TValue>.Default.Equals(oldVal, inputVal))
                return;

            _cache[key] = inputVal;
            OnDataChanged?.Invoke(key, inputVal);
        }

        public bool Remove(TKey key)
        {
            bool removed = _cache.TryRemove(key, out _);
            if (removed) OnDataRemoved?.Invoke(key);
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
            if (_cache.TryGetValue(key, out TValue currentVal) && _mergeFunc != null)
                incomingVal = _mergeFunc(currentVal, incomingVal);
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
                        _serialize(writer, targetVal);
                    else
                        _serializationStrategy.SerializeToBinary(targetVal);
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
                        return _deserialize(reader);
                    return _serializationStrategy.DeserializeFromBinary(rawData);
                }
            }
            finally
            {
                ByteBufferPool.Return(ms);
            }
        }

        internal object SerializeToObject(TValue targetVal) => _serializationStrategy.SerializeToObject(targetVal);
        internal TValue DeserializeFromObject(object serializedObj) => _serializationStrategy.DeserializeFromObject(serializedObj);

        public PhotonHashtable GetSnapshot()
        {
            PhotonHashtable snapshot = new PhotonHashtable();
            foreach (var kv in _cache)
                snapshot[kv.Key] = kv.Value;
            return snapshot;
        }

        internal ConcurrentDictionary<TKey, TValue> GetAllData() => _cache;

        // ---- ISyncCache 显式实现 ----
        void ISyncCache.ApplyRemoteSetObject(object keyObj, object valObj)
        {
            try
            {
                // 使用 ReflectionCache 进行类型转换，避免异常
                object? convertedKeyObj = ReflectionCache.ChangeType(keyObj, typeof(TKey));
                object? convertedValObj = ReflectionCache.ChangeType(valObj, typeof(TValue));
                if (convertedKeyObj == null || convertedValObj == null)
                {
                    Core.CommonPlugin.Logger.LogError($"ApplyRemoteSetObject 转换失败：键或值为 null");
                    return;
                }
                TKey typedKey = (TKey)convertedKeyObj;
                TValue typedVal = (TValue)convertedValObj;
                ApplyRemoteSet(typedKey, typedVal);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ApplyRemoteSetObject 失败: {ex}");
            }
        }

        void ISyncCache.ApplyRemoteSetBinary(object keyObj, byte[] rawData)
        {
            try
            {
                TKey typedKey;
                if (keyObj is TKey key)
                    typedKey = key;
                else
                {
                    object? converted = ReflectionCache.ChangeType(keyObj, typeof(TKey));
                    if (converted == null)
                    {
                        Core.CommonPlugin.Logger.LogError($"ApplyRemoteSetBinary 键转换失败：{keyObj}");
                        return;
                    }
                    typedKey = (TKey)converted;
                }
                TValue val = DeserializeFromBinary(rawData);
                ApplyRemoteSet(typedKey, val);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ApplyRemoteSetBinary 失败: {ex}");
            }
        }

        void ISyncCache.ApplyRemoteRemove(object keyObj)
        {
            try
            {
                if (keyObj is TKey key)
                    ApplyRemoteRemove(key);
                else
                {
                    object? converted = ReflectionCache.ChangeType(keyObj, typeof(TKey));
                    if (converted != null)
                        ApplyRemoteRemove((TKey)converted);
                }
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ApplyRemoteRemove 失败: {ex}");
            }
        }

        void ISyncCache.ApplyRemoteClear() => ApplyRemoteClear();

        void ISyncCache.ProcessMergeObject(object keyObj, object valObj)
        {
            try
            {
                object? convertedKey = ReflectionCache.ChangeType(keyObj, typeof(TKey));
                object? convertedVal = ReflectionCache.ChangeType(valObj, typeof(TValue));
                if (convertedKey == null || convertedVal == null)
                {
                    Core.CommonPlugin.Logger.LogError($"ProcessMergeObject 转换失败：键或值为 null");
                    return;
                }
                ApplyMerge((TKey)convertedKey, (TValue)convertedVal);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ProcessMergeObject 失败: {ex}");
            }
        }

        void ISyncCache.ProcessMergeBinary(object keyObj, byte[] rawData)
        {
            try
            {
                TKey typedKey;
                if (keyObj is TKey key)
                    typedKey = key;
                else
                {
                    object? converted = ReflectionCache.ChangeType(keyObj, typeof(TKey));
                    if (converted == null)
                    {
                        Core.CommonPlugin.Logger.LogError($"ProcessMergeBinary 键转换失败：{keyObj}");
                        return;
                    }
                    typedKey = (TKey)converted;
                }
                TValue val = DeserializeFromBinary(rawData);
                ApplyMerge(typedKey, val);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ProcessMergeBinary 失败: {ex}");
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
        internal void SyncNow() { }
    }
}