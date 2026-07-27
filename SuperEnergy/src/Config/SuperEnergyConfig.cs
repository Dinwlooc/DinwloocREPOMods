using BepInEx.Configuration;
using Dinwlooc.Common.Helpers;

namespace SuperEnergy
{
    public class SuperEnergyConfig : ConfigBase<SuperEnergyConfig>
    {
        // ---- 物品充电 ----
        public ConfigEntry<bool> EnableItemCharging { get; private set; } = null!;
        public ConfigEntry<ChargingSource> ChargingSourceSetting { get; private set; } = null!;
        public ConfigEntry<int> ChargeInterval { get; private set; } = null!;
        public ConfigEntry<int> ChargeAmount { get; private set; } = null!;

        // ---- 玩家自愈 ----
        public ConfigEntry<bool> EnablePlayerHeal { get; private set; } = null!;
        public ConfigEntry<HealSource> HealSourceSetting { get; private set; } = null!;
        public ConfigEntry<int> HealInterval { get; private set; } = null!;
        public ConfigEntry<int> HealAmount { get; private set; } = null!;

        // ---- 死亡头部复活 ----
        public ConfigEntry<bool> EnableDeathHeadRevive { get; private set; } = null!;
        public ConfigEntry<int> DeathHeadReviveTime { get; private set; } = null!;

        // ---- 体力加速 ----
        public ConfigEntry<bool> EnableStaminaBoost { get; private set; } = null!;
        public ConfigEntry<int> StaminaPercent { get; private set; } = null!;
        public ConfigEntry<bool> EnableCompensationWhenDisabled { get; private set; } = null!;
        public ConfigEntry<bool> EnableCrouchBoost { get; private set; } = null!;
        public ConfigEntry<bool> UseHostConfig { get; private set; } = null!;

        // ---- 滑铲加速（新增） ----
        public ConfigEntry<bool> EnableSlideBoost { get; private set; } = null!;
        public ConfigEntry<int> SlideBoostPercent { get; private set; } = null!;

        public override void Bind(ConfigFile config)
        {
            base.Bind(config);

            EnableItemCharging = config.Bind("ItemCharging", "Enable", true, "启用手持物品自动充电");
            ChargingSourceSetting = config.Bind("ItemCharging", "Source", ChargingSource.Free,
                new ConfigDescription("充电来源：Free 或 Truck"));
            ChargeInterval = config.Bind("ItemCharging", "Interval", 2,
                new ConfigDescription("充电间隔（秒）", new AcceptableValueRange<int>(1, 60)));
            ChargeAmount = config.Bind("ItemCharging", "Amount", 5,
                new ConfigDescription("每次充电量（百分比）", new AcceptableValueRange<int>(1, 100)));

            EnablePlayerHeal = config.Bind("PlayerHeal", "Enable", true, "启用玩家自愈");
            HealSourceSetting = config.Bind("PlayerHeal", "Source", HealSource.Free,
                new ConfigDescription("自愈来源：Free 或 HealthPack"));
            HealInterval = config.Bind("PlayerHeal", "Interval", 2,
                new ConfigDescription("自愈间隔（秒）", new AcceptableValueRange<int>(1, 60)));
            HealAmount = config.Bind("PlayerHeal", "Amount", 5,
                new ConfigDescription("每次恢复生命值（HP）", new AcceptableValueRange<int>(1, 100)));

            EnableDeathHeadRevive = config.Bind("DeathHeadRevive", "Enable", true, "允许死亡头部累积时间后复活");
            DeathHeadReviveTime = config.Bind("DeathHeadRevive", "RequiredTime", 30,
                new ConfigDescription("复活所需时间（秒），设为0则立刻复活", new AcceptableValueRange<int>(0, 300)));

            EnableStaminaBoost = config.Bind("StaminaBoost", "Enable", true, "启用体力加速恢复");
            StaminaPercent = config.Bind("StaminaBoost", "Percent", 100,
                new ConfigDescription("额外恢复百分比（0~500）", new AcceptableValueRange<int>(0, 500)));
            EnableCompensationWhenDisabled = config.Bind("StaminaBoost", "CompensateWhenDisabled", false,
                "原版禁用时是否强制恢复并应用加成");
            EnableCrouchBoost = config.Bind("StaminaBoost", "EnableCrouchBoost", true,
                "是否对下蹲恢复应用百分比加成");
            UseHostConfig = config.Bind("StaminaBoost", "UseHostConfig", false,
                "是否使用房主的体力配置（开启后忽略本地配置，等待房主广播）");

            // 滑铲加速配置
            EnableSlideBoost = config.Bind("SlideBoost", "Enable", true, "启用滑铲效能提升");
            SlideBoostPercent = config.Bind("SlideBoost", "Percent", 0,
                new ConfigDescription("滑铲持续时间额外增加百分比（0~500）", new AcceptableValueRange<int>(0, 500)));
        }
    }

    public enum ChargingSource { Free, Truck }
    public enum HealSource { Free, HealthPack }
}