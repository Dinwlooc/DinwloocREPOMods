using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace MonsterCombatGroup
{
    /// <summary>
    /// 月相信息文本管理，按行独立注册为 MoonAttribute。
    /// 仅在月相等级变化时更新，同一等级仅注入一次。
    /// </summary>
    public static class MoonPhaseManager
    {
        // 当前注入的属性引用列表（用于清理）
        private static readonly List<Moon.MoonAttribute> _injectedAttributes = new List<Moon.MoonAttribute>();

        // 上次更新时的月相等级（0 表示未初始化）
        private static int _lastMoonLevel = 0;

        /// <summary>
        /// 根据当前月相更新属性文本。若月相等级未变，则不做任何操作。
        /// 仅在房主端调用有效。
        /// </summary>
        public static void UpdateForCurrentMoon()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            IMoonBridge moonBridge = BridgeLocator.Moon;
            int currentLevel = moonBridge.GetCurrentMoonLevel();

            // 如果月相等级未变，直接返回（不重复注入）
            if (currentLevel == _lastMoonLevel)
                return;

            // 月相等级变化 → 先移除旧属性（若有）
            RemoveAllInjectedAttributes();

            // 更新记录
            _lastMoonLevel = currentLevel;

            // 若月相 < 1，不注入任何内容
            if (currentLevel < 1)
                return;

            // 获取当前月相的多行描述
            IReadOnlyList<string> descriptions = GetMoonDescriptions(currentLevel);

            // 逐行注入为新属性
            foreach (string text in descriptions)
            {
                if (string.IsNullOrEmpty(text))
                    continue;

                Moon.MoonAttribute attr = moonBridge.InjectAttributeToCurrentMoon(text);
                if (attr != null)
                {
                    _injectedAttributes.Add(attr);
                    MonsterCombatGroup.Logger.LogInfo($"注入月相 {currentLevel} 属性：{text}");
                }
            }
        }

        /// <summary>
        /// 移除所有已注入的属性（仅内部使用，由 UpdateForCurrentMoon 在等级变化时调用）。
        /// </summary>
        private static void RemoveAllInjectedAttributes()
        {
            if (_injectedAttributes.Count == 0)
                return;

            IMoonBridge moonBridge = BridgeLocator.Moon;
            foreach (Moon.MoonAttribute attr in _injectedAttributes)
            {
                if (attr != null)
                {
                    bool removed = moonBridge.RemoveAttributeFromCurrentMoon(attr);
                    if (removed)
                        MonsterCombatGroup.Logger.LogInfo($"移除月相属性：{attr.text}");
                    else
                        MonsterCombatGroup.Logger.LogWarning($"移除月相属性失败：{attr.text}");
                }
            }
            _injectedAttributes.Clear();
        }

        /// <summary>
        /// 根据月相等级生成多行描述（纯函数，可复用）。
        /// 每行将作为独立的 MoonAttribute 条目居中显示。
        /// </summary>
        public static IReadOnlyList<string> GetMoonDescriptions(int level)
        {
            switch (level)
            {
                case 1:
                    return new List<string>
                    {
                        "怪物开始选举领队与守卫，这些单位拥有更高的血量。",
                        "领队存在时，所有怪物都将对眩晕有一定的抵抗力。",
                        "领队在受伤时将消耗玩家手持物品的电量。"
                    };
                case 2:
                    return new List<string>
                    {
                        "领队已掌握集火战术，引导其他怪物追击目标，并消耗目标手持物品的电量。",
                        "领队获得完全的眩晕抵抗力。",
                        "领队为所有怪物赋予更强的受击眩晕抵抗。"
                    };
                default:
                    return new List<string> { $"月相 {level} 特殊能力：待补充" };
            }
        }

        /// <summary>
        /// 重置管理器（用于模组完全卸载时清理，但按需求不主动调用）。
        /// 此方法保留以作备用，但不会在正常流程中被调用。
        /// </summary>
        public static void Reset()
        {
            // 按用户要求，此处不执行移除操作，仅清空内部记录（避免内存泄漏）
            _injectedAttributes.Clear();
            _lastMoonLevel = 0;
        }
    }
}