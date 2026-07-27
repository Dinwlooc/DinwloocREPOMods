using System.IO;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.IBridge;
using Dinwlooc.Common.Sync;
using Photon.Pun;

namespace SuperEnergy
{
    public class StaminaConfigManager
    {
        private static StaminaConfigManager? _instance;
        public static StaminaConfigManager Instance => _instance ??= new StaminaConfigManager();

        private ISyncCache<string, RemoteStaminaConfig>? _syncConfigCache;
        private bool _isInitialized = false;

        private const string REMOTE_CONFIG_CACHE_NAME = "SuperEnergyRemoteConfig";
        private const string CONFIG_KEY = "current";

        private StaminaConfigManager() { }

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            // 如果 UseHostConfig 启用，创建同步缓存（同步器会自动处理广播和接收）
            if (SuperEnergyConfig.Instance.UseHostConfig.Value)
            {
                EnsureSyncCacheCreated();
            }

            SuperEnergy.Logger.LogInfo("体力配置管理器已初始化。");
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;
            _isInitialized = false;

            if (_syncConfigCache != null)
            {
                _syncConfigCache.OnDataChanged -= OnConfigChanged;
            }

            SuperEnergy.Logger.LogInfo("体力配置管理器已关闭。");
        }

        public void OnSettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
        {
            string key = e.ChangedSetting.Definition.Key;

            // 当 UseHostConfig 变化时，重新创建或销毁缓存
            if (key == "UseHostConfig")
            {
                bool newValue = (bool)e.ChangedSetting.BoxedValue;
                SuperEnergy.Logger.LogInfo($"UseHostConfig 变更为：{newValue}");

                if (newValue)
                {
                    // 启用时创建缓存
                    EnsureSyncCacheCreated();
                }
                else
                {
                    // 关闭时释放缓存引用（但不需要立即清空，同步器会在房间切换时清理）
                    _syncConfigCache = null;
                }
                return;
            }

            // 其他配置变化：如果是房主且 UseHostConfig 启用，缓存会自动同步（因为同步器已订阅事件）
            // 不需要手动推送，同步器会在缓存 Set 时自动广播
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

            // 如果自己是房主，推送当前配置到缓存（这会触发同步器广播）
            if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
            {
                PushCurrentConfigToCache();
            }
        }

        private void PushCurrentConfigToCache()
        {
            if (_syncConfigCache == null) return;

            SuperEnergyConfig config = SuperEnergyConfig.Instance;
            if (!config.EnableStaminaBoost.Value) return;

            RemoteStaminaConfig remoteConfig = new RemoteStaminaConfig(
                config.StaminaPercent.Value,
                config.EnableCompensationWhenDisabled.Value,
                config.EnableCrouchBoost.Value,
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
            bool useHost = SuperEnergyConfig.Instance.UseHostConfig.Value;

            if (!useHost)
            {
                config = new RemoteStaminaConfig(
                    SuperEnergyConfig.Instance.StaminaPercent.Value,
                    SuperEnergyConfig.Instance.EnableCompensationWhenDisabled.Value,
                    SuperEnergyConfig.Instance.EnableCrouchBoost.Value,
                    SuperEnergyConfig.Instance.SlideBoostPercent.Value
                );
                return true;
            }

            // 尝试从同步缓存读取
            ICacheProvider<string, RemoteStaminaConfig>? configCache =
                CacheManager.GetCache<string, RemoteStaminaConfig>(REMOTE_CONFIG_CACHE_NAME);
            if (configCache != null && configCache.TryGet(CONFIG_KEY, out RemoteStaminaConfig? remote))
            {
                config = remote;
                return true;
            }

            // 若未收到且自己是房主/单机，回退本地
            IGameStateBridge gameState = BridgeLocator.GameState;
            if (gameState.IsMasterClientOrSingleplayer())
            {
                config = new RemoteStaminaConfig(
                    SuperEnergyConfig.Instance.StaminaPercent.Value,
                    SuperEnergyConfig.Instance.EnableCompensationWhenDisabled.Value,
                    SuperEnergyConfig.Instance.EnableCrouchBoost.Value,
                    SuperEnergyConfig.Instance.SlideBoostPercent.Value
                );
                return true;
            }

            return false;
        }
    }
}