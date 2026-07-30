using System;
using System.IO;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.Networking;
using Dinwlooc.Common.Sync;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperEnergy
{
    /// <summary>
    /// 配置管理器：管理同步缓存，提供配置获取，支持客户端请求和超时降级。
    /// 继承 <see cref="NetworkBehaviour"/> 自动处理网络事件订阅/取消。
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

        // ---- 常量 ----
        private const string SYNC_CACHE_NAME = "SuperEnergy_SyncConfig";
        private const string SYNC_CACHE_KEY = "current";
        private const float SYNC_TIMEOUT_SECONDS = 5f;
        private const byte REQUEST_EVENT_CODE = 250;
        private const string REQUEST_CONTENT = "RequestConfig";

        // ---- 缓存 ----
        private ISyncCache<string, StaminaSyncConfig>? _syncCache;

        // ---- 状态 ----
        private bool _isLevelLoaded;
        private bool _isWaitingForSync;
        private float _syncWaitStartTime;
        private bool _hasReceivedSyncData;
        private bool _hasRequestedAndTimedOut;

        // ---- 初始化 ----
        protected override void Awake()
        {
            base.Awake(); // 确保基类订阅事件

            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
            SuperEnergy.Logger.LogInfo("配置管理器已初始化（依赖 NetworkBehaviour）。");
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            base.OnDestroy();
            _syncCache = null;
        }

        // ---- 场景事件 ----
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (SemiFunc.RunIsLevel())
            {
                _isLevelLoaded = true;
                SuperEnergy.Logger.LogInfo($"关卡加载：{scene.name}");

                // 网络是否就绪由基类状态决定，但我们需要检查 Photon 连接状态
                if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
                {
                    bool useHost = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
                    if (useHost && PhotonNetwork.IsMasterClient)
                    {
                        EnsureSyncCacheCreated();
                        PushCurrentConfigToCache();
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
            else
            {
                _isLevelLoaded = false;
            }
        }

        // ---- NetworkBehaviour 重写 ----
        protected override void OnNetworkReady()
        {
            // 网络就绪时，若关卡已加载且同步开启，则执行相应操作
            if (!_isLevelLoaded) return;

            bool useHost = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
            if (useHost && PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                EnsureSyncCacheCreated();
                PushCurrentConfigToCache();
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
            _isWaitingForSync = false;
            _hasReceivedSyncData = false;
            _hasRequestedAndTimedOut = false;
            // 基类已注销 Photon 回调，无需额外操作
        }

        // ---- 自定义网络事件（处理客户端请求） ----
        public override void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != REQUEST_EVENT_CODE) return;
            if (photonEvent.CustomData is string content && content == REQUEST_CONTENT)
            {
                bool useHost = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
                if (PhotonNetwork.IsMasterClient && _isLevelLoaded && useHost)
                {
                    EnsureSyncCacheCreated();
                    PushCurrentConfigToCache();
                    SuperEnergy.Logger.LogInfo("响应客户端配置请求，已推送当前配置。");
                }
            }
        }

        // ---- 同步缓存管理 ----
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

            SuperEnergy.Logger.LogInfo("同步缓存已创建。");
        }

        private void PushCurrentConfigToCache()
        {
            if (_syncCache == null) return;

            SuperEnergyConfig config = SuperEnergyConfig.Instance;
            if (!config.StaminaBoostEnabled.Value) return;

            StaminaSyncConfig syncConfig = new StaminaSyncConfig(
                config.StaminaBoostPercent.Value,
                config.StaminaBoostCompensateWhenDisabled.Value,
                config.StaminaBoostEnableCrouchBoost.Value,
                config.SlideBoostPercent.Value
            );

            _syncCache.Set(SYNC_CACHE_KEY, syncConfig);
            SuperEnergy.Logger.LogInfo($"房主推送配置：体力={syncConfig.Percent}%，滑铲={syncConfig.SlideBoostPercent}%");
        }

        private void OnSyncDataChanged(string key, StaminaSyncConfig config)
        {
            SuperEnergy.Logger.LogInfo($"收到房主配置更新：体力={config.Percent}%，滑铲={config.SlideBoostPercent}%");
            _hasReceivedSyncData = true;
            _isWaitingForSync = false;
            _hasRequestedAndTimedOut = false;
        }

        // ---- 获取有效配置 ----
        public StaminaSyncConfig GetEffectiveConfig()
        {
            SuperEnergyConfig config = SuperEnergyConfig.Instance;
            bool useHost = config.SyncUseHostConfig.Value;
            bool isHost = PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom;

            if (!useHost || isHost)
            {
                return BuildFromLocalConfig(config);
            }

            // ---- 客户端启用同步 ----
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

        // ---- 客户端请求逻辑 ----
        private void RequestConfigFromHost()
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return;
            if (PhotonNetwork.IsMasterClient) return;

            RaiseEventOptions options = new RaiseEventOptions
            {
                Receivers = ReceiverGroup.MasterClient
            };
            PhotonNetwork.RaiseEvent(REQUEST_EVENT_CODE, REQUEST_CONTENT, options, SendOptions.SendReliable);
            SuperEnergy.Logger.LogInfo("已向房主发送配置请求。");
        }

        // ---- 配置变更事件回调（由 SuperEnergy 订阅） ----
        public void OnConfigSettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
        {
            string key = e.ChangedSetting.Definition.Key;

            if (key == "SyncUseHostConfig")
            {
                bool newValue = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
                SuperEnergy.Logger.LogInfo($"SyncUseHostConfig 变更为：{newValue}");

                if (newValue)
                {
                    if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && _isLevelLoaded)
                    {
                        if (PhotonNetwork.IsMasterClient)
                        {
                            EnsureSyncCacheCreated();
                            PushCurrentConfigToCache();
                        }
                        else
                        {
                            _hasReceivedSyncData = false;
                            _isWaitingForSync = false;
                            _hasRequestedAndTimedOut = false;
                            EnsureSyncCacheCreated();
                            RequestConfigFromHost();
                        }
                    }
                }
                else
                {
                    _isWaitingForSync = false;
                    _hasReceivedSyncData = false;
                    _hasRequestedAndTimedOut = false;
                }
                return;
            }

            // 其他配置变更：如果是房主且同步开启，则推送
            bool useHost = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
            if (useHost && PhotonNetwork.IsMasterClient && _isLevelLoaded && PhotonNetwork.InRoom)
            {
                EnsureSyncCacheCreated();
                PushCurrentConfigToCache();
            }
        }

        // ---- 旧签名兼容 ----
        [Obsolete("请使用 Instance.GetEffectiveConfig()")]
        public static bool TryGetEffectiveConfig(out StaminaSyncConfig? config)
        {
            config = Instance.GetEffectiveConfig();
            return true;
        }

        public void Shutdown()
        {
            // 清理由 OnDestroy 处理
        }
    }
}