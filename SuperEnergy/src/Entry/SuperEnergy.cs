using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Core;
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
        private StaminaConfigManager _configManager = null!;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            // 创建/更新翻译文件
            LocalizationManager.Load();

            // 绑定配置（键名已更新）
            SuperEnergyConfig.Instance.Initialize(Config);

            _service = gameObject.AddComponent<EnergyService>();
            Config.SettingChanged += OnConfigSettingChanged;
            _configManager = StaminaConfigManager.Instance;
            _configManager.Initialize();

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }

        private void OnConfigSettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
        {
            _configManager?.OnSettingChanged(sender, e);
        }

        private void OnDestroy()
        {
            Config.SettingChanged -= OnConfigSettingChanged;
            _configManager?.Shutdown();
        }
    }
}