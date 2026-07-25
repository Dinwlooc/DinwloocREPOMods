// SuperEnergy.cs
using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.Sync;
using UnityEngine;

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
                    SyncMode.HostAuthority
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
                _syncConfigCache.OnDataChanged -= OnConfigChanged;
        }

        private void OnConfigChanged(string key, RemoteStaminaConfig config) { }

        private void PushCurrentConfig()
        {
            if (_syncConfigCache == null) return;

            var config = SuperEnergyConfig.Instance;
            if (!config.EnableStaminaBoost.Value) return;

            var remoteConfig = new RemoteStaminaConfig(
                config.StaminaPercent.Value,
                config.EnableCompensationWhenDisabled.Value,
                config.EnableCrouchBoost.Value
            );

            _syncConfigCache.Set(CONFIG_KEY, remoteConfig);
        }

        [System.Serializable]
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
        }

        internal static bool TryGetEffectiveConfig(out RemoteStaminaConfig? config)
        {
            config = null;
            var useHost = SuperEnergyConfig.Instance.UseHostConfig.Value;

            if (!useHost)
            {
                config = new RemoteStaminaConfig(
                    SuperEnergyConfig.Instance.StaminaPercent.Value,
                    SuperEnergyConfig.Instance.EnableCompensationWhenDisabled.Value,
                    SuperEnergyConfig.Instance.EnableCrouchBoost.Value
                );
                return true;
            }

            var cache = CacheManager.GetCache<string, RemoteStaminaConfig>(REMOTE_CONFIG_CACHE_NAME);
            if (cache != null && cache.TryGet(CONFIG_KEY, out var remote))
            {
                config = remote;
                return true;
            }
            return false;
        }
    }
}