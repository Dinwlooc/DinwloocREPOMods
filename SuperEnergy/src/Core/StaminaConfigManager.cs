using System.IO;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using Dinwlooc.Common.Sync;
using Photon.Pun;
using UnityEngine.SceneManagement;

namespace SuperEnergy
{
    public class StaminaConfigManager
    {
        private static StaminaConfigManager? _instance;
        public static StaminaConfigManager Instance => _instance ??= new StaminaConfigManager();

        private ISyncCache<string, RemoteStaminaConfig>? _syncConfigCache;
        private bool _isInitialized = false;
        private bool _isSubscribed = false;
        private bool _isLevelLoaded = false;
        private bool _pendingPush = false;

        private const string REMOTE_CONFIG_CACHE_NAME = "SuperEnergyRemoteConfig";
        private const string CONFIG_KEY = "current";

        private StaminaConfigManager() { }

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            SceneManager.sceneLoaded += OnSceneLoaded;
            EventBus.Subscribe<NetworkReadyEvent>(OnNetworkReady);
            EventBus.Subscribe<LeftRoomEvent>(OnLeftRoom);

            SuperEnergy.Logger.LogInfo("体力配置管理器已初始化（等待关卡加载）。");
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;
            _isInitialized = false;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            EventBus.Unsubscribe<NetworkReadyEvent>(OnNetworkReady);
            EventBus.Unsubscribe<LeftRoomEvent>(OnLeftRoom);

            if (_syncConfigCache != null)
            {
                _syncConfigCache.OnDataChanged -= OnConfigChanged;
            }

            UnsubscribeEvents();

            SuperEnergy.Logger.LogInfo("体力配置管理器已关闭。");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (SemiFunc.RunIsLevel())
            {
                _isLevelLoaded = true;
                SuperEnergy.Logger.LogInfo($"检测到关卡场景加载：{scene.name}");

                if (SuperEnergyConfig.Instance.SyncUseHostConfig.Value)
                {
                    if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
                    {
                        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
                        {
                            EnsureSyncCacheCreated();
                            PushCurrentConfigToCache();
                            _pendingPush = false;
                            SuperEnergy.Logger.LogInfo("网络已就绪，配置已推送。");
                        }
                        else
                        {
                            _pendingPush = true;
                            SuperEnergy.Logger.LogInfo("网络未就绪，将在网络就绪后补发配置（若关卡仍加载）。");
                        }
                    }
                }
            }
            else
            {
                _isLevelLoaded = false;
                _pendingPush = false;
                SuperEnergy.Logger.LogInfo("离开关卡场景。");
            }
        }

        private void OnNetworkReady(NetworkReadyEvent evt)
        {
            if (_pendingPush && _isLevelLoaded && (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom))
            {
                EnsureSyncCacheCreated();
                PushCurrentConfigToCache();
                _pendingPush = false;
                SuperEnergy.Logger.LogInfo("网络就绪后补发配置（关卡已加载）。");
            }
            else
            {
                SuperEnergy.Logger.LogInfo($"网络就绪，但无需补发 (_pendingPush={_pendingPush}, _isLevelLoaded={_isLevelLoaded})");
            }
        }

        private void OnLeftRoom(LeftRoomEvent evt)
        {
            if (_syncConfigCache != null)
            {
                _syncConfigCache.Clear();
                SuperEnergy.Logger.LogInfo("离开房间，清空同步缓存。");
            }
            _pendingPush = false;
            _isLevelLoaded = false;
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed) return;
            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed) return;
            _isSubscribed = false;
        }

        public void OnSettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
        {
            string key = e.ChangedSetting.Definition.Key;

            if (key == "UseHostConfig")
            {
                bool useHost = SuperEnergyConfig.Instance.SyncUseHostConfig.Value;
                SuperEnergy.Logger.LogInfo($"UseHostConfig 变更为：{useHost}");

                if (useHost)
                {
                    if (_isLevelLoaded)
                    {
                        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
                        {
                            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
                            {
                                EnsureSyncCacheCreated();
                                PushCurrentConfigToCache();
                                _pendingPush = false;
                            }
                            else
                            {
                                _pendingPush = true;
                                SuperEnergy.Logger.LogInfo("网络未就绪，配置将在网络就绪后补发。");
                            }
                        }
                    }
                    else
                    {
                        SuperEnergy.Logger.LogInfo("尚未进入关卡，缓存将在关卡加载后创建。");
                    }
                }
                else
                {
                    _syncConfigCache = null;
                    _pendingPush = false;
                }
                return;
            }

            if (SuperEnergyConfig.Instance.SyncUseHostConfig.Value &&
                _isLevelLoaded &&
                (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) &&
                PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                PushCurrentConfigToCache();
            }
        }

        private void EnsureSyncCacheCreated()
        {
            if (_syncConfigCache != null) return;

            _syncConfigCache = CacheManager.GetOrCreateSyncCache<string, RemoteStaminaConfig>(
                REMOTE_CONFIG_CACHE_NAME,
                SyncMode.HostAuthority,
                serialize: (BinaryWriter writer, RemoteStaminaConfig config) => config.Write(writer),
                deserialize: (BinaryReader reader) => RemoteStaminaConfig.Read(reader)
            );
            _syncConfigCache.OnDataChanged += OnConfigChanged;

            SuperEnergy.Logger.LogInfo("同步缓存已创建。");
        }

        private void PushCurrentConfigToCache()
        {
            if (_syncConfigCache == null) return;

            SuperEnergyConfig config = SuperEnergyConfig.Instance;
            if (!config.StaminaBoostEnabled.Value) return;

            RemoteStaminaConfig remoteConfig = new RemoteStaminaConfig(
                config.StaminaBoostPercent.Value,
                config.StaminaBoostCompensateWhenDisabled.Value,
                config.StaminaBoostEnableCrouchBoost.Value,
                config.SlideBoostPercent.Value
            );

            SuperEnergy.Logger.LogInfo($"房主推送配置到缓存：体力百分比={remoteConfig.Percent}, 补偿={remoteConfig.CompensateWhenDisabled}, 下蹲加成={remoteConfig.EnableCrouchBoost}, 滑铲倍率={remoteConfig.SlideBoostPercent}");
            _syncConfigCache.Set(CONFIG_KEY, remoteConfig);
        }

        private void OnConfigChanged(string key, RemoteStaminaConfig config)
        {
            SuperEnergy.Logger.LogInfo($"收到房主配置更新：体力百分比={config.Percent}, 补偿={config.CompensateWhenDisabled}, 下蹲加成={config.EnableCrouchBoost}, 滑铲倍率={config.SlideBoostPercent}");
        }

        public static bool TryGetEffectiveConfig(out RemoteStaminaConfig? config)
        {
            config = null;
            SuperEnergyConfig cfg = SuperEnergyConfig.Instance;
            bool useHost = cfg.SyncUseHostConfig.Value;

            if (!useHost)
            {
                config = new RemoteStaminaConfig(
                    cfg.StaminaBoostPercent.Value,
                    cfg.StaminaBoostCompensateWhenDisabled.Value,
                    cfg.StaminaBoostEnableCrouchBoost.Value,
                    cfg.SlideBoostPercent.Value
                );
                return true;
            }

            ICacheProvider<string, RemoteStaminaConfig>? configCache =
                CacheManager.GetCache<string, RemoteStaminaConfig>(REMOTE_CONFIG_CACHE_NAME);
            if (configCache != null && configCache.TryGet(CONFIG_KEY, out RemoteStaminaConfig? remote))
            {
                config = remote;
                return true;
            }

            IGameStateBridge gameState = BridgeLocator.GameState;
            if (gameState.IsMasterClientOrSingleplayer())
            {
                config = new RemoteStaminaConfig(
                    cfg.StaminaBoostPercent.Value,
                    cfg.StaminaBoostCompensateWhenDisabled.Value,
                    cfg.StaminaBoostEnableCrouchBoost.Value,
                    cfg.SlideBoostPercent.Value
                );
                return true;
            }

            return false;
        }
    }
}