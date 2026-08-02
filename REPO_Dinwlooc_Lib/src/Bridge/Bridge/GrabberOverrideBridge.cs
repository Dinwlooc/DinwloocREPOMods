using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    /// <summary>
    /// 抓取器覆盖实现，通过本地玩家的 PhysGrabber 调用原版覆盖方法。
    /// </summary>
    public class GrabberOverrideBridge : BridgeSingleton<GrabberOverrideBridge>, IGrabberOverrideBridge
    {
        private const float DefaultTime = 0.1f;
        private const float DefaultDisableTimer = 0.5f;

        private GrabberOverrideBridge() { }

        private PhysGrabber GetGrabber()
        {
            if (PlayerController.instance?.playerAvatarScript?.physGrabber == null)
            {
                CommonPlugin.Logger.LogWarning("[GrabberOverrideBridge] PhysGrabber is null.");
                return null;
            }
            return PlayerController.instance.playerAvatarScript.physGrabber;
        }

        public void OverrideGrab(PhysGrabObject target, float grabTime = DefaultTime, bool grabRelease = false)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideGrab(target, grabTime, grabRelease);
        }

        public void OverrideBeamColor(Color color, float time = DefaultTime)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideBeamColor(color, time);
        }

        public void OverrideGrabRelease(int releaseObjectViewID, float disableTimer = DefaultDisableTimer)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideGrabRelease(releaseObjectViewID, disableTimer);
        }

        public void OverrideGrabDisable(float time)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideGrabDisable(time);
        }

        public void OverrideGrabDistance(float dist)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideGrabDistance(dist);
        }

        public void OverrideMinimumGrabDistance(float dist)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideMinimumGrabDistance(dist);
        }

        public void OverrideGrabStrength(float strength, float time = DefaultTime)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideGrabStrength(strength, time);
        }

        public void OverrideGrabPoint(Transform grabPoint)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideGrabPoint(grabPoint);
        }

        public void OverrideAimTransform(Transform aimTransform, float time = DefaultTime)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideAimTransform(aimTransform, time);
        }

        public void OverrideGrabberVisualPosition(Transform target)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideGrabberVisualPosition(target);
        }

        public void OverrideDisableSpecialGrabPowers(float time = 1f)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer())
            {
                CommonPlugin.Logger.LogWarning("[GrabberOverrideBridge] OverrideDisableSpecialGrabPowers requires host.");
                return;
            }
            grabber.OverrideDisableSpecialGrabPowers(time);
        }

        public void OverrideColorToGreen(float time = DefaultTime)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideColorToGreen(time);
        }

        public void OverrideColorToPurple(float time = DefaultTime)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideColorToPurple(time);
        }

        public void OverrideAlwaysGrabbable(float time = DefaultTime)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideAlwaysGrabbable(time);
        }

        public void OverridePullDistanceIncrement(float distSpeed)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverridePullDistanceIncrement(distSpeed);
        }

        public void OverrideOverchargeDisable(float disableTimer = DefaultTime)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideOverchargeDisable(disableTimer);
        }

        public void OverridePhysGrabForcesDisable(float time)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverridePhysGrabForcesDisable(time);
        }

        public void OverrideDisableGrabLookAt(float time = DefaultTime)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideDisableGrabLookAt(time);
        }

        public void OverrideDiscoverSkipListSet(Transform transform)
        {
            PhysGrabber grabber = GetGrabber();
            if (grabber == null) return;
            grabber.OverrideDiscoverSkipListSet(transform);
        }
    }
}