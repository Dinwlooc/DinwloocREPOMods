using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Core;
using UnityEngine;
using System.Collections.Generic;

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

            RegisterTranslations();

            SuperEnergyConfig.Instance.Initialize(Config);

            _service = gameObject.AddComponent<EnergyService>();
            Config.SettingChanged += OnConfigSettingChanged;
            _configManager = StaminaConfigManager.Instance; // 自动初始化

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }

        private void RegisterTranslations()
        {
            var translations = new Dictionary<string, string>
            {
                ["Item Charging Enabled"] = "启用",
                ["Item Charging Source"] = "充电来源",
                ["Item Charging Interval"] = "充电间隔(秒)",
                ["Item Charging Amount"] = "每次充电量(%)",
                ["Player Heal Enabled"] = "启用",
                ["Player Heal Source"] = "自愈来源",
                ["Player Heal Interval"] = "自愈间隔(秒)",
                ["Player Heal Amount"] = "每次恢复量(HP)",
                ["Death Head Revive Enabled"] = "启用",
                ["Death Head Revive Required Time"] = "复活所需时间(秒)",
                ["Stamina Boost Enabled"] = "启用",
                ["Stamina Boost Percent"] = "额外恢复百分比",
                ["Stamina Boost Compensate When Disabled"] = "原版禁用时补偿",
                ["Stamina Boost Enable Crouch Boost"] = "下蹲加成",
                ["Slide Boost Enabled"] = "启用",
                ["Slide Boost Percent"] = "滑铲额外百分比",
                ["Sync Use Host Config"] = "使用房主配置"
            };

            TranslationManager.RegisterTranslations(
                Info.Metadata.GUID,
                "zh",
                2,
                translations
            );
        }

        private void OnConfigSettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
        {
            _configManager?.OnConfigSettingChanged(sender, e);
        }

        private void OnDestroy()
        {
            Config.SettingChanged -= OnConfigSettingChanged;
        }
    }
}