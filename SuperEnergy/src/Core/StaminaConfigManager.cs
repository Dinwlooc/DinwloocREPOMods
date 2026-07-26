using System.IO;
using System.Collections;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Sync;
using Dinwlooc.Common.Events;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace SuperEnergy
{
    /// <summary>
    /// 体力配置同步管理器，利用 ISyncCache 实现配置同步。
    /// 房主通过 Set 广播配置，新玩家由 SyncRegionManager 自动推送全量数据。
    /// 客户端手动开启 UseHostConfig 时发送请求，房主响应推送。
    /// </summary>
    public class StaminaConfigManager : IOnEventCallback
    {
        private static StaminaConfigManager? _instance;
        public static StaminaConfigManager Instance => _instance ??= new StaminaConfigManager();

        private ISyncCache<string, RemoteStaminaConfig>? _syncConfigCache;
        private bool _isInitialized = false;

        private const string REMOTE_CONFIG_CACHE_NAME = "SuperEnergyRemoteConfig";
        private const string CONFIG_KEY = "current";
        private const byte EVENT_CODE_CONFIG_REQUEST = 201;
        private const float PUSH_DELAY = 0.5f;

        private StaminaConfigManager() { }

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            PhotonNetwork.AddCallbackTarget(this);
            EventBus.Subscribe<MasterClientSwitchedEvent>(OnMasterClientSwitched);
            EventBus.Subscribe<PlayerLevelEnteredEvent>(OnPlayerLevelEntered);

            // 初始状态
            if (SuperEnergyConfig.Instance.UseHostConfig.Value)
            {
                EnsureSyncCacheCreated();
                if (SemiFunc.IsMasterClientOrSingleplayer())
                {
                    PushCurrentConfig();
                }
                else
                {
                    // 客户端可能错过了全量推送，主动请求
                    RequestConfigFromHost();
                }
            }

            SuperEnergy.Logger.LogInfo("体力配置管理器已初始化。");
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;
            _isInitialized = false;

            if (_syncConfigCache != null)
                _syncConfigCache.OnDataChanged -= OnConfigChanged;

            PhotonNetwork.RemoveCallbackTarget(this);
            EventBus.Unsubscribe<MasterClientSwitchedEvent>(OnMasterClientSwitched);
            EventBus.Unsubscribe<PlayerLevelEnteredEvent>(OnPlayerLevelEntered);

            SuperEnergy.Logger.LogInfo("体力配置管理器已关闭。");
        }

        public void OnSettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
        {
            string key = e.ChangedSetting.Definition.Key;

            if (key == "UseHostConfig")
            {
                bool newValue = (bool)e.ChangedSetting.BoxedValue;
                SuperEnergy.Logger.LogInfo($"UseHostConfig 变更为：{newValue}");

                if (newValue)
                {
                    EnsureSyncCacheCreated();
                    if (SemiFunc.IsMasterClientOrSingleplayer())
                    {
                        PushCurrentConfig();
                    }
                    else
                    {
                        RequestConfigFromHost();
                    }
                }
                // 关闭时只切换本地读取源，不清除缓存（依赖库在房间离开时会清空）
                return;
            }

            // 其他配置变更：房主且 UseHostConfig 开启时重新推送
            if (SuperEnergyConfig.Instance.UseHostConfig.Value && SemiFunc.IsMasterClientOrSingleplayer())
            {
                PushCurrentConfig();
            }
        }

        private void RequestConfigFromHost()
        {
            if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient) return;
            SuperEnergy.Logger.LogInfo("客户端请求房主配置...");
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(EVENT_CODE_CONFIG_REQUEST, null, options, SendOptions.SendReliable);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == EVENT_CODE_CONFIG_REQUEST)
            {
                if (PhotonNetwork.IsMasterClient && SemiFunc.RunIsLevel())
                {
                    SuperEnergy.Logger.LogInfo("收到客户端配置请求，推送配置...");
                    PushCurrentConfig();
                }
            }
        }

        private void OnMasterClientSwitched(MasterClientSwitchedEvent evt)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!SuperEnergyConfig.Instance.UseHostConfig.Value) return;
            if (!SuperEnergyConfig.Instance.EnableStaminaBoost.Value) return;

            SuperEnergy.Logger.LogInfo("成为新主机，重新推送配置...");
            CommonService.Instance.RunCoroutine(DelayedPush());
        }

        private void OnPlayerLevelEntered(PlayerLevelEnteredEvent evt)
        {
            // 此事件由 SyncRegionManager 处理全量推送，我们无需重复推送
            // 但若有特殊需求，可在这里触发推送，但为了避免重复，暂时忽略
        }

        private IEnumerator DelayedPush()
        {
            yield return new WaitForSeconds(PUSH_DELAY);
            PushCurrentConfig();
        }

        private void PushCurrentConfig()
        {
            if (_syncConfigCache == null) EnsureSyncCacheCreated();
            if (_syncConfigCache == null) return;

            SuperEnergyConfig config = SuperEnergyConfig.Instance;
            if (!config.EnableStaminaBoost.Value) return;

            RemoteStaminaConfig remoteConfig = new RemoteStaminaConfig(
                config.StaminaPercent.Value,
                config.EnableCompensationWhenDisabled.Value,
                config.EnableCrouchBoost.Value
            );

            SuperEnergy.Logger.LogInfo($"房主推送配置：百分比={remoteConfig.Percent}, 补偿={remoteConfig.CompensateWhenDisabled}, 下蹲加成={remoteConfig.EnableCrouchBoost}");
            _syncConfigCache.Set(CONFIG_KEY, remoteConfig);
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
        }

        private void OnConfigChanged(string key, RemoteStaminaConfig config)
        {
            SuperEnergy.Logger.LogInfo($"客户端收到房主配置：百分比={config.Percent}, 补偿={config.CompensateWhenDisabled}, 下蹲加成={config.EnableCrouchBoost}");
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
                    SuperEnergyConfig.Instance.EnableCrouchBoost.Value
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

            // 若未收到且自己是房主/单机，回退本地
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                config = new RemoteStaminaConfig(
                    SuperEnergyConfig.Instance.StaminaPercent.Value,
                    SuperEnergyConfig.Instance.EnableCompensationWhenDisabled.Value,
                    SuperEnergyConfig.Instance.EnableCrouchBoost.Value
                );
                return true;
            }

            return false;
        }
    }
}