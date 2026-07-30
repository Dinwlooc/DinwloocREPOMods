using System;
using System.Collections.Generic;
using System.IO;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.Reflection;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 同步缓存实现，包装 <see cref="MemoryCache{TKey,TValue}"/> 提供存储，
    /// 附加网络同步事件和远程操作支持。
    /// </summary>
    internal class SyncCache<TKey, TValue> : ISyncCache<TKey, TValue>
        where TKey : notnull
    {
        private readonly MemoryCache<TKey, TValue> _localCache;
        private readonly string _cacheName;
        private readonly SyncMode _mode;
        private readonly Func<TValue, TValue, TValue>? _mergeFunc;
        private readonly ISerializationStrategy<TValue> _serializationStrategy;

        public event Action<TKey, TValue>? OnDataChanged;
        public event Action<TKey>? OnDataRemoved;
        public event Action? OnDataCleared;

        public string CacheName => _cacheName;
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

            _localCache = new MemoryCache<TKey, TValue>();

            if (serialize != null && deserialize != null)
                _serializationStrategy = new BinaryStrategy<TValue>(serialize, deserialize);
            else
                _serializationStrategy = new HashtableStrategy<TValue>();
        }

        // ---- ICacheProvider 实现（全部委托给 _localCache） ----
        public bool TryGet(TKey key, out TValue value) => _localCache.TryGet(key, out value);

        public void Set(TKey key, TValue value, TimeSpan? expiration = null)
        {
            _localCache.Set(key, value, expiration);
            OnDataChanged?.Invoke(key, value);
        }

        public bool Remove(TKey key)
        {
            bool removed = _localCache.Remove(key);
            if (removed)
                OnDataRemoved?.Invoke(key);
            return removed;
        }

        public void Clear()
        {
            _localCache.Clear();
            OnDataCleared?.Invoke();
        }

        public void Refresh(TKey key) => _localCache.Refresh(key);

        // ---- 同步特有方法 ----
        internal void ApplyMerge(TKey key, TValue incomingVal)
        {
            if (_localCache.TryGet(key, out TValue currentVal) && _mergeFunc != null)
                incomingVal = _mergeFunc(currentVal, incomingVal);
            _localCache.Set(key, incomingVal, null);
            OnDataChanged?.Invoke(key, incomingVal);
        }

        internal byte[] SerializeToBinary(TValue value) => _serializationStrategy.SerializeToBinary(value);
        internal TValue DeserializeFromBinary(byte[] data) => _serializationStrategy.DeserializeFromBinary(data);
        internal object SerializeToObject(TValue value) => _serializationStrategy.SerializeToObject(value);
        internal TValue DeserializeFromObject(object obj) => _serializationStrategy.DeserializeFromObject(obj);

        public PhotonHashtable GetSnapshot()
        {
            PhotonHashtable snapshot = new PhotonHashtable();
            IReadOnlyDictionary<TKey, TValue> all = _localCache.GetAllItems();
            foreach (KeyValuePair<TKey, TValue> kv in all)
                snapshot[kv.Key] = kv.Value;
            return snapshot;
        }

        // ---- ISyncCache 显式实现（远程操作，不触发本地事件） ----
        void ISyncCache.ApplyRemoteSetObject(object keyObj, object valObj)
        {
            try
            {
                object? convertedKey = ReflectionCache.ChangeType(keyObj, typeof(TKey));
                object? convertedVal = ReflectionCache.ChangeType(valObj, typeof(TValue));
                if (convertedKey == null || convertedVal == null)
                {
                    Core.CommonPlugin.Logger.LogError($"ApplyRemoteSetObject 转换失败：键或值为 null");
                    return;
                }
                TKey typedKey = (TKey)convertedKey;
                TValue typedVal = (TValue)convertedVal;
                _localCache.Set(typedKey, typedVal, null);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ApplyRemoteSetObject 失败: {ex}");
            }
        }

        void ISyncCache.ApplyRemoteSetBinary(object keyObj, byte[] data)
        {
            try
            {
                object? convertedKey = ReflectionCache.ChangeType(keyObj, typeof(TKey));
                if (convertedKey == null)
                {
                    Core.CommonPlugin.Logger.LogError($"ApplyRemoteSetBinary 键转换失败：{keyObj}");
                    return;
                }
                TKey typedKey = (TKey)convertedKey;
                TValue val = DeserializeFromBinary(data);
                _localCache.Set(typedKey, val, null);
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
                object? convertedKey = ReflectionCache.ChangeType(keyObj, typeof(TKey));
                if (convertedKey != null)
                    _localCache.Remove((TKey)convertedKey);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ApplyRemoteRemove 失败: {ex}");
            }
        }

        void ISyncCache.ApplyRemoteClear() => _localCache.Clear();

        void ISyncCache.ProcessMergeObject(object keyObj, object valObj)
        {
            try
            {
                object? convertedKey = ReflectionCache.ChangeType(keyObj, typeof(TKey));
                object? convertedVal = ReflectionCache.ChangeType(valObj, typeof(TValue));
                if (convertedKey == null || convertedVal == null)
                {
                    Core.CommonPlugin.Logger.LogError($"ProcessMergeObject 转换失败");
                    return;
                }
                ApplyMerge((TKey)convertedKey, (TValue)convertedVal);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ProcessMergeObject 失败: {ex}");
            }
        }

        void ISyncCache.ProcessMergeBinary(object keyObj, byte[] data)
        {
            try
            {
                object? convertedKey = ReflectionCache.ChangeType(keyObj, typeof(TKey));
                if (convertedKey == null)
                {
                    Core.CommonPlugin.Logger.LogError($"ProcessMergeBinary 键转换失败");
                    return;
                }
                TValue val = DeserializeFromBinary(data);
                ApplyMerge((TKey)convertedKey, val);
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"ProcessMergeBinary 失败: {ex}");
            }
        }

        void ISyncCache.SyncNow() { /* 预留 */ }
    }
}