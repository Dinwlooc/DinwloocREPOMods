// 文件：Dinwlooc.Common/Core/SceneEventGenerator.cs
using Dinwlooc.Common.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dinwlooc.Common.Core
{
    /// <summary>
    /// 场景切换事件生成器，监听 Unity 场景加载事件，
    /// 在所有客户端发布 SceneChangedEvent（不受主机权限限制）。
    /// 使用懒加载单例，首次访问 Instance 时创建。
    /// </summary>
    public class SceneEventGenerator : MonoBehaviour
    {
        private static SceneEventGenerator? _instance;
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
            _ = Instance; // 触发 Instance 属性，创建 GameObject 并启动 Awake
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
            // 忽略加载模式（Additive 也视为场景切换）
            SceneType type = DetermineSceneType(scene);
            SceneChangedEvent evt = new SceneChangedEvent(scene.name, scene.buildIndex, type);
            EventBus.Publish(evt);
            CommonPlugin.Logger.LogInfo($"[SceneEventGenerator] 场景切换: {scene.name} (索引 {scene.buildIndex}, 类型 {type})");
        }

        private static SceneType DetermineSceneType(Scene scene)
        {
            if (scene.name == "MainMenu" || scene.name == "Main Menu")
                return SceneType.MainMenu;

            if (SemiFunc.RunIsLobbyMenu())
                return SceneType.Lobby;

            if (SemiFunc.RunIsShop())
                return SceneType.Shop;

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
    }
}