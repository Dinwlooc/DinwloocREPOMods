// 文件：Dinwlooc.Common/Sync/SyncManager.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.Core;

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

        internal readonly ConcurrentDictionary<string, ISyncCache> SyncCaches = new ConcurrentDictionary<string, ISyncCache>();
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
            Func<BinaryReader, TValue>? deserialize = null) where TKey : notnull
        {
            if (SyncCaches.TryGetValue(cacheName, out ISyncCache existing))
            {
                if (existing is SyncCache<TKey, TValue> typed)
                    return typed;
                throw new InvalidOperationException($"缓存 '{cacheName}' 已存在但类型不匹配。");
            }

            var newCache = new SyncCache<TKey, TValue>(cacheName, mode, mergeFunc, serialize, deserialize);
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

        public bool TryGetCache(string cacheName, out ISyncCache cache)
        {
            return SyncCaches.TryGetValue(cacheName, out cache);
        }

        // ---- Photon 回调 ----
        public override void OnJoinedRoom()
        {
            SyncRpcModule.EnsureInitialized();
            _isNetworkReady = true;
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 已加入房间，网络就绪。");

            if (PhotonNetwork.IsMasterClient)
            {
                Player? newPlayer = PhotonNetwork.CurrentRoom.Players.Values
                    .FirstOrDefault(p => p.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber && !p.IsInactive);
                if (newPlayer != null)
                    SendAllCachesToPlayer(newPlayer);
            }

            // 发布网络就绪事件
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

        // ---- 快照推送 ----
        private void SendAllCachesToPlayer(Player targetPlayer)
        {
            foreach (var kv in SyncCaches)
            {
                var snapshot = kv.Value.GetSnapshot();
                if (snapshot.Count == 0) continue;
                SyncRpcModule.SendFullSnapshotToPlayer(kv.Key, snapshot, targetPlayer.ActorNumber);
            }
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 已向玩家 {targetPlayer.ActorNumber} 发送所有缓存快照。");
        }

        private void BroadcastAllCachesToAll()
        {
            foreach (var kv in SyncCaches)
            {
                var snapshot = kv.Value.GetSnapshot();
                if (snapshot.Count == 0) continue;
                SyncRpcModule.BroadcastFullSnapshot(kv.Key, snapshot);
            }
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 已广播所有缓存快照给所有客户端。");
        }

        // ---- 远程操作入口 ----
        internal void ApplyRemoteSet(string cacheName, object key, object value)
        {
            if (SyncCaches.TryGetValue(cacheName, out var cache)) cache.ApplyRemoteSetObject(key, value);
        }

        internal void ApplyRemoteSetBinary(string cacheName, object key, byte[] data)
        {
            if (SyncCaches.TryGetValue(cacheName, out var cache)) cache.ApplyRemoteSetBinary(key, data);
        }

        internal void ApplyRemoteRemove(string cacheName, object key)
        {
            if (SyncCaches.TryGetValue(cacheName, out var cache)) cache.ApplyRemoteRemove(key);
        }

        internal void ApplyRemoteClear(string cacheName)
        {
            if (SyncCaches.TryGetValue(cacheName, out var cache)) cache.ApplyRemoteClear();
        }

        internal void ApplyMergeRequest(string cacheName, object key, object value)
        {
            if (SyncCaches.TryGetValue(cacheName, out var cache)) cache.ProcessMergeObject(key, value);
        }

        internal void ApplyMergeRequestBinary(string cacheName, object key, byte[] data)
        {
            if (SyncCaches.TryGetValue(cacheName, out var cache)) cache.ProcessMergeBinary(key, data);
        }

        public string[] GetAllCacheNames() => SyncCaches.Keys.ToArray();
    }
}