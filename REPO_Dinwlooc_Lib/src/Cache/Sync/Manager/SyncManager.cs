using System;
using System.IO;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.Events;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace Dinwlooc.Common.Sync
{
    public class SyncManager : MonoBehaviourPunCallbacks
    {
        private static SyncManager? _instance;
        private static readonly object _lock = new object();

        public static SyncManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            GameObject go = new GameObject(nameof(SyncManager));
                            DontDestroyOnLoad(go);
                            _instance = go.AddComponent<SyncManager>();
                        }
                    }
                }
                return _instance;
            }
        }

        private bool _isNetworkReady = false;
        private const string LOG_TAG = "[SyncManager]";

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

        public ISyncCache<TKey, TValue> GetOrCreateSyncCache<TKey, TValue>(
            string cacheName,
            SyncMode mode,
            Func<TValue, TValue, TValue>? mergeFunc = null,
            Action<BinaryWriter, TValue>? serialize = null,
            Func<BinaryReader, TValue>? deserialize = null)
            where TKey : notnull
        {
            // 先尝试从 CacheManager 获取（避免重复创建）
            ICacheProvider<TKey, TValue>? existing = CacheManager.GetCache<TKey, TValue>(cacheName);
            if (existing is ISyncCache<TKey, TValue> typed)
                return typed;

            var newCache = new SyncCache<TKey, TValue>(cacheName, mode, mergeFunc, serialize, deserialize);

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
                        SyncRpcModule.SendSnapshotBinary<TKey>(cacheName, key, newCache.SerializeToBinary(value));
                    else
                        SyncRpcModule.SendSnapshot<TKey, object>(cacheName, key, newCache.SerializeToObject(value));
                }
                else if (mode == SyncMode.Merge && !isHost)
                {
                    if (newCache.UseBinarySerialization)
                        SyncRpcModule.SendMergeRequestBinary<TKey>(cacheName, key, newCache.SerializeToBinary(value));
                    else
                        SyncRpcModule.SendMergeRequest<TKey, object>(cacheName, key, newCache.SerializeToObject(value));
                }
            };

            newCache.OnDataRemoved += (key) =>
            {
                if (!_isNetworkReady) return;
                bool isHost = PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom;
                if (isHost && (mode == SyncMode.HostAuthority || mode == SyncMode.Merge))
                    SyncRpcModule.BroadcastRemove<TKey>(cacheName, key);
            };

            newCache.OnDataCleared += () =>
            {
                if (!_isNetworkReady) return;
                bool isHost = PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom;
                if (isHost && (mode == SyncMode.HostAuthority || mode == SyncMode.Merge))
                    SyncRpcModule.BroadcastClear(cacheName);
            };

            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 缓存 '{cacheName}' 已创建（模式：{mode}）。");
            return newCache;
        }

        // ---- Photon 回调 ----
        public override void OnJoinedRoom()
        {
            SyncRpcModule.EnsureInitialized();
            _isNetworkReady = true;
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 已加入房间，网络就绪。");

            // 如果是主机，立即将现有缓存广播给已在房间的其他玩家
            if (PhotonNetwork.IsMasterClient)
            {
                BroadcastAllCachesToAll();
            }

            EventBus.Publish(new NetworkReadyEvent());
        }

        public override void OnLeftRoom()
        {
            _isNetworkReady = false;
            SyncRpcModule.Reset();
            EventBus.Publish(new LeftRoomEvent());
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 离开房间，网络未就绪。");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            _isNetworkReady = false;
            SyncRpcModule.Reset();
            EventBus.Publish(new LeftRoomEvent());
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 断开连接，网络未就绪。原因: {cause}");
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            SyncRpcModule.EnsureInitialized();
            if (newMasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 此客户端成为新主机，广播全量缓存。");
                BroadcastAllCachesToAll();
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (PhotonNetwork.IsMasterClient && _isNetworkReady)
            {
                Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 新玩家 {newPlayer.ActorNumber} 加入，发送全量缓存。");
                SendAllCachesToPlayer(newPlayer);
            }
        }

        // ---- 快照推送 ----
        private void SendAllCachesToPlayer(Player targetPlayer)
        {
            ISyncCache[] allCaches = CacheManager.GetAllSyncCaches();
            foreach (ISyncCache cache in allCaches)
            {
                PhotonHashtable snapshot = cache.GetSnapshot();
                if (snapshot.Count == 0) continue;
                SyncRpcModule.SendFullSnapshotToPlayer(cache.CacheName, snapshot, targetPlayer.ActorNumber);
            }
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 已向玩家 {targetPlayer.ActorNumber} 发送所有缓存快照。");
        }

        private void BroadcastAllCachesToAll()
        {
            ISyncCache[] allCaches = CacheManager.GetAllSyncCaches();
            foreach (ISyncCache cache in allCaches)
            {
                PhotonHashtable snapshot = cache.GetSnapshot();
                if (snapshot.Count == 0) continue;
                SyncRpcModule.BroadcastFullSnapshot(cache.CacheName, snapshot);
            }
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 已广播所有缓存快照给所有客户端。");
        }

        // ---- 远程操作入口（供 SyncRpcModule 调用） ----
        internal void ApplyRemoteSet(string cacheName, object key, object value)
        {
            ISyncCache? cache = FindCacheByName(cacheName);
            cache?.ApplyRemoteSetObject(key, value);
        }

        internal void ApplyRemoteSetBinary(string cacheName, object key, byte[] data)
        {
            ISyncCache? cache = FindCacheByName(cacheName);
            cache?.ApplyRemoteSetBinary(key, data);
        }

        internal void ApplyRemoteRemove(string cacheName, object key)
        {
            ISyncCache? cache = FindCacheByName(cacheName);
            cache?.ApplyRemoteRemove(key);
        }

        internal void ApplyRemoteClear(string cacheName)
        {
            ISyncCache? cache = FindCacheByName(cacheName);
            cache?.ApplyRemoteClear();
        }

        internal void ApplyMergeRequest(string cacheName, object key, object value)
        {
            ISyncCache? cache = FindCacheByName(cacheName);
            cache?.ProcessMergeObject(key, value);
        }

        internal void ApplyMergeRequestBinary(string cacheName, object key, byte[] data)
        {
            ISyncCache? cache = FindCacheByName(cacheName);
            cache?.ProcessMergeBinary(key, data);
        }

        /// <summary>
        /// 根据缓存名称查找同步缓存（内部使用）。
        /// </summary>
        internal ISyncCache? FindCacheByName(string cacheName)
        {
            if (CacheManager.TryGetSyncCache(cacheName, out ISyncCache? cache))
                return cache;
            return null;
        }
    }

}