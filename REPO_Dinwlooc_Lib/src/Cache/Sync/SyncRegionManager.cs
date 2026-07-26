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
    /// <summary>
    /// 推送模式，决定在哪些场景自动向新玩家推送全量数据。
    /// </summary>
    public enum PushMode
    {
        /// <summary>
        /// 仅在游戏关卡（Level）场景中推送。
        /// </summary>
        LevelOnly,

        /// <summary>
        /// 在所有场景（包括商店、大厅等）中推送。
        /// </summary>
        AllScenes
    }

    /// <summary>
    /// 同步区域管理器，负责管理所有同步缓存的生命周期和网络事件。
    /// 采用懒加载，仅在首次调用 GetOrCreateSyncCache 时创建实例。
    /// 自动处理房间切换和主机变更，确保缓存数据与当前房间状态一致。
    /// 提供推送模式配置，支持全场景或仅关卡自动推送。
    /// </summary>
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

        internal readonly ConcurrentDictionary<string, object> SyncCaches = new ConcurrentDictionary<string, object>();

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

            // 注册两个生成器的步长（仅一次，重复调用无副作用）
            PlayerLevelEnterEventGenerator.Instance.RegisterStep(PUSH_STEP);
            PlayerJoinedEventGenerator.Instance.RegisterStep(PUSH_STEP);

            // 订阅两类事件，由推送模式决定实际处理
            EventBus.Subscribe<PlayerLevelEnteredEvent>(OnPlayerLevelEntered);
            EventBus.Subscribe<PlayerJoinedEvent>(OnPlayerJoined);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerLevelEnteredEvent>(OnPlayerLevelEntered);
            EventBus.Unsubscribe<PlayerJoinedEvent>(OnPlayerJoined);
            // 可选：取消生成器步长注册（但通常不必要，因为生成器是全局的）
        }

        /// <summary>
        /// 设置推送模式，决定向新玩家推送全量数据的场景范围。
        /// </summary>
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
            if (SyncCaches.TryGetValue(cacheName, out object existing))
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

        /// <summary>
        /// 清空所有同步缓存的数据（用于房间切换或重置）。
        /// </summary>
        public void ClearAllCaches()
        {
            foreach (KeyValuePair<string, object> kv in SyncCaches)
            {
                object cacheObj = kv.Value;
                if (cacheObj is ICacheProvider<object, object> typedCache)
                {
                    typedCache.Clear();
                }
                else
                {
                    Type cacheType = cacheObj.GetType();
                    MethodInfo? clearMethod = cacheType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);
                    clearMethod?.Invoke(cacheObj, null);
                }
            }
            Core.CommonPlugin.Logger.LogInfo("SyncRegionManager: 已清空所有同步缓存数据。");
        }

        // ----- 事件处理：根据推送模式决定是否推送 -----
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

        // ----- 封装的全量推送逻辑 -----
        private void PushFullSnapshotToPlayer(PlayerAvatar player)
        {
            if (player == null) return;
            if (SyncCaches.Count == 0) return;

            int targetViewId = player.photonView.ViewID;
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
                        SyncRpcModule.SendFullSnapshotBinaryToPlayer(cacheName, binaryData, targetViewId);
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
                        SyncRpcModule.SendFullSnapshotToPlayer(cacheName, hashtable, targetViewId);
                    }
                }
            }
        }

        // ----- Photon 回调（继承自 MonoBehaviourPunCallbacks）-----
        public override void OnMasterClientSwitched(Player newMaster)
        {
            // 无论主机切换到自己还是别人，都清空缓存（旧主机数据已失效）
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

    /// <summary>
    /// 主机切换事件，供模组监听并重新推送配置。
    /// </summary>
    public struct MasterClientSwitchedEvent { }
}