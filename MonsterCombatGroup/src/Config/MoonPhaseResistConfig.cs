using System.Collections.Generic;

namespace MonsterCombatGroup.Handler
{
    /// <summary>
    /// 定义各怪物类型在不同月相下的抵抗参数。
    /// </summary>
    public static class MoonPhaseResistConfig
    {
        public struct ResistParams
        {
            public float StrongDuration; // 强抵抗持续时间（秒）
            public float NormalDuration; // 普通抵抗持续时间（秒）
            public float Cooldown;       // 冷却时间（秒）
        }

        // 普通怪物参数
        private static readonly Dictionary<int, ResistParams> _normalParams = new Dictionary<int, ResistParams>
        {
            { 1, new ResistParams { StrongDuration = 0f, NormalDuration = 1f, Cooldown = 7f } },
            { 2, new ResistParams { StrongDuration = 1f, NormalDuration = 5f, Cooldown = 7f } },
            // 未来月相 3+ 可在此添加
        };

        // 守卫参数
        private static readonly Dictionary<int, ResistParams> _guardParams = new Dictionary<int, ResistParams>
        {
            { 1, new ResistParams { StrongDuration = 2f, NormalDuration = 3f, Cooldown = 5f } },
            { 2, new ResistParams { StrongDuration = 3f, NormalDuration = 5f, Cooldown = 5f } },
        };

        // 领队参数（月相一）
        private static readonly Dictionary<int, ResistParams> _leaderParams = new Dictionary<int, ResistParams>
        {
            { 1, new ResistParams { StrongDuration = 2f, NormalDuration = 5f, Cooldown = 7f } },
            // 月相二领队使用特殊逻辑（强制起身唤醒守卫），不在此配置
        };

        /// <summary>
        /// 获取普通怪物的抵抗参数，若月相未配置则返回默认值（无抵抗）。
        /// </summary>
        public static ResistParams GetNormalParams(int moonLevel)
        {
            if (_normalParams.TryGetValue(moonLevel, out ResistParams result))
                return result;
            return new ResistParams { StrongDuration = 0f, NormalDuration = 0f, Cooldown = 0f };
        }

        /// <summary>
        /// 获取守卫的抵抗参数，若月相未配置则返回默认值。
        /// </summary>
        public static ResistParams GetGuardParams(int moonLevel)
        {
            if (_guardParams.TryGetValue(moonLevel, out ResistParams result))
                return result;
            return new ResistParams { StrongDuration = 0f, NormalDuration = 0f, Cooldown = 0f };
        }

        /// <summary>
        /// 获取领队的抵抗参数（仅月相一），若月相未配置则返回默认值。
        /// </summary>
        public static ResistParams GetLeaderParams(int moonLevel)
        {
            if (_leaderParams.TryGetValue(moonLevel, out ResistParams result))
                return result;
            return new ResistParams { StrongDuration = 0f, NormalDuration = 0f, Cooldown = 0f };
        }
    }
}