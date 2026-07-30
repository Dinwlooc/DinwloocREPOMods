using UnityEngine;

namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// 玩家死亡头部覆盖接口，提供对死亡头部状态的临时控制。
    /// </summary>
    public interface IDeathHeadOverrideBridge
    {
        /// <summary>强制进入附身（Spectated）状态，持续指定时间。</summary>
        void OverrideSpectated(float time);

        /// <summary>强制退出附身状态并恢复。</summary>
        void OverrideSpectatedReset();

        /// <summary>覆盖死亡头部的位置/旋转，先跟随目标变换，然后释放到指定位置/旋转。</summary>
        void OverridePositionRotation(Transform followTransform, Vector3 releasePosition, Quaternion releaseRotation, float time);

        /// <summary>重置位置/旋转覆盖，恢复到释放终点。</summary>
        void OverridePositionRotationReset();
    }
}