using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

namespace Dinwlooc.Common.Sync
{
    public class SyncRegionManager : MonoBehaviour
    {
        private static SyncRegionManager? _instance;
        public static SyncRegionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject(nameof(SyncRegionManager));
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _instance = go.AddComponent<SyncRegionManager>();
                }
                return _instance;
            }
        }

        internal readonly ConcurrentDictionary<string, object> SyncCaches = new ConcurrentDictionary<string, object>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            EventBus.Subscribe<PlayerLevelEnteredEvent>(OnPlayerLevelEntered);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerLevelEnteredEvent>(OnPlayerLevelEntered);
        }

        public ISyncCache<TKey, TValue> GetOrCreateSyncCache<TKey, TValue>(
            string cacheName,
            SyncMode mode,
            Func<TValue, TValue, TValue>? mergeFunc = null,
            Action<BinaryWriter, TValue>? serialize = null,
            Func<BinaryReader, TValue>? deserialize = null) where TKey : notnull
        {
            if (SyncCaches.TryGetValue(cacheName, out object existing))
            {
                if (existing is SyncCache<TKey, TValue> typed)
                {
                    return typed;
                }
                throw new InvalidOperationException($"缓存 '{cacheName}' 已存在但类型不匹配。");
            }

            SyncCache<TKey, TValue> newCache = new SyncCache<TKey, TValue>(cacheName, mode, mergeFunc, serialize, deserialize);
            SyncCaches[cacheName] = newCache;
            Core.CommonPlugin.Logger.LogInfo($"同步缓存 '{cacheName}' 已创建（模式：{mode}）。");
            return newCache;
        }

        private void OnPlayerLevelEntered(PlayerLevelEnteredEvent evt)
        {
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                return;
            }

            foreach (KeyValuePair<string, object> kv in SyncCaches)
            {
                string cacheName = kv.Key;
                object cacheObj = kv.Value;
                Type cacheType = cacheObj.GetType();

                PropertyInfo useBinaryProp = cacheType.GetProperty("UseBinarySerialization");
                bool useBinary = useBinaryProp != null && (bool)useBinaryProp.GetValue(cacheObj);

                if (useBinary)
                {
                    MethodInfo? getAllBinary = cacheType.GetMethod("GetAllDataAsBinary", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (getAllBinary != null)
                    {
                        Dictionary<object, byte[]> binaryData = (Dictionary<object, byte[]>)getAllBinary.Invoke(cacheObj, null)!;
                        SyncRpcHelper.SendFullSnapshotBinaryToPlayer(cacheName, binaryData, evt.Player.photonView.ViewID);
                    }
                }
                else
                {
                    MethodInfo? getAllObjects = cacheType.GetMethod("GetAllDataAsObjects", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (getAllObjects != null)
                    {
                        Dictionary<object, object> objectData = (Dictionary<object, object>)getAllObjects.Invoke(cacheObj, null)!;
                        Hashtable hashtable = new Hashtable();
                        foreach (KeyValuePair<object, object> entry in objectData)
                        {
                            hashtable[entry.Key] = entry.Value;
                        }
                        SyncRpcHelper.SendFullSnapshotToPlayer(cacheName, hashtable, evt.Player.photonView.ViewID);
                    }
                }
            }
        }
    }
}