using UnityEngine;

namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// 怪物属性修改与行为控制的桥接接口（扩展功能）。
    /// 由依赖库提供，供所有模组复用。
    /// </summary>
    public interface IEnemyModifierBridge
    {
        /// <summary>设置怪物的生命值（同时修改当前值和上限），仅主机有效。</summary>
        void SetHealth(EnemyParent enemy, int newHealth);

        /// <summary>重置怪物的眩晕计时器（若存在），仅主机有效。</summary>
        void ResetStun(EnemyParent enemy);

        /// <summary>
        /// 强制怪物追击指定玩家（等同于调用 Enemy.SetChaseTarget），仅主机有效。
        /// </summary>
        void ForceChase(EnemyParent enemy, PlayerAvatar target);

        /// <summary>
        /// 为怪物施加眩晕免疫（持续期间无法被新的眩晕覆盖，且当前眩晕被清除）。
        /// 通过调用 EnemyStateStunned.OverrideDisable(duration) 实现。
        /// </summary>
        void ApplyStunImmunity(EnemyParent enemy, float duration);
    }
}