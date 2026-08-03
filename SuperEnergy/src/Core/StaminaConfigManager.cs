using System;
using System.IO;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.Networking;
using Dinwlooc.Common.Sync;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperEnergy
{
    public class StaminaConfigManager : NetworkBehaviour
    {
        private static StaminaConfigManager? _instance;
        public static StaminaConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject(nameof(StaminaConfigManager));
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<StaminaConfigManager>();
                }
                return _instance;
            }
        }

        private const string SYNC_CACHE_NAME = "SuperEnergy_SyncConfig";
        private const string SYNC_CACHE_KEY = "current";
        private const float SYNC_TIMEOUT_SECONDS = 5f;

        private ISyncCache<string, StaminaSyncConfig>? _syncCache;
        private bool _hasSubscribedToDataChanged = false;

        private enum SyncState
        {
            None,
            WaitingForHost,
            Received,
            TimedOut
        }
        private SyncState _syncState = SyncState.None;
        private float _syncWaitStartTime = 0f;
        private int _localVersion = 0;
        private bool _hasPushedForRoom = false;
        private string _currentRoomIdentifier = "";

        protected void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _ = SceneEventGenerator.Instance;
            EventBus.Subscribe<SceneChangedEvent>(OnSceneChanged);

            // 不再主动访问 SyncManager，懒加载由 CacheManager 触发

            SuperEnergy.Logger.LogInfo("配置管理器已初始化。");
        }

        protected void OnDestroy()
        {
            EventBus.Unsubscribe<SceneChangedEvent>(OnSceneChanged);
            UnsubscribeFromSyncEvents();
            _syncCache = null;
        }

        private void OnSceneChanged(SceneChangedEvent evt)
        {
            if (evt.Type != SceneType.Level)
                return;

            // 直接尝试初始化，内部会通过创建缓存触发 SyncManager 实例化
            TryInitializeSyncForCurrentRoom();
        }

        protected override void OnNetworkReady()
        {
            base.OnNetworkReady();
            SuperEnergy.Logger.LogInfo("网络就绪，尝试初始化同步。");

            if (SemiFunc.RunIsLevel())
            {
                TryInitializeSyncForCurrentRoom();
            }
            else
            {
                SuperEnergy.Logger.LogInfo("网络就绪但非关卡场景，等待场景切换。");
            }
        }

        protected override void OnLeftRoom()
        {
            ResetRoomState();
            base.OnLeftRoom();
        }

        private void TryInitializeSyncForCurrentRoom()
        {
            if (!PhotonNetwork.InRoom)
            {
                SuperEnergy.Logger.LogInfo("未在房间中，延迟同步初始化。");
                return;
            }

            string roomIdentifier = $"{PhotonNetwork.CurrentRoom?.Name}_{SceneManager.GetActiveScene().name}";
            if (_currentRoomIdentifier == roomIdentifier)
            {
                SuperEnergy.Logger.LogInfo("当前房间已初始化过同步，跳过。");
                return;
            }
            _currentRoomIdentifier = roomIdentifier;

            bool useHost = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
            if (!useHost)
            {
                SuperEnergy.Logger.LogInfo("同步配置未启用，跳过。");
                return;
            }

            EnsureSyncCacheCreated();

            if (PhotonNetwork.IsMasterClient)
            {
                if (!_hasPushedForRoom)
                {
                    PushCurrentConfigToCache();
                    _hasPushedForRoom = true;
                }
                else
                {
                    SuperEnergy.Logger.LogInfo("房主已推送过当前房间配置。");
                }
            }
            else
            {
                if (_syncState == SyncState.Received)
                {
                    SuperEnergy.Logger.LogInfo("客户端已收到配置，无需重复请求。");
                    return;
                }

                if (_syncState == SyncState.WaitingForHost)
                {
                    if (Time.realtimeSinceStartup - _syncWaitStartTime >= SYNC_TIMEOUT_SECONDS)
                    {
                        SuperEnergy.Logger.LogWarning("等待房主配置超时，切换至本地配置。");
                        _syncState = SyncState.TimedOut;
                    }
                    else
                    {
                        SuperEnergy.Logger.LogInfo("客户端正在等待房主配置。");
                        return;
                    }
                }

                RequestConfigFromHost();
                _syncState = SyncState.WaitingForHost;
                _syncWaitStartTime = Time.realtimeSinceStartup;
            }
        }

        private void EnsureSyncCacheCreated()
        {
            if (_syncCache != null)
                return;

            // 调用 GetOrCreateSyncCache 会触发 SyncManager 的实例化和 EnsureReady
            ICacheProvider<string, StaminaSyncConfig>? existing = CacheManager.GetCache<string, StaminaSyncConfig>(SYNC_CACHE_NAME);
            if (existing != null && existing is ISyncCache<string, StaminaSyncConfig> syncCache)
            {
                _syncCache = syncCache;
                _localVersion = Convert.ToInt32(_syncCache.Version ?? 0);
                SubscribeToSyncEvents();
                SuperEnergy.Logger.LogInfo($"复用已有同步缓存，版本 {_localVersion}。");
                return;
            }

            _syncCache = CacheManager.GetOrCreateSyncCache<string, StaminaSyncConfig>(
                SYNC_CACHE_NAME,
                SyncMode.HostAuthority,
                serialize: (BinaryWriter w, StaminaSyncConfig c) => c.Write(w),
                deserialize: (BinaryReader r) => StaminaSyncConfig.Read(r)
            );
            _localVersion = Convert.ToInt32(_syncCache.Version ?? 0);
            SubscribeToSyncEvents();
            SuperEnergy.Logger.LogInfo($"同步缓存已创建，版本 {_localVersion}。");
        }

        private void SubscribeToSyncEvents()
        {
            if (_syncCache == null || _hasSubscribedToDataChanged)
                return;

            _syncCache.OnDataChanged += OnSyncDataChanged;
            _hasSubscribedToDataChanged = true;
        }

        private void UnsubscribeFromSyncEvents()
        {
            if (_syncCache != null && _hasSubscribedToDataChanged)
            {
                _syncCache.OnDataChanged -= OnSyncDataChanged;
                _hasSubscribedToDataChanged = false;
            }
        }

        private void OnSyncDataChanged(string key, StaminaSyncConfig config)
        {
            _localVersion = Convert.ToInt32(_syncCache?.Version ?? 0);

            if (!PhotonNetwork.IsMasterClient)
            {
                SuperEnergy.Logger.LogInfo($"收到房主配置：体力={config.Percent}%，滑铲={config.SlideBoostPercent}%，版本={_localVersion}");
                _syncState = SyncState.Received;
                _syncWaitStartTime = 0f;
            }
            else
            {
                SuperEnergy.Logger.LogInfo($"房主本地配置更新，版本 {_localVersion}");
            }
        }

        private void PushCurrentConfigToCache()
        {
            if (_syncCache == null)
                return;

            SuperEnergyConfig config = SuperEnergyConfig.Instance;
            StaminaSyncConfig syncConfig = new StaminaSyncConfig(
                config.StaminaBoostPercent.Value,
                config.StaminaBoostCompensateWhenDisabled.Value,
                config.StaminaBoostEnableCrouchBoost.Value,
                config.SlideBoostPercent.Value
            );

            _localVersion++;
            _syncCache.Version = _localVersion;
            _syncCache.Set(SYNC_CACHE_KEY, syncConfig);

            SuperEnergy.Logger.LogInfo($"房主推送配置：体力={syncConfig.Percent}%，滑铲={syncConfig.SlideBoostPercent}%，版本={_localVersion}");
        }

        private void RequestConfigFromHost()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
            {
                SuperEnergy.Logger.LogWarning("请求配置失败：未在房间中或是房主。");
                return;
            }

            if (_syncCache == null)
                EnsureSyncCacheCreated();

            _syncCache?.RequestFullUpdate(_localVersion);
            SuperEnergy.Logger.LogInfo($"已向房主请求配置，当前版本 {_localVersion}。");
        }

        private void ResetRoomState()
        {
            _syncState = SyncState.None;
            _syncWaitStartTime = 0f;
            _hasPushedForRoom = false;
            _localVersion = 0;
            _currentRoomIdentifier = "";
            SuperEnergy.Logger.LogInfo("房间状态已重置。");
        }

        public StaminaSyncConfig GetEffectiveConfig()
        {
            SuperEnergyConfig config = SuperEnergyConfig.Instance;
            bool useHost = config.SyncUseHostConfig.Value;
            bool isHost = PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom;

            if (!useHost || isHost)
            {
                return BuildFromLocalConfig(config);
            }

            if (_syncState == SyncState.Received && _syncCache != null)
            {
                if (_syncCache.TryGet(SYNC_CACHE_KEY, out StaminaSyncConfig? cached) && cached != null)
                {
                    return cached;
                }
                SuperEnergy.Logger.LogWarning("同步缓存丢失，降级本地。");
                _syncState = SyncState.None;
            }

            return BuildFromLocalConfig(config);
        }

        private StaminaSyncConfig BuildFromLocalConfig(SuperEnergyConfig config)
        {
            return new StaminaSyncConfig(
                config.StaminaBoostPercent.Value,
                config.StaminaBoostCompensateWhenDisabled.Value,
                config.StaminaBoostEnableCrouchBoost.Value,
                config.SlideBoostPercent.Value
            );
        }

        public void OnConfigSettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
        {
            string key = e.ChangedSetting.Definition.Key;
            SuperEnergy.Logger.LogInfo($"配置变更：{key} = {e.ChangedSetting.BoxedValue}");

            if (key == "SyncUseHostConfig")
            {
                bool newValue = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
                SuperEnergy.Logger.LogInfo($"SyncUseHostConfig 变更为 {newValue}");

                if (newValue)
                {
                    ResetRoomState();
                    TryInitializeSyncForCurrentRoom();
                }
                else
                {
                    ResetRoomState();
                    SuperEnergy.Logger.LogInfo("同步已禁用，使用本地配置。");
                }
                return;
            }

            bool useHost = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
            if (useHost && PhotonNetwork.IsMasterClient && SemiFunc.RunIsLevel() && PhotonNetwork.InRoom)
            {
                EnsureSyncCacheCreated();
                PushCurrentConfigToCache();
            }
        }
    }
}