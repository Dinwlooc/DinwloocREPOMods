using BepInEx.Configuration;
using Dinwlooc.Common.Helpers;

namespace MonsterCombatGroup
{
    public class MonsterCombatGroupConfig : ConfigBase<MonsterCombatGroupConfig>
    {
        // ---- 领队机制总开关 ----
        public ConfigEntry<bool> EnableLeaderMechanic { get; private set; } = null!;

        // ---- 选举配置 ----
        public ConfigEntry<float> ElectionCooldownSeconds { get; private set; } = null!;

        // ---- 属性倍率（月相 1 生效） ----
        public ConfigEntry<float> LeaderHealthMultiplier { get; private set; } = null!;
        public ConfigEntry<float> GuardHealthMultiplier { get; private set; } = null!;

        // ---- 眩晕免疫 ----
        public ConfigEntry<float> StunImmunityDuration { get; private set; } = null!;

        // ---- 效果开关 ----
        public ConfigEntry<bool> EnableBatteryDrainOnLeaderHurt { get; private set; } = null!;
        public ConfigEntry<bool> EnableGuardStunRecoveryOnHurt { get; private set; } = null!;

        // ---- 指挥配置（月相 2 生效） ----
        public ConfigEntry<float> CommandInterval { get; private set; } = null!;
        public ConfigEntry<int> CommandAttackCount { get; private set; } = null!;
        public ConfigEntry<float> GlobalStunImmunityDuration { get; private set; } = null!;
        public ConfigEntry<float> LeaderExtraStunImmunityPerGuard { get; private set; } = null!;

        // ---- 领队死亡奖励（月相 1+ 生效） ----
        public ConfigEntry<bool> EnableLeaderDeathReward { get; private set; } = null!;
        public ConfigEntry<float> RewardDuration { get; private set; } = null!;

        public override void Bind(ConfigFile config)
        {
            base.Bind(config);

            EnableLeaderMechanic = config.Bind("General", "Enable", true, "启用领队机制（月相 ≥1 时自动生效）");
            ElectionCooldownSeconds = config.Bind("Election", "CooldownSeconds", 300f,
                new ConfigDescription("领队死亡后冷却时间（秒）", new AcceptableValueRange<float>(0f, 3600f)));
            LeaderHealthMultiplier = config.Bind("Stats", "LeaderHealthMultiplier", 10f,
                new ConfigDescription("月相 1 时领队生命值倍率", new AcceptableValueRange<float>(1f, 100f)));
            GuardHealthMultiplier = config.Bind("Stats", "GuardHealthMultiplier", 5f,
                new ConfigDescription("月相 1 时守卫生命值倍率", new AcceptableValueRange<float>(1f, 50f)));
            StunImmunityDuration = config.Bind("Immunity", "DurationSeconds", 1f,
                new ConfigDescription("基础眩晕免疫时长（秒）", new AcceptableValueRange<float>(0f, 10f)));
            EnableBatteryDrainOnLeaderHurt = config.Bind("Effects", "EnableBatteryDrain", true, "领队受伤减少玩家电量（月相 1/2 生效）");
            EnableGuardStunRecoveryOnHurt = config.Bind("Effects", "EnableGuardStunRecovery", true, "守卫受伤解除眩晕");

            CommandInterval = config.Bind("Combat", "CommandInterval", 5f,
                new ConfigDescription("指挥状态下召集攻击间隔（秒）（月相 2 生效）", new AcceptableValueRange<float>(1f, 30f)));
            CommandAttackCount = config.Bind("Combat", "CommandAttackCount", 6,
                new ConfigDescription("每次锁定时指挥攻击次数（月相 2 生效）", new AcceptableValueRange<int>(1, 20)));
            GlobalStunImmunityDuration = config.Bind("Immunity", "GlobalStunImmunity", 1f,
                new ConfigDescription("指挥状态下全体怪物受击获得的眩晕免疫时长（秒）（月相 2 生效）", new AcceptableValueRange<float>(0f, 10f)));
            LeaderExtraStunImmunityPerGuard = config.Bind("Immunity", "LeaderExtraPerGuard", 4f,
                new ConfigDescription("每名护卫为领队额外提供的眩晕免疫时长（秒）（月相 2 生效）", new AcceptableValueRange<float>(0f, 20f)));

            EnableLeaderDeathReward = config.Bind("Reward", "Enable", true, "启用领队死亡奖励（Valuable 无敌保护）（月相 1+ 生效）");
            RewardDuration = config.Bind("Reward", "Duration", 300f,
                new ConfigDescription("奖励持续时间（秒）", new AcceptableValueRange<float>(0f, 3600f)));
        }
    }
}