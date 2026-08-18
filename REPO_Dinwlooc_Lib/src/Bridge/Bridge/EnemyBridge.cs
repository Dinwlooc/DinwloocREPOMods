using System;
using System.Collections.Generic;
using System.Reflection;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using Dinwlooc.Common.Reflection;
using Photon.Pun;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    /// <summary>
    /// 敌人系统统一桥接实现，合并了基础查询、修改控制及健康系统操作。
    /// 所有修改方法均检查主机/单机权限。
    /// </summary>
    public class EnemyBridge : BridgeSingleton<EnemyBridge>,
        IEnemyBridge,
        IEnemyModifierBridge,
        IEnemyHealthBridge
    {
        // ---------- 常量（来自原 EnemyBridge） ----------
        private const float DefaultHeightOffset = 0.5f;
        private const float MinHeightOffset = 0.3f;
        private const float MaxHeightOffset = 5f;
        private const float FallbackScaleFactor = 0.8f;
        private const float BoundsExtra = 0.15f;

        private EnemyBridge() { }

        // 静态构造：自动将自身注册到 BridgeLocator 的三个接口
        static EnemyBridge()
        {
            BridgeLocator.Register<IEnemyBridge>(() => Instance);
            BridgeLocator.Register<IEnemyModifierBridge>(() => Instance);
            BridgeLocator.Register<IEnemyHealthBridge>(() => Instance);
            CommonPlugin.Logger.LogInfo("[EnemyBridge] Registered to IEnemyBridge, IEnemyModifierBridge, IEnemyHealthBridge.");
        }

        // ==================== IEnemyBridge 实现 ====================
        public IReadOnlyList<EnemyParent> GetAllEnemies()
        {
            EnemyDirector director = EnemyDirector.instance;
            if (director == null) return Array.Empty<EnemyParent>();
            var list = director.enemiesSpawned;
            return list ?? (IReadOnlyList<EnemyParent>)Array.Empty<EnemyParent>();
        }

        public bool IsEnemyValid(EnemyParent enemy)
        {
            if (enemy == null) return false;
            if (!enemy.Spawned) return false;
            if (enemy.Enemy == null) return false;
            EnemyHealth health = enemy.Enemy.Health;
            if (health == null) return false;
            if (health.health <= 0) return false;
            if (!enemy.Enemy.gameObject.activeInHierarchy) return false;
            return true;
        }

        public Vector3 GetEnemyPosition(EnemyParent enemy)
        {
            if (enemy?.Enemy == null) return Vector3.zero;
            Enemy enemyComp = enemy.Enemy;
            if (enemyComp.CenterTransform != null)
                return enemyComp.CenterTransform.position;
            return enemyComp.transform != null ? enemyComp.transform.position : Vector3.zero;
        }

        public int GetEnemyInstanceId(EnemyParent enemy)
        {
            if (enemy?.Enemy == null) return 0;
            return enemy.Enemy.GetInstanceID();
        }

        public void ApplyHighlight(EnemyParent enemy, bool active, Color color)
        {
            if (enemy == null || enemy.EnableObject == null) return;

            GameObject enableObj = enemy.EnableObject;
            Transform modelTransform = enableObj.transform.Find("[VISUALS]");
            if (modelTransform == null) modelTransform = enableObj.transform.Find("Visual");
            if (modelTransform == null) modelTransform = enableObj.transform.Find("Model");

            GameObject modelTarget = modelTransform != null ? modelTransform.gameObject : enableObj;
            Renderer[] renderers = modelTarget.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rend in renderers)
            {
                if (rend == null) continue;
                if (rend.GetComponent<ParticleSystem>() != null) continue;
                Material mat = rend.material;
                if (!mat.HasProperty("_EmissionColor")) continue;

                if (active)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 2f);
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        public float GetIndicatorHeightOffset(EnemyParent enemy)
        {
            if (enemy?.Enemy == null) return DefaultHeightOffset;
            Enemy enemyComp = enemy.Enemy;

            // 尝试通过碰撞体计算
            FieldInfo rigidField = ReflectionCache.Enemy_Rigidbody;
            try
            {
                EnemyRigidbody rigid = rigidField?.GetValue(enemyComp) as EnemyRigidbody;
                if (rigid != null)
                {
                    Collider[] colliders = rigid.GetComponentsInChildren<Collider>();
                    Vector3 center = GetEnemyPosition(enemy);
                    Bounds bounds = new Bounds(center, Vector3.zero);
                    bool hasBounds = false;
                    foreach (Collider col in colliders)
                    {
                        if (col == null || col.isTrigger) continue;
                        if (!hasBounds)
                        {
                            bounds = col.bounds;
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(col.bounds);
                        }
                    }
                    if (hasBounds)
                    {
                        float height = bounds.max.y - center.y + BoundsExtra;
                        return Mathf.Clamp(height, MinHeightOffset, MaxHeightOffset);
                    }
                }
            }
            catch { /* 忽略 */ }

            // 备用方法：通过渲染器或缩放
            try
            {
                Transform model = enemyComp.CenterTransform;
                if (model != null)
                {
                    Renderer renderer = model.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        float height = renderer.bounds.size.y * FallbackScaleFactor;
                        return Mathf.Clamp(height, MinHeightOffset, MaxHeightOffset);
                    }
                    float scaleY = model.lossyScale.y;
                    if (scaleY > 0.5f)
                    {
                        return Mathf.Clamp(scaleY * FallbackScaleFactor, MinHeightOffset, MaxHeightOffset);
                    }
                }
            }
            catch { /* 忽略 */ }

            return DefaultHeightOffset;
        }

        // ==================== IEnemyModifierBridge 实现 ====================
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
                CommonPlugin.Logger.LogWarning("[EnemyBridge] 无法获取 Enemy.StateStunned 字段，重置眩晕失败。");
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
                CommonPlugin.Logger.LogWarning("[EnemyBridge] 无法获取 EnemyStateStunned.stunTimer 字段。");
                return;
            }

            try
            {
                stunTimerField.SetValue(stateStunned, 0f);
            }
            catch (Exception ex)
            {
                CommonPlugin.Logger.LogError($"[EnemyBridge] 重置眩晕失败: {ex.Message}");
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

            ResetStun(enemy); // 先清除当前眩晕

            FieldInfo stateStunnedField = ReflectionCache.GetField(
                typeof(Enemy),
                "StateStunned",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (stateStunnedField == null)
            {
                CommonPlugin.Logger.LogWarning("[EnemyBridge] 无法获取 Enemy.StateStunned 字段，施加眩晕免疫失败。");
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
                CommonPlugin.Logger.LogWarning("[EnemyBridge] 无法获取 EnemyStateStunned.OverrideDisable 方法。");
                return;
            }

            try
            {
                overrideDisableMethod.Invoke(stateStunned, new object[] { duration });
                CommonPlugin.Logger.LogDebug($"[EnemyBridge] 为怪物 {enemy.name} 施加 {duration} 秒眩晕免疫。");
            }
            catch (Exception ex)
            {
                CommonPlugin.Logger.LogError($"[EnemyBridge] 施加眩晕免疫失败: {ex.Message}");
            }
        }

        // ==================== IEnemyHealthBridge 实现 ====================
        private EnemyHealth GetHealth(EnemyParent enemy)
        {
            if (enemy?.Enemy == null) return null;
            return enemy.Enemy.Health;
        }

        private T GetFieldValue<T>(EnemyHealth health, string fieldName)
        {
            if (health == null) return default;
            var field = ReflectionCache.GetField(typeof(EnemyHealth), fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return default;
            try { return (T)field.GetValue(health); } catch { return default; }
        }

        private void SetFieldValue(EnemyHealth health, string fieldName, object value)
        {
            if (health == null) return;
            var field = ReflectionCache.GetField(typeof(EnemyHealth), fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return;
            try { field.SetValue(health, value); } catch { }
        }

        public int GetCurrentHealth(EnemyParent enemy)
        {
            var health = GetHealth(enemy);
            return health == null ? 0 : GetFieldValue<int>(health, "healthCurrent");
        }

        public int GetMaxHealth(EnemyParent enemy)
        {
            var health = GetHealth(enemy);
            return health == null ? 0 : health.health;
        }

        public bool IsDead(EnemyParent enemy)
        {
            var health = GetHealth(enemy);
            return health == null || GetFieldValue<bool>(health, "dead");
        }

        public float GetDamageResistance(EnemyParent enemy)
        {
            var health = GetHealth(enemy);
            return health == null ? 0f : GetFieldValue<float>(health, "damageResistance");
        }

        public void SetDamageResistance(EnemyParent enemy, float resistance, float time)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            var health = GetHealth(enemy);
            if (health == null) return;
            health.OverrideDamageResistance(resistance, time);
        }

        public void Heal(EnemyParent enemy, int amount)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            var health = GetHealth(enemy);
            if (health == null || amount <= 0) return;
            health.Heal(amount);
        }

        public void Hurt(EnemyParent enemy, int damage, Vector3 direction)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            var health = GetHealth(enemy);
            if (health == null || damage <= 0) return;
            health.Hurt(damage, direction);
        }

        public void ObjectHurtDisable(EnemyParent enemy, float time)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            var health = GetHealth(enemy);
            if (health == null) return;
            health.ObjectHurtDisable(time);
        }

        public void NonStunHurtOverride(EnemyParent enemy, float time)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            var health = GetHealth(enemy);
            if (health == null) return;
            health.NonStunHurtOverride(time);
        }

        public void ResetHealth(EnemyParent enemy)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            var health = GetHealth(enemy);
            if (health == null) return;
            SetFieldValue(health, "healthCurrent", health.health);
            SetFieldValue(health, "dead", false);
        }

        public void Respawn(EnemyParent enemy)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            var health = GetHealth(enemy);
            if (health == null) return;
            health.OnSpawn();
        }
    }
}