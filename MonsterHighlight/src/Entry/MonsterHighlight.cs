using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace MonsterHighlight
{
    [BepInPlugin("Dinwlooc.MonsterHighlight", "MonsterHighlight", "1.0.0")]
    [BepInDependency("Dinwlooc.Common")]
    public class MonsterHighlight : BaseUnityPlugin
    {
        public new static ManualLogSource Logger { get; private set; } = null!;

        private MonsterHighlightConfig _config = null!;
        private HighlightController _controller = null!;
        private bool _isControllerStarted = false;
        private bool _subscribedToScene = false;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo("MonsterHighlight Awake started.");

            try
            {
                _config = MonsterHighlightConfig.Instance;
                _config.Initialize(Config);

                RegisterTranslations();

                // 确保 SceneEventGenerator 激活（不影响原生监听）
                var _ = SceneEventGenerator.Instance;
                Logger.LogInfo("SceneEventGenerator instance ensured.");

                var enemyBridge = BridgeLocator.Enemy;
                var playerBridge = BridgeLocator.Player;
                var gameStateBridge = BridgeLocator.GameState;

                _controller = new HighlightController(
                    _config,
                    enemyBridge,
                    playerBridge,
                    gameStateBridge
                );

                // 订阅 SceneChangedEvent（备用）
                EventBus.Subscribe<SceneChangedEvent>(OnSceneChanged);
                Logger.LogInfo("Subscribed to SceneChangedEvent.");

                // 直接订阅 Unity 场景加载事件，确保可靠性
                if (!_subscribedToScene)
                {
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    _subscribedToScene = true;
                    Logger.LogInfo("Subscribed to SceneManager.sceneLoaded.");
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error in MonsterHighlight.Awake: {ex}");
            }

            Logger.LogInfo("MonsterHighlight Awake completed.");
        }

        private void RegisterTranslations()
        {
            var translations = new Dictionary<string, string>
            {
                ["Enabled"] = "启用模组",
                ["Highlight Preset"] = "高亮颜色",
                ["Enable Emission"] = "启用自发光",
                ["Enable Indicator"] = "启用屏幕指示器",
                ["Check Interval Ms"] = "检测间隔(毫秒)",
                ["Indicator Update Step"] = "指示器更新步长(帧)",
                ["Indicator Size"] = "指示器基础尺寸",
                ["Min Distance"] = "开始缩小距离(米)",
                ["Max Distance"] = "达到最小尺寸距离(米)",
                ["Min Size Ratio"] = "最小尺寸比例",
                ["Indicator Alpha"] = "指示器透明度"
            };

            TranslationManager.RegisterTranslations(
                Info.Metadata.GUID,
                "zh",
                1,
                translations
            );
        }

        private void OnSceneChanged(SceneChangedEvent evt)
        {
            Logger.LogInfo($"OnSceneChanged: {evt.Type} - {evt.SceneName}");
            if (evt.Type == SceneType.Level)
            {
                StartController();
            }
            else
            {
                StopController();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.LogInfo($"OnSceneLoaded: {scene.name}, buildIndex: {scene.buildIndex}");
            if (SemiFunc.RunIsLevel())
            {
                StartController();
            }
            else
            {
                StopController();
            }
        }

        private void StartController()
        {
            if (!_isControllerStarted && _config.Enabled.Value)
            {
                _controller.Start();
                _isControllerStarted = true;
                Logger.LogInfo("MonsterHighlight started in level.");
            }
        }

        private void StopController()
        {
            if (_isControllerStarted)
            {
                _controller.Stop();
                _isControllerStarted = false;
                Logger.LogInfo("MonsterHighlight stopped (not in level).");
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SceneChangedEvent>(OnSceneChanged);
            if (_subscribedToScene)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _subscribedToScene = false;
            }
            if (_isControllerStarted)
                _controller.Stop();
        }
    }
}