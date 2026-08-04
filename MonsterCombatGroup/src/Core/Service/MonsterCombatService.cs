using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using MonsterCombatGroup.Handler;
using Photon.Pun;
using UnityEngine;

namespace MonsterCombatGroup
{
    public class MonsterCombatService : MonoBehaviour
    {
        private const float TICK_INTERVAL = 0.5f;

        private readonly List<ICombatHandler> _handlers = new List<ICombatHandler>();
        private float _nextTickTime = 0f;
        private IGameStateBridge? _gameState;
        private bool _isInitialized = false;
        private bool _subscribed = false;

        // 分发器持有引用（便于重置）
        private CombatEventDispatcher? _dispatcher;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            _gameState = BridgeLocator.GameState;

            EnemyEventGenerator.Instance.RegisterStep(5);
            MonsterCombatGroup.Logger.LogInfo("EnemyEventGenerator 已启动。");

            // ---- 创建所有处理器 ----
            LeaderElectionHandler electionHandler = new LeaderElectionHandler();
            LeaderAbilityHandler leaderHandler = new LeaderAbilityHandler();
            GuardAbilityHandler guardHandler = new GuardAbilityHandler();
            LeaderCombatHandler combatHandler = new LeaderCombatHandler();
            LeaderDeathRewardHandler deathRewardHandler = new LeaderDeathRewardHandler();
            NormalMonsterHandler normalHandler = new NormalMonsterHandler();

            // 创建分发器（依赖 leaderHandler, guardHandler, normalHandler）
            _dispatcher = new CombatEventDispatcher(leaderHandler, guardHandler, normalHandler);

            // 将所有需要周期性 Process 的处理器加入列表
            _handlers.Add(electionHandler);
            _handlers.Add(leaderHandler);
            _handlers.Add(guardHandler);
            _handlers.Add(combatHandler);
            _handlers.Add(deathRewardHandler);
            // 注意：分发器不需要 Process，因此不加入 _handlers

            PhotonNetwork.AddCallbackTarget(this);

            if (!_subscribed)
            {
                EventBus.Subscribe<SceneChangedEvent>(OnSceneChanged);
                _subscribed = true;
            }

            _isInitialized = true;
            MonsterCombatGroup.Logger.LogInfo("MonsterCombatService initialized.");
        }

        private void Update()
        {
            if (!_isInitialized) return;
            if (_gameState == null) return;
            if (_gameState.IsMainMenu() || !_gameState.IsLevelLoaded()) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            if (Time.time < _nextTickTime) return;
            _nextTickTime = Time.time + TICK_INTERVAL;

            foreach (ICombatHandler handler in _handlers)
            {
                handler.Process(TICK_INTERVAL);
            }
        }

        private void OnSceneChanged(SceneChangedEvent evt)
        {
            if (evt.Type != SceneType.Level && evt.Type != SceneType.Lobby)
                return;
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            MonsterCombatGroup.Logger.LogInfo($"场景切换：{evt.SceneName}，重置状态。");
            // 重置所有处理器
            foreach (ICombatHandler handler in _handlers)
            {
                if (handler is IResettable resettable)
                    resettable.ResetState();
            }
            MonsterSyncManager.ClearState();
            if (evt.Type == SceneType.Level)
            {
                MoonPhaseManager.UpdateForCurrentMoon();
                MonsterSyncManager.EnsureInitialized();
                MonsterCombatGroup.Logger.LogInfo("MonsterSyncManager 已为当前关卡初始化。");
            }
        }

        private void OnJoinedRoom()
        {
            foreach (ICombatHandler handler in _handlers)
            {
                if (handler is IResettable resettable)
                    resettable.ResetState();
            }
            _dispatcher?.ResetState();
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                PhotonNetwork.RemoveCallbackTarget(this);
                if (_subscribed)
                {
                    EventBus.Unsubscribe<SceneChangedEvent>(OnSceneChanged);
                    _subscribed = false;
                }
                _dispatcher?.Dispose();
                _handlers.Clear();
                _isInitialized = false;
            }

            MoonPhaseManager.Reset();
            MonsterSyncManager.Reset();
        }
    }

    public interface IResettable
    {
        void ResetState();
    }
}