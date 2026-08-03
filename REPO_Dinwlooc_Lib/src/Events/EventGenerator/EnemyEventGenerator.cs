using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    /// <summary>
    /// 怪物事件生成器：负责扫描怪物列表，并为每个怪物挂载 <see cref="EnemyEventRelay"/> 转发器。
    /// 继承自 <see cref="EventGeneratorBase{object}"/>，由模组显式调用 RegisterStep 启动。
    /// 监听场景切换事件，在关卡重进时清空已挂载 ID 缓存，确保新怪物可正确挂载。
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
        private bool _subscribed = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 订阅场景切换事件（替代 Unity 原生 sceneLoaded）
            if (!_subscribed)
            {
                EventBus.Subscribe<SceneChangedEvent>(OnSceneChanged);
                _subscribed = true;
            }
        }

        private void Start()
        {
            _gameState = BridgeLocator.GameState;
            _enemyBridge = BridgeLocator.Enemy;
            _isInitialized = true;
            CommonPlugin.Logger.LogInfo("[EnemyEventGenerator] created (idle).");
        }

        // 场景切换事件处理（替代 OnSceneLoaded）
        private void OnSceneChanged(SceneChangedEvent evt)
        {
            // 只在关卡场景加载时清空（排除主菜单、商店、过渡场景等）
            if (evt.Type != SceneType.Level)
                return;
            // 清空已挂载 ID 缓存，因为怪物实例已重置
            _attachedEnemyIds.Clear();
            CommonPlugin.Logger.LogInfo($"[EnemyEventGenerator] 清空已挂载 ID 缓存（关卡加载：{evt.SceneName}）。");
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
            IReadOnlyList<EnemyParent> allEnemies = _enemyBridge.GetAllEnemies();
            if (allEnemies == null || allEnemies.Count == 0)
            {
                if (_attachedEnemyIds.Count > 0)
                    _attachedEnemyIds.Clear();
                return;
            }
            HashSet<int> currentEnemyIds = new HashSet<int>();
            int attachedCount = 0;  // 新增：统计成功挂载的数量
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
                        attachedCount++;  // 只计数，不逐条打印日志
                    }
                    _attachedEnemyIds.Add(id);
                }
            }
            // 仅在成功挂载时输出汇总日志（若没有新挂载则静默）
            if (attachedCount > 0)
                CommonPlugin.Logger.LogInfo($"[EnemyEventGenerator] Attached {attachedCount} EnemyEventRelay(s).");
            // 清理已不存在的怪物 ID
            _attachedEnemyIds.RemoveWhere(id => !currentEnemyIds.Contains(id));
        }

        private void OnDestroy()
        {
            if (_subscribed)
            {
                EventBus.Unsubscribe<SceneChangedEvent>(OnSceneChanged);
                _subscribed = false;
            }
        }
    }
}