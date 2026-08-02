using System;
using System.IO;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.Networking;
using Dinwlooc.Common.Sync;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperEnergy
{
    /// <summary>
    /// 配置管理器：管理同步缓存，提供配置获取，支持客户端请求和超时降级。
    /// </summary>
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

        private bool _isWaitingForSync;
        private float _syncWaitStartTime;
        private bool _hasReceivedSyncData;
        private bool _hasRequestedAndTimedOut;
        private int _localVersion;
        private bool _hasPushedForRoom;

        protected override void Awake()
        {
            base.Awake();

            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _localVersion = 0;
            _hasPushedForRoom = false;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SuperEnergy.Logger.LogInfo("配置管理器已初始化。");
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            base.OnDestroy();
            _syncCache = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 仅当为关卡场景时执行同步相关操作
            if (!SemiFunc.RunIsLevel())
                return;

            SuperEnergy.Logger.LogInfo($"关卡加载：{scene.name}");

            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                bool useHost = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
                if (useHost && PhotonNetwork.IsMasterClient)
                {
                    EnsureSyncCacheCreated();
                    if (!_hasPushedForRoom)
                    {
                        PushCurrentConfigToCache();
                    }
                }
                else if (useHost && !PhotonNetwork.IsMasterClient)
                {
                    EnsureSyncCacheCreated();
                    _hasReceivedSyncData = false;
                    _isWaitingForSync = false;
                    _hasRequestedAndTimedOut = false;
                    RequestConfigFromHost();
                }
            }
            else
            {
                SuperEnergy.Logger.LogInfo("关卡加载时网络未就绪，延迟网络操作。");
            }
        }

        protected override void OnNetworkReady()
        {
            // 网络就绪时，若当前在关卡中则执行同步逻辑
            if (!SemiFunc.RunIsLevel())
                return;

            bool useHost = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
            if (useHost && PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                EnsureSyncCacheCreated();
                if (!_hasPushedForRoom)
                {
                    PushCurrentConfigToCache();
                }
            }
            else if (useHost && !PhotonNetwork.IsMasterClient)
            {
                EnsureSyncCacheCreated();
                _hasReceivedSyncData = false;
                _isWaitingForSync = false;
                _hasRequestedAndTimedOut = false;
                RequestConfigFromHost();
            }
        }

        protected override void OnLeftRoom()
        {
            ResetRoomState();
            base.OnLeftRoom();
        }

        private void ResetRoomState()
        {
            _isWaitingForSync = false;
            _hasReceivedSyncData = false;
            _hasRequestedAndTimedOut = false;
            _localVersion = 0;
            _hasPushedForRoom = false;
        }

        private void EnsureSyncCacheCreated()
        {
            if (_syncCache != null) return;

            _syncCache = CacheManager.GetOrCreateSyncCache<string, StaminaSyncConfig>(
                SYNC_CACHE_NAME,
                SyncMode.HostAuthority,
                serialize: (BinaryWriter w, StaminaSyncConfig c) => c.Write(w),
                deserialize: (BinaryReader r) => StaminaSyncConfig.Read(r)
                );
            _syncCache.OnDataChanged += OnSyncDataChanged;

            _localVersion = Convert.ToInt32(_syncCache.Version ?? 0);
            SuperEnergy.Logger.LogInfo($"同步缓存已创建，初始版本 {_localVersion}。");
        }

        private void PushCurrentConfigToCache()
        {
            if (_syncCache == null) return;

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

            _hasPushedForRoom = true;

            SuperEnergy.Logger.LogInfo($"房主推送配置：体力={syncConfig.Percent}%，滑铲={syncConfig.SlideBoostPercent}%，版本={_localVersion}");
        }

        private void OnSyncDataChanged(string key, StaminaSyncConfig config)
        {
            _localVersion = Convert.ToInt32(_syncCache?.Version ?? 0);

            if (PhotonNetwork.IsMasterClient)
            {
                return;
            }

            SuperEnergy.Logger.LogInfo($"收到房主配置更新：体力={config.Percent}%，滑铲={config.SlideBoostPercent}%，版本={_localVersion}");
            _hasReceivedSyncData = true;
            _isWaitingForSync = false;
            _hasRequestedAndTimedOut = false;
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

            if (_hasReceivedSyncData && _syncCache != null)
            {
                if (_syncCache.TryGet(SYNC_CACHE_KEY, out StaminaSyncConfig? cached))
                {
                    return cached;
                }
                SuperEnergy.Logger.LogWarning("同步缓存丢失，降级到本地配置。");
                return BuildFromLocalConfig(config);
            }

            if (_hasRequestedAndTimedOut)
            {
                return BuildFromLocalConfig(config);
            }

            if (!_isWaitingForSync)
            {
                _isWaitingForSync = true;
                _syncWaitStartTime = Time.realtimeSinceStartup;
                RequestConfigFromHost();
                SuperEnergy.Logger.LogInfo("请求房主配置，等待响应...");
                return BuildFromLocalConfig(config);
            }

            if (Time.realtimeSinceStartup - _syncWaitStartTime >= SYNC_TIMEOUT_SECONDS)
            {
                SuperEnergy.Logger.LogWarning($"超过 {SYNC_TIMEOUT_SECONDS} 秒未收到房主配置，降级到本地配置。");
                _hasRequestedAndTimedOut = true;
                _isWaitingForSync = false;
                return BuildFromLocalConfig(config);
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

        private void RequestConfigFromHost()
        {
            if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom)
            {
                SuperEnergy.Logger.LogWarning("请求配置失败：网络未就绪或不在房间内。");
                return;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                SuperEnergy.Logger.LogWarning("房主不应请求配置。");
                return;
            }

            if (_syncCache == null)
            {
                EnsureSyncCacheCreated();
            }

            _syncCache?.RequestFullUpdate(_localVersion);
            SuperEnergy.Logger.LogInfo($"已向房主发送配置请求，当前版本 {_localVersion}。");
        }

        public void OnConfigSettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
        {
            string key = e.ChangedSetting.Definition.Key;
            object? newValue = e.ChangedSetting.BoxedValue;
            SuperEnergy.Logger.LogInfo($"配置变更触发：{key} = {newValue}");

            if (key == "SyncUseHostConfig")
            {
                bool newValueBool = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
                SuperEnergy.Logger.LogInfo($"SyncUseHostConfig 变更为：{newValueBool}");

                if (newValueBool)
                {
                    if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && SemiFunc.RunIsLevel())
                    {
                        if (PhotonNetwork.IsMasterClient)
                        {
                            SuperEnergy.Logger.LogInfo("房主同步已启用，强制推送当前配置");
                            EnsureSyncCacheCreated();
                            PushCurrentConfigToCache();
                        }
                        else
                        {
                            SuperEnergy.Logger.LogInfo("客户端同步已启用，请求房主配置");
                            _hasReceivedSyncData = false;
                            _isWaitingForSync = false;
                            _hasRequestedAndTimedOut = false;
                            EnsureSyncCacheCreated();
                            RequestConfigFromHost();
                        }
                    }
                    else
                    {
                        SuperEnergy.Logger.LogInfo($"同步启用但网络未就绪或不在关卡：连接={PhotonNetwork.IsConnected}，在房间={PhotonNetwork.InRoom}，关卡加载={SemiFunc.RunIsLevel()}");
                    }
                }
                else
                {
                    SuperEnergy.Logger.LogInfo("同步已禁用，重置客户端状态");
                    _isWaitingForSync = false;
                    _hasReceivedSyncData = false;
                    _hasRequestedAndTimedOut = false;
                }
                return;
            }

            bool useHost = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
            bool isLevel = SemiFunc.RunIsLevel();
            if (useHost && PhotonNetwork.IsMasterClient && isLevel && PhotonNetwork.InRoom)
            {
                SuperEnergy.Logger.LogInfo($"配置变更 {key}，房主推送新配置");
                EnsureSyncCacheCreated();
                PushCurrentConfigToCache();
            }
            else
            {
                SuperEnergy.Logger.LogInfo($"配置变更 {key}，但条件不满足推送：同步启用={useHost}，是房主={PhotonNetwork.IsMasterClient}，关卡加载={isLevel}，在房间={PhotonNetwork.InRoom}");
            }
        }

        [Obsolete("请使用 Instance.GetEffectiveConfig()")]
        public static bool TryGetEffectiveConfig(out StaminaSyncConfig? config)
        {
            config = Instance.GetEffectiveConfig();
            return true;
        }

        public void Shutdown()
        {
            // 由 OnDestroy 处理
        }
    }
}