using System.Reflection;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using Dinwlooc.Common.Reflection;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    /// <summary>
    /// 实现 IEnemyModifierBridge，通过依赖库的 ReflectionCache 中心缓存访问私有字段。
    /// 所有操作均检查主机权限。
    /// </summary>
    public class EnemyModifierBridge : BridgeSingleton<EnemyModifierBridge>, IEnemyModifierBridge
    {
        private EnemyModifierBridge() { }

        public void SetHealth(EnemyParent enemy, int newHealth)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (enemy == null || enemy.Enemy == null) return;

            EnemyHealth health = enemy.Enemy.Health;
            if (health == null) return;

            health.health = newHealth;
            health.healthCurrent = newHealth;
        }

        public void ResetStun(EnemyParent enemy)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (enemy == null || enemy.Enemy == null) return;

            Enemy enemyComp = enemy.Enemy;
            if (!enemyComp.HasStateStunned) return;

            FieldInfo stateStunnedField = ReflectionCache.GetField(
                typeof(Enemy),
                "StateStunned",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (stateStunnedField == null)
            {
                CommonPlugin.Logger.LogWarning("无法获取 Enemy.StateStunned 字段，重置眩晕失败。");
                return;
            }

            object stateStunned = stateStunnedField.GetValue(enemyComp);
            if (stateStunned == null) return;

            FieldInfo stunTimerField = ReflectionCache.GetField(
                stateStunned.GetType(),
                "stunTimer",
                BindingFlags.Public | BindingFlags.Instance);

            if (stunTimerField == null)
            {
                CommonPlugin.Logger.LogWarning("无法获取 EnemyStateStunned.stunTimer 字段，重置眩晕失败。");
                return;
            }

            try
            {
                stunTimerField.SetValue(stateStunned, 0f);
            }
            catch (System.Exception ex)
            {
                CommonPlugin.Logger.LogError($"重置眩晕失败: {ex.Message}");
            }
        }

        public void ForceChase(EnemyParent enemy, PlayerAvatar target)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (enemy == null || enemy.Enemy == null) return;
            if (target == null || target.isDisabled) return;

            enemy.Enemy.SetChaseTarget(target);
        }

        public void ApplyStunImmunity(EnemyParent enemy, float duration)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (enemy == null || enemy.Enemy == null) return;
            if (duration <= 0f) return;

            Enemy enemyComp = enemy.Enemy;
            if (!enemyComp.HasStateStunned) return;

            ResetStun(enemy);

            FieldInfo stateStunnedField = ReflectionCache.GetField(
                typeof(Enemy),
                "StateStunned",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (stateStunnedField == null)
            {
                CommonPlugin.Logger.LogWarning("无法获取 Enemy.StateStunned 字段，施加眩晕免疫失败。");
                return;
            }

            object stateStunned = stateStunnedField.GetValue(enemyComp);
            if (stateStunned == null) return;

            MethodInfo overrideDisableMethod = ReflectionCache.GetMethod(
                stateStunned.GetType(),
                "OverrideDisable",
                BindingFlags.Public | BindingFlags.Instance);

            if (overrideDisableMethod == null)
            {
                CommonPlugin.Logger.LogWarning("无法获取 EnemyStateStunned.OverrideDisable 方法，施加眩晕免疫失败。");
                return;
            }

            try
            {
                overrideDisableMethod.Invoke(stateStunned, new object[] { duration });
                CommonPlugin.Logger.LogDebug($"为怪物 {enemy.name} 施加 {duration} 秒眩晕免疫。");
            }
            catch (System.Exception ex)
            {
                CommonPlugin.Logger.LogError($"施加眩晕免疫失败: {ex.Message}");
            }
        }
    }
}