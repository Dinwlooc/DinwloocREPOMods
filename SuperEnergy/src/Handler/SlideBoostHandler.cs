using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperEnergy
{
    /// <summary>
    /// 滑铲效能加速处理器：延长滑铲持续时间，同时等比例降低速度衰减系数，
    /// 保证滑铲总路程不变，避免反向加速。
    /// 使用 SceneManager.sceneLoaded 监听关卡加载，确保每次进入关卡时重新应用配置。
    /// </summary>
    public class SlideBoostHandler : IEnergyHandler
    {
        // 原版默认滑铲参数
        private const float DEFAULT_SLIDE_TIME = 1f;
        private const float DEFAULT_SLIDE_DECAY = 0.1f;
        private const float MIN_SLIDE_TIME = 0.05f;
        private const float MIN_SLIDE_DECAY = 0.001f;

        private int _lastAppliedPercent = -1;
        private bool _isSubscribed = false;

        public SlideBoostHandler()
        {
            // 订阅场景加载事件，确保每次关卡加载时重置状态
            SceneManager.sceneLoaded += OnSceneLoaded;
            _isSubscribed = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 仅在关卡场景（非主菜单、非商店等）重置
            // 使用 SemiFunc.RunIsLevel() 判断是否关卡
            if (SemiFunc.RunIsLevel())
            {
                _lastAppliedPercent = -1;
                SuperEnergy.Logger.LogInfo($"滑铲配置：场景 {scene.name} 加载，重置应用状态");
            }
        }

        public void Process(bool isHost, float deltaTime)
        {
            // 检查是否在关卡中
            if (!SemiFunc.RunIsLevel())
            {
                return;
            }

            PlayerController ctrl = PlayerController.instance;
            if (ctrl == null)
            {
                return;
            }

            // 读取有效配置（包含本地或房主广播值）
            if (!StaminaConfigManager.TryGetEffectiveConfig(out RemoteStaminaConfig? remoteConfig) || remoteConfig == null)
            {
                return;
            }

            SuperEnergyConfig config = SuperEnergyConfig.Instance;
            if (!config.EnableSlideBoost.Value)
            {
                // 若禁用，不做任何修改（保持当前值，用户重启场景恢复原样）
                return;
            }

            int percent = remoteConfig.SlideBoostPercent;
            if (_lastAppliedPercent == percent)
            {
                return; // 倍率未变，无需更新
            }

            // 计算倍率因子
            float factor = 1f + percent / 100f;
            factor = Mathf.Max(0.01f, factor); // 至少保留 1% 以防止除零

            // 应用滑铲持续时间
            float newTime = DEFAULT_SLIDE_TIME * factor;
            ctrl.SlideTime = Mathf.Max(MIN_SLIDE_TIME, newTime);

            // 应用滑铲速度衰减系数（与持续时间成反比，保持总路程不变）
            float newDecay = DEFAULT_SLIDE_DECAY / factor;
            ctrl.SlideDecay = Mathf.Max(MIN_SLIDE_DECAY, newDecay);

            _lastAppliedPercent = percent;

            SuperEnergy.Logger.LogInfo(
                $"滑铲效能倍率应用：百分比={percent}%，" +
                $"滑铲时间={ctrl.SlideTime:F2}秒，衰减系数={ctrl.SlideDecay:F3}"
            );
        }

        // 可选：如果需要在模组卸载时取消订阅，可由外部调用此方法
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