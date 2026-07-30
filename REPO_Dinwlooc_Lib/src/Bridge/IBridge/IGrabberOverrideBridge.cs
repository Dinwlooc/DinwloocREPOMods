using UnityEngine;

namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// 抓取器覆盖接口，提供对本地玩家抓取器（PhysGrabber）的临时行为控制。
    /// </summary>
    public interface IGrabberOverrideBridge
    {
        /// <summary>强制抓取指定物理对象（若已抓取则先释放）。</summary>
        void OverrideGrab(PhysGrabObject target, float grabTime = 0.1f, bool grabRelease = false);

        /// <summary>覆盖抓取光束颜色（持续指定时间）。</summary>
        void OverrideBeamColor(Color color, float time = 0.1f);

        /// <summary>强制释放当前抓取对象（可指定释放对象ID，-1表示当前）。</summary>
        void OverrideGrabRelease(int releaseObjectViewID, float disableTimer = 0.5f);

        /// <summary>禁用抓取输入（持续指定时间）。</summary>
        void OverrideGrabDisable(float time);

        /// <summary>覆盖抓取距离（绝对值，持续0.1秒后恢复）。</summary>
        void OverrideGrabDistance(float dist);

        /// <summary>覆盖最小抓取距离（绝对值，持续0.1秒后恢复）。</summary>
        void OverrideMinimumGrabDistance(float dist);

        /// <summary>覆盖抓取强度（-1表示使用默认）。</summary>
        void OverrideGrabStrength(float strength, float time = 0.1f);

        /// <summary>覆盖抓取点（目标Transform）。</summary>
        void OverrideGrabPoint(Transform grabPoint);

        /// <summary>覆盖瞄准变换（用于拉取方向）。</summary>
        void OverrideAimTransform(Transform aimTransform, float time = 0.1f);

        /// <summary>覆盖抓取器视觉位置（目标Transform）。</summary>
        void OverrideGrabberVisualPosition(Transform target);

        /// <summary>禁用特殊抓取能力（如攀爬、过载），持续指定时间。</summary>
        void OverrideDisableSpecialGrabPowers(float time = 1f);

        /// <summary>将抓取器颜色临时覆盖为绿色（持续指定时间）。</summary>
        void OverrideColorToGreen(float time = 0.1f);

        /// <summary>将抓取器颜色临时覆盖为紫色（持续指定时间）。</summary>
        void OverrideColorToPurple(float time = 0.1f);

        /// <summary>覆盖“始终可抓取”状态（持续指定时间）。</summary>
        void OverrideAlwaysGrabbable(float time = 0.1f);

        /// <summary>增量调整拉取距离（正值远离，负值靠近）。</summary>
        void OverridePullDistanceIncrement(float distSpeed);

        /// <summary>禁用过载（Overcharge）功能（持续指定时间）。</summary>
        void OverrideOverchargeDisable(float disableTimer = 0.1f);

        /// <summary>禁用物理抓取力（持续指定时间，力不施加）。</summary>
        void OverridePhysGrabForcesDisable(float time);

        /// <summary>禁用抓取时的注视跟随（持续指定时间）。</summary>
        void OverrideDisableGrabLookAt(float time = 0.1f);

        /// <summary>向发现跳过列表添加一个Transform（用于忽略特定物体的遮挡检测）。</summary>
        void OverrideDiscoverSkipListSet(Transform transform);
    }
}