using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using Dinwlooc.Common.Reflection;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    /// <summary>
    /// 滑铲桥接实现，通过反射缓存高效访问 PlayerController 的滑铲字段。
    /// </summary>
    public class SlideBridge : BridgeSingleton<SlideBridge>, ISlideBridge
    {
        private const float MinDecay = 0f;

        private SlideBridge() { }

        private PlayerController GetController()
        {
            return PlayerController.instance;
        }

        public bool IsSliding()
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return false;
            return ctrl.Sliding;
        }

        public float GetSlideTimerRemaining()
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return 0f;
            return ctrl.SlideTimer;
        }

        public float GetSlideDuration()
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return 0f;
            return ctrl.SlideTime;
        }

        public float GetSlideDecay()
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return 0f;
            return ctrl.SlideDecay;
        }

        public void SetSlideDecay(float decay)
        {
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer()) return;
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.SlideDecay = Mathf.Max(MinDecay, decay);
        }
    }
}