using System;
using System.Collections.Generic;
using System.IO;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.Events;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private bool _hasBroadcastedForRoom = false;
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

            // 订阅场景变化事件（用于广播缓存）
            EventBus.Subscribe<SceneChangedEvent>(OnSceneChanged);
            // 订阅自定义请求事件（房主处理客户端请求）
            EventBus.Subscribe<CustomRequestEvent>(OnCustomRequestReceived);
            // 订阅自定义响应事件（客户端可接收响应，但本模组不直接处理）
            EventBus.Subscribe<CustomResponseEvent>(OnCustomResponseReceived);
            _ = SceneEventGenerator.Instance;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SceneChangedEvent>(OnSceneChanged);
            EventBus.Unsubscribe<CustomRequestEvent>(OnCustomRequestReceived);
            EventBus.Unsubscribe<CustomResponseEvent>(OnCustomResponseReceived);
        }

        private void TryInitializeState()
        {
            if (_isNetworkReady) return;

            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            {
                HandleJoinedRoom();
            }
            else
            {
                Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 实例已创建，当前未在房间内，等待 OnJoinedRoom。");
            }
        }

        private void TryBroadcastIfNeeded()
        {
            if (!_isNetworkReady)
            {
                // Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 网络未就绪，跳过广播尝试。");
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                // Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 非房主，跳过广播尝试。");
                return;
            }

            if (_hasBroadcastedForRoom)
            {
                // Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 当前房间已广播过，跳过。");
                return;
            }
            Scene currentScene = SceneManager.GetActiveScene();
            SceneType type = SceneEventGenerator.DetermineSceneType(currentScene);
            if (type == SceneType.MainMenu || type == SceneType.Unknown)
            {
                return;
            }
            BroadcastAllCachesToAll();
            _hasBroadcastedForRoom = true;
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 首次进入有效场景 {currentScene.name}，已广播所有缓存。");
        }


        // ---------- Photon 回调 ----------
        public override void OnJoinedRoom()
        {
            HandleJoinedRoom();
        }

        private void HandleJoinedRoom()
        {
            if (_isNetworkReady)
            {
                Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 已加入房间，但网络已就绪，跳过重复处理。");
                return;
            }

            _isNetworkReady = true;
            _hasBroadcastedForRoom = false;
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 已加入房间，网络就绪。");

            EventBus.Publish(new NetworkReadyEvent());
        }

        public override void OnLeftRoom()
        {
            _isNetworkReady = false;
            _hasBroadcastedForRoom = false;
            SyncRpcModule.Reset();
            EventBus.Publish(new LeftRoomEvent());
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 离开房间，网络未就绪。");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            _isNetworkReady = false;
            _hasBroadcastedForRoom = false;
            SyncRpcModule.Reset();
            EventBus.Publish(new LeftRoomEvent());
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 断开连接，网络未就绪。原因: {cause}");
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (!_isNetworkReady)
            {
                Core.CommonPlugin.Logger.LogError($"{LOG_TAG} OnMasterClientSwitched 时网络未就绪，忽略操作。");
                return;
            }
            if (newMasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 此客户端成为新主机，尝试广播缓存。");
                TryBroadcastIfNeeded();
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (!_isNetworkReady)
            {
                Core.CommonPlugin.Logger.LogError($"{LOG_TAG} OnPlayerEnteredRoom 时网络未就绪，忽略操作。");
                return;
            }
            if (PhotonNetwork.IsMasterClient)
            {
                Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 新玩家 {newPlayer.ActorNumber} 加入，发送全量缓存。");
                SendAllCachesToPlayer(newPlayer);
            }
        }

        private void OnSceneChanged(SceneChangedEvent evt)
        {
            TryBroadcastIfNeeded();
        }

        // ---------- 自定义请求/响应处理 ----------
        private void OnCustomRequestReceived(CustomRequestEvent evt)
        {
            // 仅房主处理请求
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (evt.Data is not PhotonHashtable data)
                return;

            if (!data.ContainsKey("type"))
                return;

            string type = (string)data["type"];
            if (type != "SyncCacheFullUpdateRequest")
                return;

            string cacheName = (string)data["cacheName"];
            object? requestVersion = data.ContainsKey("version") ? data["version"] : null;

            ISyncCache? cache = FindCacheByName(cacheName);
            if (cache == null)
            {
                Core.CommonPlugin.Logger.LogWarning($"收到未知缓存 '{cacheName}' 的全量更新请求，忽略。");
                return;
            }

            object? currentVersion = cache.Version;
            if (currentVersion != null && requestVersion != null && currentVersion.Equals(requestVersion))
            {
                Core.CommonPlugin.Logger.LogInfo($"缓存 '{cacheName}' 版本一致（{requestVersion}），无需响应。");
                return;
            }

            PhotonHashtable response = new PhotonHashtable
            {
                ["type"] = "SyncCacheFullUpdateResponse",
                ["cacheName"] = cacheName,
                ["version"] = currentVersion ?? 0
            };

            // 根据序列化策略发送快照
            if (cache.UseBinarySerialization && cache.TryGetSnapshotBinary(out Dictionary<object, byte[]> binarySnapshot))
            {
                response["binarySnapshot"] = binarySnapshot;
                Core.CommonPlugin.Logger.LogInfo($"向 Actor {evt.SenderActor} 发送二进制快照（缓存：{cacheName}，版本 {currentVersion ?? 0}）。");
            }
            else
            {
                PhotonHashtable snapshot = cache.GetSnapshot();
                if (snapshot.Count == 0)
                {
                    Core.CommonPlugin.Logger.LogInfo($"缓存 '{cacheName}' 快照为空，不发送响应。");
                    return;
                }
                response["snapshot"] = snapshot;
                Core.CommonPlugin.Logger.LogInfo($"向 Actor {evt.SenderActor} 发送快照（缓存：{cacheName}，版本 {currentVersion ?? 0}）。");
            }

            SyncRpcModule.SendCustomResponse(evt.SenderActor, response);
        }

        // 客户端收到响应的处理（通常不需要，但保留以防扩展）
        private void OnCustomResponseReceived(CustomResponseEvent evt)
        {
            if (evt.Data is not PhotonHashtable data)
                return;

            if (!data.ContainsKey("type"))
                return;

            string type = (string)data["type"];
            if (type != "SyncCacheFullUpdateResponse")
                return;

            string cacheName = (string)data["cacheName"];
            object? newVersion = data.ContainsKey("version") ? data["version"] : null;

            ISyncCache? cache = FindCacheByName(cacheName);
            if (cache == null)
            {
                Core.CommonPlugin.Logger.LogWarning($"收到未知缓存 '{cacheName}' 的响应，忽略。");
                return;
            }

            if (data.ContainsKey("binarySnapshot") && data["binarySnapshot"] is Dictionary<object, byte[]> binarySnapshot)
            {
                cache.ApplyRemoteClear();
                foreach (var kv in binarySnapshot)
                    cache.ApplyRemoteSetBinary(kv.Key, kv.Value);
                cache.Version = newVersion;
                Core.CommonPlugin.Logger.LogInfo($"已应用缓存 '{cacheName}' 的二进制全量快照，版本更新为 {newVersion ?? 0}。");
            }
            else if (data["snapshot"] is PhotonHashtable snapshot)
            {
                cache.ApplyRemoteClear();
                foreach (object key in snapshot.Keys)
                    cache.ApplyRemoteSetObject(key, snapshot[key]);
                cache.Version = newVersion;
                Core.CommonPlugin.Logger.LogInfo($"已应用缓存 '{cacheName}' 的全量快照，版本更新为 {newVersion ?? 0}。");
            }
        }

        // ---------- 快照广播 ----------
        private void SendAllCachesToPlayer(Player targetPlayer)
        {
            ISyncCache[] allCaches = CacheManager.GetAllSyncCaches();
            foreach (ISyncCache cache in allCaches)
            {
                if (cache.TryGetSnapshotBinary(out Dictionary<object, byte[]> binarySnapshot))
                {
                    SyncRpcModule.SendFullSnapshotBinaryToPlayer(cache.CacheName, binarySnapshot, targetPlayer.ActorNumber);
                }
                else
                {
                    PhotonHashtable snapshot = cache.GetSnapshot();
                    if (snapshot.Count == 0) continue;
                    SyncRpcModule.SendFullSnapshotToPlayer(cache.CacheName, snapshot, targetPlayer.ActorNumber);
                }
            }
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 已向玩家 {targetPlayer.ActorNumber} 发送所有缓存快照。");
        }

        private void BroadcastAllCachesToAll()
        {
            ISyncCache[] allCaches = CacheManager.GetAllSyncCaches();
            foreach (ISyncCache cache in allCaches)
            {
                if (cache.TryGetSnapshotBinary(out Dictionary<object, byte[]> binarySnapshot))
                {
                    SyncRpcModule.BroadcastFullSnapshotBinary(cache.CacheName, binarySnapshot);
                }
                else
                {
                    PhotonHashtable snapshot = cache.GetSnapshot();
                    if (snapshot.Count == 0) continue;
                    SyncRpcModule.BroadcastFullSnapshot(cache.CacheName, snapshot);
                }
            }
            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 已广播所有缓存快照给所有客户端。");
        }

        // ---------- 远程操作应用（由 SyncRpcModule 调用） ----------
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

        internal ISyncCache? FindCacheByName(string cacheName)
        {
            if (CacheManager.TryGetSyncCache(cacheName, out ISyncCache? cache))
                return cache;
            return null;
        }

        // ---------- 统一处理 RPC 操作（由 SyncRpcModule 调用） ----------
        internal void HandleRpcOperation(RpcMessage.SubOpCode op, string cacheName, object? key, object? value, int senderActor)
        {
            switch (op)
            {
                case RpcMessage.SubOpCode.ApplyData:
                    ApplyRemoteSet(cacheName, key!, value!);
                    break;
                case RpcMessage.SubOpCode.ApplyDataBinary:
                    ApplyRemoteSetBinary(cacheName, key!, (byte[])value!);
                    break;
                case RpcMessage.SubOpCode.ApplyRemove:
                    ApplyRemoteRemove(cacheName, key!);
                    break;
                case RpcMessage.SubOpCode.ApplyClear:
                    ApplyRemoteClear(cacheName);
                    break;
                case RpcMessage.SubOpCode.ApplyFullSnapshot:
                    ApplyFullSnapshot(cacheName, (PhotonHashtable)value!);
                    break;
                case RpcMessage.SubOpCode.ApplyFullSnapshotBinary:
                    ApplyFullSnapshotBinary(cacheName, (Dictionary<object, byte[]>)value!);
                    break;
                case RpcMessage.SubOpCode.ReceiveSnapshot:
                case RpcMessage.SubOpCode.ReceiveMergeRequest:
                    ApplyRemoteSet(cacheName, key!, value!);
                    break;
                case RpcMessage.SubOpCode.ReceiveSnapshotBinary:
                case RpcMessage.SubOpCode.ReceiveMergeRequestBinary:
                    ApplyRemoteSetBinary(cacheName, key!, (byte[])value!);
                    break;
                case RpcMessage.SubOpCode.ReceiveFullSnapshot:
                    ApplyFullSnapshot(cacheName, (PhotonHashtable)value!);
                    break;
                case RpcMessage.SubOpCode.ReceiveFullSnapshotBinary:
                    ApplyFullSnapshotBinary(cacheName, (Dictionary<object, byte[]>)value!);
                    break;
                case RpcMessage.SubOpCode.CustomRequest:
                case RpcMessage.SubOpCode.CustomRequestBinary:
                    // 通过事件发布，由 OnCustomRequestReceived 处理
                    EventBus.Publish(new CustomRequestEvent(value!, senderActor));
                    break;
                case RpcMessage.SubOpCode.CustomResponse:
                case RpcMessage.SubOpCode.CustomResponseBinary:
                    EventBus.Publish(new CustomResponseEvent(value!));
                    break;
                default:
                    Core.CommonPlugin.Logger.LogWarning($"{LOG_TAG} 未知 RPC 操作: {op}");
                    break;
            }
        }

        private void ApplyFullSnapshot(string cacheName, PhotonHashtable snapshot)
        {
            ISyncCache? cache = FindCacheByName(cacheName);
            if (cache == null) return;
            cache.ApplyRemoteClear();
            foreach (object key in snapshot.Keys)
                cache.ApplyRemoteSetObject(key, snapshot[key]);
        }

        private void ApplyFullSnapshotBinary(string cacheName, Dictionary<object, byte[]> snapshot)
        {
            ISyncCache? cache = FindCacheByName(cacheName);
            if (cache == null) return;
            cache.ApplyRemoteClear();
            foreach (var kv in snapshot)
                cache.ApplyRemoteSetBinary(kv.Key, kv.Value);
        }
        internal ISyncCache<TKey, TValue> CreateSyncCache<TKey, TValue>(
            string cacheName,
            SyncMode mode,
            Func<TValue, TValue, TValue>? mergeFunc = null,
            Action<BinaryWriter, TValue>? serialize = null,
            Func<BinaryReader, TValue>? deserialize = null,
            bool allowFullUpdateRequest = true)
            where TKey : notnull
        {
            SyncCache<TKey, TValue> newCache = new SyncCache<TKey, TValue>(
                cacheName, mode, mergeFunc, serialize, deserialize, allowFullUpdateRequest);

            // 事件绑定
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

            Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 缓存 '{cacheName}' 已创建（模式：{mode}，二进制序列化：{newCache.UseBinarySerialization}，允许请求：{allowFullUpdateRequest}）。");
            return newCache;
        }
    }
}