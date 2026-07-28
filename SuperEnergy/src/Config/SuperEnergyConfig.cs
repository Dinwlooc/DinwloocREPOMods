using BepInEx.Configuration;
using Dinwlooc.Common.Helpers;

namespace SuperEnergy
{
    public class SuperEnergyConfig : ConfigBase<SuperEnergyConfig>
    {
        public ConfigEntry<bool> ItemChargingEnabled { get; private set; } = null!;
        public ConfigEntry<ChargingSource> ItemChargingSource { get; private set; } = null!;
        public ConfigEntry<int> ItemChargingInterval { get; private set; } = null!;
        public ConfigEntry<int> ItemChargingAmount { get; private set; } = null!;

        public ConfigEntry<bool> PlayerHealEnabled { get; private set; } = null!;
        public ConfigEntry<HealSource> PlayerHealSource { get; private set; } = null!;
        public ConfigEntry<int> PlayerHealInterval { get; private set; } = null!;
        public ConfigEntry<int> PlayerHealAmount { get; private set; } = null!;

        public ConfigEntry<bool> DeathHeadReviveEnabled { get; private set; } = null!;
        public ConfigEntry<int> DeathHeadReviveRequiredTime { get; private set; } = null!;

        public ConfigEntry<bool> StaminaBoostEnabled { get; private set; } = null!;
        public ConfigEntry<int> StaminaBoostPercent { get; private set; } = null!;
        public ConfigEntry<bool> StaminaBoostCompensateWhenDisabled { get; private set; } = null!;
        public ConfigEntry<bool> StaminaBoostEnableCrouchBoost { get; private set; } = null!;

        public ConfigEntry<bool> SlideBoostEnabled { get; private set; } = null!;
        public ConfigEntry<int> SlideBoostPercent { get; private set; } = null!;

        public ConfigEntry<bool> SyncUseHostConfig { get; private set; } = null!;

        public override void Bind(ConfigFile config)
        {
            base.Bind(config);

            ItemChargingEnabled = config.Bind("ItemCharging", "ItemChargingEnabled", true,
                new ConfigDescription("启用手持物品自动充电"));
            ItemChargingSource = config.Bind("ItemCharging", "ItemChargingSource", ChargingSource.Free,
                new ConfigDescription("充电来源：Free 或 Truck"));
            ItemChargingInterval = config.Bind("ItemCharging", "ItemChargingInterval", 2,
                new ConfigDescription("充电间隔（秒）", new AcceptableValueRange<int>(1, 60)));
            ItemChargingAmount = config.Bind("ItemCharging", "ItemChargingAmount", 5,
                new ConfigDescription("每次充电量（百分比）", new AcceptableValueRange<int>(1, 100)));

            PlayerHealEnabled = config.Bind("PlayerHeal", "PlayerHealEnabled", true,
                new ConfigDescription("启用玩家自愈"));
            PlayerHealSource = config.Bind("PlayerHeal", "PlayerHealSource", HealSource.Free,
                new ConfigDescription("自愈来源：Free 或 HealthPack"));
            PlayerHealInterval = config.Bind("PlayerHeal", "PlayerHealInterval", 2,
                new ConfigDescription("自愈间隔（秒）", new AcceptableValueRange<int>(1, 60)));
            PlayerHealAmount = config.Bind("PlayerHeal", "PlayerHealAmount", 5,
                new ConfigDescription("每次恢复生命值（HP）", new AcceptableValueRange<int>(1, 100)));

            DeathHeadReviveEnabled = config.Bind("DeathHeadRevive", "DeathHeadReviveEnabled", true,
                new ConfigDescription("允许死亡头部累积时间后复活"));
            DeathHeadReviveRequiredTime = config.Bind("DeathHeadRevive", "DeathHeadReviveRequiredTime", 30,
                new ConfigDescription("复活所需时间（秒），设为0则立刻复活", new AcceptableValueRange<int>(0, 300)));

            StaminaBoostEnabled = config.Bind("StaminaBoost", "StaminaBoostEnabled", true,
                new ConfigDescription("启用体力加速恢复"));
            StaminaBoostPercent = config.Bind("StaminaBoost", "StaminaBoostPercent", 100,
                new ConfigDescription("额外恢复百分比（0~500）", new AcceptableValueRange<int>(0, 500)));
            StaminaBoostCompensateWhenDisabled = config.Bind("StaminaBoost", "StaminaBoostCompensateWhenDisabled", false,
                new ConfigDescription("原版禁用时是否强制恢复并应用加成"));
            StaminaBoostEnableCrouchBoost = config.Bind("StaminaBoost", "StaminaBoostEnableCrouchBoost", true,
                new ConfigDescription("是否对下蹲恢复应用百分比加成"));

            SlideBoostEnabled = config.Bind("SlideBoost", "SlideBoostEnabled", true,
                new ConfigDescription("启用滑铲效能提升"));
            SlideBoostPercent = config.Bind("SlideBoost", "SlideBoostPercent", 0,
                new ConfigDescription("滑铲持续时间额外增加百分比（0~500）", new AcceptableValueRange<int>(0, 500)));

            SyncUseHostConfig = config.Bind("Sync", "SyncUseHostConfig", false,
                new ConfigDescription("是否使用房主的体力配置（开启后忽略本地配置，等待房主广播）"));
        }
    }

    public enum ChargingSource { Free, Truck }
    public enum HealSource { Free, HealthPack }
}