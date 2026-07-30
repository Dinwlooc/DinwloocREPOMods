using UnityEngine;

namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// 玩家翻滚/倒地覆盖接口，提供对本地玩家翻滚状态及关联行为的控制。
    /// </summary>
    public interface ITumbleOverrideBridge
    {
        /// <summary>覆盖翻滚时对敌人的伤害碰撞（持续指定时间）。</summary>
        void OverrideEnemyHurt(float time);

        /// <summary>强制翻滚时注视摄像头方向（持续指定时间）。</summary>
        void OverrideLookAtCamera(float time, float speed = 5f, float dampen = 3f);

        /// <summary>禁用翻滚时的自动注视（持续指定时间）。</summary>
        void OverrideDisableLookAtCamera(float time);

        /// <summary>禁用翻滚UI提示（持续指定时间）。</summary>
        void OverrideTumbleUIDisable(float time);

        /// <summary>禁用翻滚移动音效（持续指定时间）。</summary>
        void OverrideDisableTumbleMoveSound(float time);

        /// <summary>强制开启/关闭翻滚状态（仅主机有效）。</summary>
        void OverrideTumble(bool active);

        /// <summary>覆盖翻滚持续时间（仅主机有效）。</summary>
        void OverrideTumbleTime(float time);

        /// <summary>禁用玩家对敌人的调查触发（持续指定时间）。</summary>
        void OverrideDisableEnemyInvestigate(float time = 1f);
    }
}