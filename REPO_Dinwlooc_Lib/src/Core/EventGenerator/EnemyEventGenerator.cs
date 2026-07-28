using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dinwlooc.Common.Core
{
    /// <summary>
    /// 怪物事件生成器：负责扫描怪物列表，并为每个怪物挂载 <see cref="EnemyEventRelay"/> 转发器。
    /// 继承自 <see cref="EventGeneratorBase{object}"/>，由模组显式调用 RegisterStep 启动。
    /// 监听场景加载事件，在关卡重进时清空已挂载 ID 缓存，确保新怪物可正确挂载。
    /// </summary>
    public class EnemyEventGenerator : EventGeneratorBase<object>
    {
        private static EnemyEventGenerator _instance;
        public static EnemyEventGenerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject(nameof(EnemyEventGenerator));
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<EnemyEventGenerator>();
                }
                return _instance;
            }
        }

        private IGameStateBridge _gameState;
        private IEnemyBridge _enemyBridge;
        private readonly HashSet<int> _attachedEnemyIds = new HashSet<int>();
        private bool _isInitialized = false;
        private bool _sceneLoadedSubscribed = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 订阅场景加载事件，在关卡加载时清空缓存
            if (!_sceneLoadedSubscribed)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                _sceneLoadedSubscribed = true;
            }
        }

        private void Start()
        {
            _gameState = BridgeLocator.GameState;
            _enemyBridge = BridgeLocator.Enemy;
            _isInitialized = true;
            CommonPlugin.Logger.LogInfo("EnemyEventGenerator created (idle).");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 只在关卡场景加载时清空（非主菜单/商店）
            if (!SemiFunc.RunIsLevel()) return;
            if (_gameState != null && _gameState.IsMainMenu()) return;

            // 清空已挂载 ID 缓存，因为怪物实例已重置
            _attachedEnemyIds.Clear();
            CommonPlugin.Logger.LogInfo($"EnemyEventGenerator 清空已挂载 ID 缓存（关卡加载：{scene.name}）。");
        }

        protected override void GenerateEvent()
        {
            // 仅在主机/单机模式下运行
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            if (!_isInitialized || _gameState == null || _enemyBridge == null)
                return;

            // 只在关卡中运行
            if (_gameState.IsMainMenu() || !_gameState.IsLevelLoaded())
                return;

            // 执行扫描
            IReadOnlyList<EnemyParent> allEnemies = _enemyBridge.GetAllEnemies();
            if (allEnemies == null || allEnemies.Count == 0)
            {
                if (_attachedEnemyIds.Count > 0)
                    _attachedEnemyIds.Clear();
                return;
            }

            HashSet<int> currentEnemyIds = new HashSet<int>();
            foreach (EnemyParent ep in allEnemies)
            {
                if (ep == null || ep.Enemy == null)
                    continue;
                int id = ep.GetInstanceID();
                currentEnemyIds.Add(id);

                if (!_attachedEnemyIds.Contains(id))
                {
                    GameObject enemyGO = ep.Enemy.gameObject;
                    if (enemyGO.GetComponent<EnemyEventRelay>() == null)
                    {
                        enemyGO.AddComponent<EnemyEventRelay>();
                        CommonPlugin.Logger.LogInfo($"Attached EnemyEventRelay to {ep.name} (ID:{id})");
                    }
                    _attachedEnemyIds.Add(id);
                }
            }

            _attachedEnemyIds.RemoveWhere(id => !currentEnemyIds.Contains(id));
        }

        private void OnDestroy()
        {
            if (_sceneLoadedSubscribed)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _sceneLoadedSubscribed = false;
            }
        }
    }
}