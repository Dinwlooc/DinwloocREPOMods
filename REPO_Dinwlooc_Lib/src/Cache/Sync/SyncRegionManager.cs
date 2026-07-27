using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Dinwlooc.Common.Sync
{
    public enum PushMode
    {
        LevelOnly,
        AllScenes
    }

    public class SyncRegionManager : MonoBehaviourPunCallbacks
    {
        private static SyncRegionManager? _instance;
        private static bool _initializing = false;
        private static readonly object _lock = new object();

        public static SyncRegionManager Instance
        {
            get
            {
                if (_instance == null && !_initializing)
                {
                    lock (_lock)
                    {
                        if (_instance == null && !_initializing)
                        {
                            _initializing = true;
                            GameObject go = new GameObject(nameof(SyncRegionManager));
                            DontDestroyOnLoad(go);
                            _instance = go.AddComponent<SyncRegionManager>();
                            _initializing = false;
                        }
                    }
                }
                return _instance!;
            }
        }

        // 存储非泛型 ISyncCache 供 RPC 处理器使用
        internal readonly ConcurrentDictionary<string, ISyncCache> SyncCaches = new ConcurrentDictionary<string, ISyncCache>();

        private const int PUSH_STEP = 10;
        private PushMode _pushMode = PushMode.LevelOnly;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            PlayerLevelEnterEventGenerator.Instance.RegisterStep(PUSH_STEP);
            PlayerJoinedEventGenerator.Instance.RegisterStep(PUSH_STEP);

            EventBus.Subscribe<PlayerLevelEnteredEvent>(OnPlayerLevelEntered);
            EventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerLevelEnteredEvent>(OnPlayerLevelEntered);
            EventBus.Unsubscribe<PlayerJoinedEvent>(OnPlayerJoined);
        }

        public void SetPushMode(PushMode mode)
        {
            _pushMode = mode;
            Core.CommonPlugin.Logger.LogInfo($"SyncRegionManager: 推送模式切换为 {mode}");
        }

        public ISyncCache<TKey, TValue> GetOrCreateSyncCache<TKey, TValue>(
            string cacheName,
            SyncMode mode,
            Func<TValue, TValue, TValue>? mergeFunc = null,
            Action<BinaryWriter, TValue>? serialize = null,
            Func<BinaryReader, TValue>? deserialize = null) where TKey : notnull
        {
            // 修复：使用 out ISyncCache 而非 out object
            if (SyncCaches.TryGetValue(cacheName, out ISyncCache existing))
            {
                if (existing is SyncCache<TKey, TValue> typed)
                    return typed;
                throw new InvalidOperationException($"缓存 '{cacheName}' 已存在但类型不匹配。");
            }

            SyncCache<TKey, TValue> newCache = new SyncCache<TKey, TValue>(cacheName, mode, mergeFunc, serialize, deserialize);
            SyncCaches[cacheName] = newCache;
            Core.CommonPlugin.Logger.LogInfo($"同步缓存 '{cacheName}' 已创建（模式：{mode}）。");
            return newCache;
        }

        public void ClearAllCaches()
        {
            foreach (KeyValuePair<string, ISyncCache> kv in SyncCaches)
            {
                kv.Value.ApplyRemoteClear();
            }
            Core.CommonPlugin.Logger.LogInfo("SyncRegionManager: 已清空所有同步缓存数据。");
        }

        private void OnPlayerLevelEntered(PlayerLevelEnteredEvent evt)
        {
            if (_pushMode != PushMode.LevelOnly)
                return;
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
                return;
            PushFullSnapshotToPlayer(evt.Player);
        }

        private void OnPlayerJoined(PlayerJoinedEvent evt)
        {
            if (_pushMode != PushMode.AllScenes)
                return;
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
                return;
            PushFullSnapshotToPlayer(evt.Player);
        }

        private void PushFullSnapshotToPlayer(PlayerAvatar player)
        {
            if (player == null) return;
            if (SyncCaches.Count == 0) return;

            int targetViewId = player.photonView.ViewID;
            foreach (KeyValuePair<string, ISyncCache> kv in SyncCaches)
            {
                string cacheName = kv.Key;
                ISyncCache cache = kv.Value;
                Type cacheType = cache.GetType();

                PropertyInfo useBinaryProp = cacheType.GetProperty("UseBinarySerialization");
                bool useBinary = useBinaryProp != null && (bool)useBinaryProp.GetValue(cache)!;

                if (useBinary)
                {
                    MethodInfo? getAllBinary = cacheType.GetMethod("GetAllDataAsBinary", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (getAllBinary != null)
                    {
                        Dictionary<object, byte[]> binaryData = (Dictionary<object, byte[]>)getAllBinary.Invoke(cache, null)!;
                        SyncRpcModule.SendFullSnapshotBinaryToPlayer(cacheName, binaryData, targetViewId);
                    }
                }
                else
                {
                    MethodInfo? getAllObjects = cacheType.GetMethod("GetAllDataAsObjects", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (getAllObjects != null)
                    {
                        Dictionary<object, object> objectData = (Dictionary<object, object>)getAllObjects.Invoke(cache, null)!;
                        Hashtable hashtable = new Hashtable();
                        foreach (KeyValuePair<object, object> entry in objectData)
                        {
                            hashtable[entry.Key] = entry.Value;
                        }
                        SyncRpcModule.SendFullSnapshotToPlayer(cacheName, hashtable, targetViewId);
                    }
                }
            }
        }

        public override void OnMasterClientSwitched(Player newMaster)
        {
            ClearAllCaches();
            EventBus.Publish(new MasterClientSwitchedEvent());
            Core.CommonPlugin.Logger.LogInfo($"SyncRegionManager: 主机切换为 {newMaster.NickName}，缓存已清空。");
        }

        public override void OnJoinedRoom()
        {
            Core.CommonPlugin.Logger.LogInfo("SyncRegionManager: 已加入房间。");
        }

        public override void OnLeftRoom()
        {
            ClearAllCaches();
            Core.CommonPlugin.Logger.LogInfo("SyncRegionManager: 已离开房间，缓存已清空。");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            ClearAllCaches();
            Core.CommonPlugin.Logger.LogInfo($"SyncRegionManager: 已断开连接 (原因: {cause})，缓存已清空。");
        }
    }

    public struct MasterClientSwitchedEvent { }
}