using UnityEngine;

namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// 玩家外观/视觉覆盖接口（瞳孔、TTS位置、眼部材质、全身材质特效）。
    /// 所有方法作用于本地玩家。
    /// </summary>
    public interface IAppearanceOverrideBridge
    {
        /// <summary>
        /// 覆盖瞳孔大小（带弹簧过渡）。
        /// </summary>
        /// <param name="multiplier">目标倍数</param>
        /// <param name="priority">优先级（数值越高越优先）</param>
        /// <param name="springSpeedIn">进入速度</param>
        /// <param name="dampIn">进入阻尼</param>
        /// <param name="springSpeedOut">退出速度</param>
        /// <param name="dampOut">退出阻尼</param>
        /// <param name="time">持续时间（秒）</param>
        void OverridePupilSize(float multiplier, int priority, float springSpeedIn, float dampIn, float springSpeedOut, float dampOut, float time = 0.1f);

        /// <summary>
        /// 覆盖TTS语音位置（使语音跟随玩家头部，持续指定时间）。
        /// </summary>
        /// <param name="time">持续时间（秒）</param>
        void OverrideTTSPosition(float time = 0.1f);

        /// <summary>
        /// 覆盖眼部材质颜色（红/绿/爱心/天花板眼/反转），优先级控制覆盖竞争。
        /// </summary>
        void EyeMaterialOverride(PlayerHealth.EyeOverrideState state, float time, int priority);

        /// <summary>
        /// 覆盖全身材质特效（如升级闪光）。
        /// </summary>
        void MaterialEffectOverride(PlayerHealth.Effect effect);
    }
}