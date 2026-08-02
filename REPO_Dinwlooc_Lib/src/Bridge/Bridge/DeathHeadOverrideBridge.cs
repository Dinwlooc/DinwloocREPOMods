using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    /// <summary>
    /// 死亡头部覆盖实现，通过本地玩家的 PlayerDeathHead 调用原版覆盖方法。
    /// </summary>
    public class DeathHeadOverrideBridge : BridgeSingleton<DeathHeadOverrideBridge>, IDeathHeadOverrideBridge
    {
        private const float DefaultTime = 0.1f;

        private DeathHeadOverrideBridge() { }

        private PlayerDeathHead GetDeathHead()
        {
            if (PlayerController.instance?.playerAvatarScript?.playerDeathHead == null)
            {
                CommonPlugin.Logger.LogWarning("[DeathHeadOverrideBridge] PlayerDeathHead is null.");
                return null;
            }
            return PlayerController.instance.playerAvatarScript.playerDeathHead;
        }

        public void OverrideSpectated(float time)
        {
            PlayerDeathHead head = GetDeathHead();
            if (head == null) return;
            head.OverrideSpectated(time);
        }

        public void OverrideSpectatedReset()
        {
            PlayerDeathHead head = GetDeathHead();
            if (head == null) return;
            head.OverrideSpectatedReset();
        }

        public void OverridePositionRotation(Transform followTransform, Vector3 releasePosition, Quaternion releaseRotation, float time)
        {
            PlayerDeathHead head = GetDeathHead();
            if (head == null) return;
            head.OverridePositionRotation(followTransform, releasePosition, releaseRotation, time);
        }

        public void OverridePositionRotationReset()
        {
            PlayerDeathHead head = GetDeathHead();
            if (head == null) return;
            head.OverridePositionRotationReset();
        }
    }
}