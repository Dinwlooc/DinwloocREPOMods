using System.Reflection;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using Dinwlooc.Common.Reflection;
using Unity.VisualScripting;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    /// <summary>
    /// 实现 IEnemyModifierBridge，通过依赖库的 ReflectionCache 中心缓存访问私有字段。
    /// 所有操作均检查主机权限。
    /// </summary>
    public class EnemyModifierBridge : IEnemyModifierBridge
    {
        private static EnemyModifierBridge? _instance;
        public static EnemyModifierBridge Instance => _instance ??= new EnemyModifierBridge();

        private EnemyModifierBridge() { }
        public void SetHealth(EnemyParent enemy, int newHealth)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (enemy == null || enemy.Enemy == null) return;

            EnemyHealth health = enemy.Enemy.Health;
            if (health == null) return;

            // 直接修改 public 字段
            health.health = newHealth;
            health.healthCurrent = newHealth;
        }

        public void ResetStun(EnemyParent enemy)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (enemy == null || enemy.Enemy == null) return;

            Enemy enemyComp = enemy.Enemy;
            if (!enemyComp.HasStateStunned) return;

            // 使用 ReflectionCache 获取私有字段（自动缓存到中心）
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

            // 获取 stunTimer 字段（通常是 public）
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

            Enemy enemyComp = enemy.Enemy;
            if (target == null || target.isDisabled) return;

            // 直接调用原版公开方法
            enemyComp.SetChaseTarget(target);
        }
        public void ApplyStunImmunity(EnemyParent enemy, float duration)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (enemy == null || enemy.Enemy == null) return;
            if (duration <= 0f) return;

            Enemy enemyComp = enemy.Enemy;
            if (!enemyComp.HasStateStunned) return;

            // 先重置当前眩晕，确保免疫立即生效
            ResetStun(enemy);

            // 获取 EnemyStateStunned 组件并调用 OverrideDisable
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