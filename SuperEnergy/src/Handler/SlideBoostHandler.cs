using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperEnergy
{
    /// <summary>
    /// 滑铲效能加速处理器：延长滑铲持续时间，同时等比例降低速度衰减系数，
    /// 保证滑铲总路程不变，避免反向加速。
    /// </summary>
    public class SlideBoostHandler : IEnergyHandler
    {
        private const float DEFAULT_SLIDE_TIME = 1f;
        private const float DEFAULT_SLIDE_DECAY = 0.1f;
        private const float MIN_SLIDE_TIME = 0.05f;
        private const float MIN_SLIDE_DECAY = 0.001f;

        private int _lastAppliedPercent = -1;
        private bool _isSubscribed = false;

        public SlideBoostHandler()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _isSubscribed = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (SemiFunc.RunIsLevel())
            {
                _lastAppliedPercent = -1;
                SuperEnergy.Logger.LogInfo($"滑铲配置：场景 {scene.name} 加载，重置应用状态");
            }
        }

        public void Process(bool isHost, float deltaTime)
        {
            if (!SemiFunc.RunIsLevel())
                return;

            PlayerController ctrl = PlayerController.instance;
            if (ctrl == null)
                return;

            if (!StaminaConfigManager.TryGetEffectiveConfig(out RemoteStaminaConfig? remoteConfig) || remoteConfig == null)
                return;

            SuperEnergyConfig config = SuperEnergyConfig.Instance;
            if (!config.SlideBoostEnabled.Value)
                return;

            int percent = remoteConfig.SlideBoostPercent;
            if (_lastAppliedPercent == percent)
                return;

            float factor = 1f + percent / 100f;
            factor = Mathf.Max(0.01f, factor);

            float newTime = DEFAULT_SLIDE_TIME * factor;
            ctrl.SlideTime = Mathf.Max(MIN_SLIDE_TIME, newTime);

            float newDecay = DEFAULT_SLIDE_DECAY / factor;
            ctrl.SlideDecay = Mathf.Max(MIN_SLIDE_DECAY, newDecay);

            _lastAppliedPercent = percent;

            SuperEnergy.Logger.LogInfo(
                $"滑铲效能倍率应用：百分比={percent}%，" +
                $"滑铲时间={ctrl.SlideTime:F2}秒，衰减系数={ctrl.SlideDecay:F3}"
            );
        }

        public void Unsubscribe()
        {
            if (_isSubscribed)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _isSubscribed = false;
            }
        }
    }
}