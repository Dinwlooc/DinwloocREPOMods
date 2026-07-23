using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace SuperEnergy
{
    [BepInPlugin("Dinwlooc.SuperEnergy", "SuperEnergy", "1.0.0")]
    public class SuperEnergy : BaseUnityPlugin
    {
        internal static SuperEnergy Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;

        public static ConfigEntry<bool>? EnableItemCharging { get; private set; }
        public static ConfigEntry<ChargingSource>? ChargingSourceSetting { get; private set; }
        public static ConfigEntry<int>? ChargeInterval { get; private set; }
        public static ConfigEntry<int>? ChargeAmount { get; private set; }

        public static ConfigEntry<bool>? EnablePlayerHeal { get; private set; }
        public static ConfigEntry<HealSource>? HealSourceSetting { get; private set; }
        public static ConfigEntry<int>? HealInterval { get; private set; }
        public static ConfigEntry<int>? HealAmount { get; private set; }

        public static ConfigEntry<bool>? EnableDeathHeadRevive { get; private set; }
        public static ConfigEntry<int>? DeathHeadReviveTime { get; private set; }

        public static ConfigEntry<bool>? EnableStaminaBoost { get; private set; }
        public static ConfigEntry<int>? StaminaMultiplier { get; private set; }

        private EnergyService _service = null!;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            gameObject.transform.parent = null;
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            // ---- 物品充电 ----
            EnableItemCharging = Config.Bind("ItemCharging", "Enable", true, "启用手持物品自动充电");
            ChargingSourceSetting = Config.Bind("ItemCharging", "Source", ChargingSource.Free,
                new ConfigDescription("充电来源：Free 或 Truck"));
            ChargeInterval = Config.Bind("ItemCharging", "Interval", 2,
                new ConfigDescription("充电间隔（秒）", new AcceptableValueRange<int>(1, 60)));
            ChargeAmount = Config.Bind("ItemCharging", "Amount", 5,
                new ConfigDescription("每次充电量（百分比）", new AcceptableValueRange<int>(1, 100)));

            // ---- 玩家自愈 ----
            EnablePlayerHeal = Config.Bind("PlayerHeal", "Enable", true, "启用玩家自愈");
            HealSourceSetting = Config.Bind("PlayerHeal", "Source", HealSource.Free,
                new ConfigDescription("自愈来源：Free 或 HealthPack"));
            HealInterval = Config.Bind("PlayerHeal", "Interval", 2,
                new ConfigDescription("自愈间隔（秒）", new AcceptableValueRange<int>(1, 60)));
            HealAmount = Config.Bind("PlayerHeal", "Amount", 5,
                new ConfigDescription("每次恢复生命值（HP）", new AcceptableValueRange<int>(1, 100)));

            // ---- 死亡头部复活 ----
            EnableDeathHeadRevive = Config.Bind("DeathHeadRevive", "Enable", true, "允许死亡头部累积时间后复活");
            // 在 Awake 中，修改 DeathHeadReviveTime 配置
            DeathHeadReviveTime = Config.Bind("DeathHeadRevive", "RequiredTime", 30,
                new ConfigDescription("复活所需时间（秒），设为0则立刻复活", new AcceptableValueRange<int>(0, 300)));

            // ---- 体力加速 ----
            EnableStaminaBoost = Config.Bind("StaminaBoost", "Enable", true, "启用体力加速恢复");
            StaminaMultiplier = Config.Bind("StaminaBoost", "Multiplier", 2,
                new ConfigDescription("恢复倍率", new AcceptableValueRange<int>(1, 10)));

            _service = gameObject.AddComponent<EnergyService>();
            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }
    }

    public enum ChargingSource { Free, Truck }
    public enum HealSource { Free, HealthPack }
}