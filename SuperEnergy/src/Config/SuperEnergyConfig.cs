using BepInEx.Configuration;
using Dinwlooc.Common.Helpers;

namespace SuperEnergy
{
    public class SuperEnergyConfig : ConfigBase<SuperEnergyConfig>
    {
        // ---- 物品充电 ----
        public ConfigEntry<bool> EnableItemCharging { get; private set; } = null!;
        public ConfigEntry<ChargingSource> ChargingSourceSetting { get; private set; } = null!; // 重命名
        public ConfigEntry<int> ChargeInterval { get; private set; } = null!;
        public ConfigEntry<int> ChargeAmount { get; private set; } = null!;

        // ---- 玩家自愈 ----
        public ConfigEntry<bool> EnablePlayerHeal { get; private set; } = null!;
        public ConfigEntry<HealSource> HealSourceSetting { get; private set; } = null!; // 重命名
        public ConfigEntry<int> HealInterval { get; private set; } = null!;
        public ConfigEntry<int> HealAmount { get; private set; } = null!;

        // ---- 死亡头部复活 ----
        public ConfigEntry<bool> EnableDeathHeadRevive { get; private set; } = null!;
        public ConfigEntry<int> DeathHeadReviveTime { get; private set; } = null!;

        // ---- 体力加速 ----
        public ConfigEntry<bool> EnableStaminaBoost { get; private set; } = null!;
        public ConfigEntry<int> StaminaMultiplier { get; private set; } = null!;

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
            StaminaMultiplier = config.Bind("StaminaBoost", "Multiplier", 2,
                new ConfigDescription("恢复倍率", new AcceptableValueRange<int>(1, 10)));
        }
    }

    public enum ChargingSource { Free, Truck }
    public enum HealSource { Free, HealthPack }
}