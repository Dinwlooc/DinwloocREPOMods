using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.Sync;
using UnityEngine;
using System.IO;

namespace SuperEnergy
{
    [BepInPlugin("Dinwlooc.SuperEnergy", "SuperEnergy", "1.0.0")]
    [BepInDependency("Dinwlooc.Common", BepInDependency.DependencyFlags.HardDependency)]
    public class SuperEnergy : BaseUnityPlugin
    {
        internal static SuperEnergy Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;

        private EnergyService _service = null!;
        private ISyncCache<string, RemoteStaminaConfig>? _syncConfigCache;

        private const string REMOTE_CONFIG_CACHE_NAME = "SuperEnergyRemoteConfig";
        private const string CONFIG_KEY = "current";

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            SuperEnergyConfig.Instance.Initialize(Config);
            _service = gameObject.AddComponent<EnergyService>();

            if (SuperEnergyConfig.Instance.UseHostConfig.Value)
            {
                _syncConfigCache = CacheManager.GetOrCreateSyncCache<string, RemoteStaminaConfig>(
                    REMOTE_CONFIG_CACHE_NAME,
                    SyncMode.HostAuthority,
                    serialize: (BinaryWriter writer, RemoteStaminaConfig config) => config.Write(writer),
                    deserialize: (BinaryReader reader) => RemoteStaminaConfig.Read(reader)
                );

                _syncConfigCache.OnDataChanged += OnConfigChanged;

                if (SemiFunc.IsMasterClientOrSingleplayer())
                {
                    PushCurrentConfig();
                }
            }

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }

        private void OnDestroy()
        {
            if (_syncConfigCache != null)
            {
                _syncConfigCache.OnDataChanged -= OnConfigChanged;
            }
        }

        private void OnConfigChanged(string key, RemoteStaminaConfig config)
        {
            // 业务日志可选，暂不实现
        }

        private void PushCurrentConfig()
        {
            if (_syncConfigCache == null)
            {
                return;
            }

            SuperEnergyConfig config = SuperEnergyConfig.Instance;
            if (!config.EnableStaminaBoost.Value)
            {
                return;
            }

            RemoteStaminaConfig remoteConfig = new RemoteStaminaConfig(
                config.StaminaPercent.Value,
                config.EnableCompensationWhenDisabled.Value,
                config.EnableCrouchBoost.Value
            );

            _syncConfigCache.Set(CONFIG_KEY, remoteConfig);
            Logger.LogInfo($"配置已推送：百分比={remoteConfig.Percent}, 补偿={remoteConfig.CompensateWhenDisabled}, 下蹲加成={remoteConfig.EnableCrouchBoost}");
        }

        internal static bool TryGetEffectiveConfig(out RemoteStaminaConfig? config)
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

            ICacheProvider<string, RemoteStaminaConfig>? cache = CacheManager.GetCache<string, RemoteStaminaConfig>(REMOTE_CONFIG_CACHE_NAME);
            if (cache != null && cache.TryGet(CONFIG_KEY, out RemoteStaminaConfig? remote))
            {
                config = remote;
                return true;
            }

            return false;
        }

        public class RemoteStaminaConfig
        {
            public int Percent { get; }
            public bool CompensateWhenDisabled { get; }
            public bool EnableCrouchBoost { get; }

            public RemoteStaminaConfig(int percent, bool comp, bool crouch)
            {
                Percent = percent;
                CompensateWhenDisabled = comp;
                EnableCrouchBoost = crouch;
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(Percent);
                writer.Write(CompensateWhenDisabled);
                writer.Write(EnableCrouchBoost);
            }

            public static RemoteStaminaConfig Read(BinaryReader reader)
            {
                int percent = reader.ReadInt32();
                bool comp = reader.ReadBoolean();
                bool crouch = reader.ReadBoolean();
                return new RemoteStaminaConfig(percent, comp, crouch);
            }
        }
    }
}