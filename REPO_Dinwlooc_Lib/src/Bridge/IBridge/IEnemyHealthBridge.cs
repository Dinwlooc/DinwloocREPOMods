using UnityEngine;

namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// 敌人健康系统桥接接口，提供对 EnemyHealth 组件的完整操作。
    /// 所有修改操作均需主机/单机权限。
    /// </summary>
    public interface IEnemyHealthBridge
    {
        /// <summary>获取敌人当前生命值</summary>
        int GetCurrentHealth(EnemyParent enemy);

        /// <summary>获取敌人最大生命值（初始值）</summary>
        int GetMaxHealth(EnemyParent enemy);

        /// <summary>判断敌人是否已死亡</summary>
        bool IsDead(EnemyParent enemy);

        /// <summary>获取当前伤害抗性（0~1）</summary>
        float GetDamageResistance(EnemyParent enemy);

        /// <summary>覆盖伤害抗性（持续指定时间）</summary>
        void SetDamageResistance(EnemyParent enemy, float resistance, float time);

        /// <summary>治疗敌人（增加生命值）</summary>
        void Heal(EnemyParent enemy, int amount);

        /// <summary>对敌人造成伤害（方向用于冲击效果）</summary>
        void Hurt(EnemyParent enemy, int damage, Vector3 direction);

        /// <summary>禁用物体碰撞伤害（持续指定时间）</summary>
        void ObjectHurtDisable(EnemyParent enemy, float time);

        /// <summary>覆盖非眩晕状态下的受伤判定（持续指定时间）</summary>
        void NonStunHurtOverride(EnemyParent enemy, float time);

        /// <summary>重置生命值至满血，并标记为未死亡</summary>
        void ResetHealth(EnemyParent enemy);

        /// <summary>完全重生敌人（调用 OnSpawn，重置所有状态）</summary>
        void Respawn(EnemyParent enemy);
    }
}