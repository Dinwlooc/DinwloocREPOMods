using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Dinwlooc.Common.Sync
{
    public class SyncRegionManager : MonoBehaviourPunCallbacks
    {
        private static SyncRegionManager? _instance;
        private static readonly object _lock = new object();

        public static SyncRegionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            GameObject go = new GameObject(nameof(SyncRegionManager));
                            DontDestroyOnLoad(go);
                            _instance = go.AddComponent<SyncRegionManager>();
                        }
                    }
                }
                return _instance;
            }
        }

        internal readonly ConcurrentDictionary<string, ISyncCache> SyncCaches = new ConcurrentDictionary<string, ISyncCache>();
        private bool _isNetworkReady = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!_isNetworkReady && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                _isNetworkReady = true;
                Core.CommonPlugin.Logger.LogInfo("SyncRegionManager 检测到网络就绪（已加入房间）。");
            }
        }

        public ISyncCache<TKey, TValue> GetOrCreateSyncCache<TKey, TValue>(
            string cacheName,
            SyncMode mode,
            Func<TValue, TValue, TValue>? mergeFunc = null,
            Action<BinaryWriter, TValue>? serialize = null,
            Func<BinaryReader, TValue>? deserialize = null) where TKey : notnull
        {
            if (SyncCaches.TryGetValue(cacheName, out ISyncCache existing))
            {
                if (existing is SyncCache<TKey, TValue> typed)
                {
                    return typed;
                }
                throw new InvalidOperationException($"缓存 '{cacheName}' 已存在但类型不匹配。");
            }

            SyncCache<TKey, TValue> newCache = new SyncCache<TKey, TValue>(cacheName, mode, mergeFunc, serialize, deserialize);
            SyncCaches[cacheName] = newCache;

            newCache.OnDataChanged += (key, value) =>
            {
                if (!_isNetworkReady) return;
                bool isHost = PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom;
                bool canBroadcast = isHost || (mode == SyncMode.ClientSnapshot) || (mode == SyncMode.Merge);
                if (!canBroadcast) return;

                if (isHost && (mode == SyncMode.HostAuthority || mode == SyncMode.Merge))
                {
                    if (newCache.UseBinarySerialization)
                    {
                        byte[] data = newCache.SerializeToBinary(value);
                        SyncRpcModule.BroadcastDataBinary<TKey>(cacheName, key, data);
                    }
                    else
                    {
                        object data = newCache.SerializeToObject(value);
                        SyncRpcModule.BroadcastData<TKey, object>(cacheName, key, data);
                    }
                }
                else if (mode == SyncMode.ClientSnapshot && !isHost)
                {
                    if (newCache.UseBinarySerialization)
                    {
                        byte[] data = newCache.SerializeToBinary(value);
                        SyncRpcModule.SendSnapshotBinary<TKey>(cacheName, key, data);
                    }
                    else
                    {
                        object data = newCache.SerializeToObject(value);
                        SyncRpcModule.SendSnapshot<TKey, object>(cacheName, key, data);
                    }
                }
                else if (mode == SyncMode.Merge && !isHost)
                {
                    if (newCache.UseBinarySerialization)
                    {
                        byte[] data = newCache.SerializeToBinary(value);
                        SyncRpcModule.SendMergeRequestBinary<TKey>(cacheName, key, data);
                    }
                    else
                    {
                        object data = newCache.SerializeToObject(value);
                        SyncRpcModule.SendMergeRequest<TKey, object>(cacheName, key, data);
                    }
                }
            };

            newCache.OnDataRemoved += (key) =>
            {
                if (!_isNetworkReady) return;
                bool isHost = PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom;
                if (isHost && (mode == SyncMode.HostAuthority || mode == SyncMode.Merge))
                {
                    SyncRpcModule.BroadcastRemove<TKey>(cacheName, key);
                }
            };

            newCache.OnDataCleared += () =>
            {
                if (!_isNetworkReady) return;
                bool isHost = PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom;
                if (isHost && (mode == SyncMode.HostAuthority || mode == SyncMode.Merge))
                {
                    SyncRpcModule.BroadcastClear(cacheName);
                }
            };

            Core.CommonPlugin.Logger.LogInfo($"同步缓存 '{cacheName}' 已创建（模式：{mode}）。");
            return newCache;
        }

        public bool TryGetCache(string cacheName, out ISyncCache cache)
        {
            return SyncCaches.TryGetValue(cacheName, out cache);
        }

        public override void OnJoinedRoom()
        {
            _isNetworkReady = true;
            // 重置 SyncRpcModule 以确保监听器被正确注册（处理网络重连）
            SyncRpcModule.Reset();
            Core.CommonPlugin.Logger.LogInfo("SyncRegionManager 已加入房间，网络就绪。");
        }

        public override void OnLeftRoom()
        {
            _isNetworkReady = false;
            Core.CommonPlugin.Logger.LogInfo("SyncRegionManager 离开房间，网络未就绪。");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            _isNetworkReady = false;
            Core.CommonPlugin.Logger.LogInfo($"SyncRegionManager 断开连接，网络未就绪。原因: {cause}");
        }
    }
}