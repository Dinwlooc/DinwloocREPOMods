// 文件：Dinwlooc.Common/Core/SceneEventGenerator.cs
using Dinwlooc.Common.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dinwlooc.Common.Core
{
    /// <summary>
    /// 场景切换事件生成器，监听 Unity 场景加载事件，
    /// 在所有客户端发布 SceneChangedEvent（不受主机权限限制）。
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
            yield return null; // 等待一帧，确保 SemiFunc 状态已更新

            string sceneName = scene.name;
            int buildIndex = scene.buildIndex;
            SceneType type = DetermineSceneType(scene);

            SceneChangedEvent evt = new SceneChangedEvent(sceneName, buildIndex, type);
            EventBus.Publish(evt);
            CommonPlugin.Logger.LogInfo($"[SceneEventGenerator] 场景切换: {sceneName} (索引 {buildIndex}, 类型 {type})");
        }

        /// <summary>
        /// 判断场景类型，完全对齐原版 RunManager 的可用判断方法。
        /// 延迟一帧后调用，此时 SemiFunc 状态已稳定。
        /// 对于主菜单等无法通过 SemiFunc 识别的场景，统一返回 Unknown，
        /// 由订阅者（如 SyncManager）决定是否忽略。
        /// </summary>
        public static SceneType DetermineSceneType(Scene scene)
        {
            // 过渡场景直接过滤，避免误判为 Level
            if (scene.name == "Reload")
                return SceneType.Unknown;

            // 严格按照原版 SemiFunc 方法顺序判断
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

            // 主菜单或其他未识别场景
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