using System.Collections.Generic;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace MonsterCombatGroup.Handler
{
    /// <summary>
    /// 管理怪物眩晕抵抗逻辑，基于时间戳实现强抵抗和普通抵抗。
    /// 所有方法均为静态，线程安全（仅在主线程调用）。
    /// </summary>
    public static class ResistanceManager
    {
        // 每个怪物上次触发完整效果的时间（用于冷却）
        private static readonly Dictionary<int, float> _lastFullTriggerTime = new Dictionary<int, float>();
        // 每个怪物的强抵抗结束时间（在此时间内受击只起身不更新记录）
        private static readonly Dictionary<int, float> _strongResistEndTime = new Dictionary<int, float>();

        /// <summary>
        /// 处理单个怪物的抵抗逻辑。
        /// </summary>
        /// <param name="enemy">目标怪物</param>
        /// <param name="instanceId">怪物实例ID</param>
        /// <param name="strongDuration">强抵抗持续时间（秒），0表示无强抵抗</param>
        /// <param name="normalDuration">普通抵抗持续时间（秒）</param>
        /// <param name="cooldown">冷却时间（秒），两次完整效果之间的最小间隔</param>
        /// <param name="modifier">桥接接口，用于执行实际操作</param>
        /// <returns>是否触发了完整效果（起身+普通抵抗+记录）</returns>
        public static bool ProcessResist(
            EnemyParent enemy,
            int instanceId,
            float strongDuration,
            float normalDuration,
            float cooldown,
            IEnemyModifierBridge? modifier)
        {
            if (modifier == null) return false;
            if (enemy == null || enemy.Enemy == null) return false;

            float now = Time.time;

            // 1. 检查是否处于强抵抗有效期内
            if (_strongResistEndTime.TryGetValue(instanceId, out float strongEnd) && now < strongEnd)
            {
                // 强抵抗期间：只起身，不更新记录，不给予普通抵抗
                modifier.ResetStun(enemy);
                return false;
            }

            // 2. 检查冷却是否结束
            if (_lastFullTriggerTime.TryGetValue(instanceId, out float lastTime))
            {
                if (now - lastTime < cooldown)
                {
                    // 冷却中，不做任何事
                    return false;
                }
            }

            // 3. 冷却结束 → 触发完整效果：起身 + 普通抵抗 + 记录
            modifier.ApplyStunImmunity(enemy, normalDuration); // 内部 ResetStun + OverrideDisable

            // 更新状态
            _lastFullTriggerTime[instanceId] = now;
            if (strongDuration > 0f)
            {
                _strongResistEndTime[instanceId] = now + strongDuration;
            }
            else
            {
                _strongResistEndTime.Remove(instanceId);
            }

            return true;
        }

        /// <summary>
        /// 强制使怪物立即起身（重置眩晕）。
        /// </summary>
        public static void ForceResetStun(EnemyParent enemy, IEnemyModifierBridge? modifier)
        {
            modifier?.ResetStun(enemy);
        }

        /// <summary>
        /// 刷新另一个守卫的冷却（月相二守卫受击时调用）。
        /// </summary>
        /// <param name="currentGuardId">当前守卫的ID</param>
        /// <param name="otherGuardId">要刷新冷却的守卫ID</param>
        /// <param name="cooldown">冷却时长，用于设置为“已冷却”</param>
        public static void RefreshCooldownForGuard(int otherGuardId, float cooldown)
        {
            if (otherGuardId <= 0) return;
            float now = Time.time;
            // 将冷却置为已冷却（即 lastFullTriggerTime 设为 now - cooldown）
            _lastFullTriggerTime[otherGuardId] = now - cooldown;
            // 清除其强抵抗状态，使其下次受击立即触发完整效果
            _strongResistEndTime.Remove(otherGuardId);
        }

        /// <summary>
        /// 清除所有状态（用于场景重置）。
        /// </summary>
        public static void Reset()
        {
            _lastFullTriggerTime.Clear();
            _strongResistEndTime.Clear();
        }
    }
}