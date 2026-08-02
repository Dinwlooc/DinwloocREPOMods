using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperEnergy
{
    /// <summary>
    /// 滑铲效能加速处理器：依赖库事件驱动，
    /// 仅在关卡场景（SceneType.Level）中生效，
    /// 日志仅在百分比变化时输出一次。
    /// </summary>
    public class SlideBoostHandler : IEnergyHandler
    {
        private const float DEFAULT_SLIDE_TIME = 1f;
        private const float DEFAULT_SLIDE_DECAY = 0.1f;
        private const float MIN_SLIDE_TIME = 0.05f;
        private const float MIN_SLIDE_DECAY = 0.001f;

        private int _lastLoggedPercent = -1;
        private bool _needApply = false;

        public SlideBoostHandler()
        {
            // 改用依赖库的包装事件
            EventBus.Subscribe<SceneChangedEvent>(OnSceneChanged);
        }

        private void OnSceneChanged(SceneChangedEvent evt)
        {
            if (evt.Type == SceneType.Level)
            {
                _needApply = true;
                SuperEnergy.Logger.LogInfo($"滑铲配置：场景 {evt.SceneName} (Level) 加载，准备应用");
            }
        }

        public void Process(bool isHost, float deltaTime)
        {
            // 仅在需要应用时执行，避免每帧处理
            if (!_needApply)
                return;

            // 双重检查：确保当前确实是关卡场景
            if (!SemiFunc.RunIsLevel())
                return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (SceneEventGenerator.DetermineSceneType(activeScene) != SceneType.Level)
                return;

            PlayerController ctrl = PlayerController.instance;
            if (ctrl == null)
                return;

            SuperEnergyConfig config = SuperEnergyConfig.Instance;
            if (!config.SlideBoostEnabled.Value)
            {
                _needApply = false;
                return;
            }

            StaminaSyncConfig syncConfig = StaminaConfigManager.Instance.GetEffectiveConfig();
            if (syncConfig == null)
            {
                _needApply = false;
                return;
            }

            int percent = syncConfig.SlideBoostPercent;

            float factor = 1f + percent / 100f;
            factor = Mathf.Max(0.01f, factor);
            float targetTime = Mathf.Max(MIN_SLIDE_TIME, DEFAULT_SLIDE_TIME * factor);
            float targetDecay = Mathf.Max(MIN_SLIDE_DECAY, DEFAULT_SLIDE_DECAY / factor);

            // 应用滑铲参数
            ctrl.SlideTime = targetTime;
            ctrl.SlideDecay = targetDecay;

            // 日志：仅在百分比变化时输出，确保不重复
            if (_lastLoggedPercent != percent)
            {
                _lastLoggedPercent = percent;
                SuperEnergy.Logger.LogInfo(
                    $"滑铲效能倍率应用：百分比={percent}%，" +
                    $"滑铲时间={ctrl.SlideTime:F2}秒，衰减系数={ctrl.SlideDecay:F3}"
                );
            }

            // 标记已应用，本关卡内不再重复处理
            _needApply = false;
        }

        public void Unsubscribe()
        {
            EventBus.Unsubscribe<SceneChangedEvent>(OnSceneChanged);
        }
    }
}