// Dinwlooc.Common/Core/SceneEventGenerator.cs
using Dinwlooc.Common.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dinwlooc.Common.Core
{
    public class SceneEventGenerator : MonoBehaviour, IEventGenerator
    {
        private static SceneEventGenerator _instance;
        private static readonly object _lock = new object();
        private bool _subscribed = false;

        public static SceneEventGenerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            GameObject go = new GameObject(nameof(SceneEventGenerator));
                            DontDestroyOnLoad(go);
                            _instance = go.AddComponent<SceneEventGenerator>();
                        }
                    }
                }
                return _instance;
            }
        }

        public static void Activate()
        {
            _ = Instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (!_subscribed)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                _subscribed = true;
                CommonPlugin.Logger.LogInfo("[SceneEventGenerator] 已订阅场景加载事件。");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(DelayedPublishSceneChanged(scene));
        }

        private System.Collections.IEnumerator DelayedPublishSceneChanged(Scene scene)
        {
            yield return null;
            string sceneName = scene.name;
            int buildIndex = scene.buildIndex;
            SceneType type = DetermineSceneType(scene);

            SceneChangedEvent evt = new SceneChangedEvent(sceneName, buildIndex, type);
            EventBus.Publish(evt);
            CommonPlugin.Logger.LogInfo($"[SceneEventGenerator] 场景切换: {sceneName} (索引 {buildIndex}, 类型 {type})");
        }

        public static SceneType DetermineSceneType(Scene scene)
        {
            if (scene.name == "Reload")
                return SceneType.Unknown;

            if (SemiFunc.RunIsLobbyMenu())
                return SceneType.LobbyMenu;

            if (SemiFunc.RunIsLobby())
                return SceneType.Lobby;

            if (SemiFunc.RunIsShop())
                return SceneType.Shop;

            if (SemiFunc.RunIsTutorial())
                return SceneType.Tutorial;

            if (SemiFunc.RunIsRecording())
                return SceneType.Recording;

            if (SemiFunc.RunIsLevel())
                return SceneType.Level;

            return SceneType.Unknown;
        }

        private void OnDestroy()
        {
            if (_subscribed)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _subscribed = false;
            }
        }

        // ---------- IEventGenerator 实现 ----------
        // 场景生成器始终处于激活状态，Enable 只需确保实例已创建
        public void Enable(int stepFrames)
        {
            Activate(); // 确保实例存在并已订阅
        }

        public void Disable()
        {
            // 场景生成器不关闭，始终监听
        }
    }
}