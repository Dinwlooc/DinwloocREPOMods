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

            _handlers.Add(new LeaderElectionHandler());
            _handlers.Add(new LeaderAbilityHandler());
            _handlers.Add(new GuardAbilityHandler());
            _handlers.Add(new LeaderCombatHandler());
            _handlers.Add(new LeaderDeathRewardHandler());

            PhotonNetwork.AddCallbackTarget(this);

            // 订阅场景切换事件（替代 Unity 原生 sceneLoaded）
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

        // 场景切换事件处理（替代 OnSceneLoaded）
        private void OnSceneChanged(SceneChangedEvent evt)
        {
            // 只在关卡或大厅场景重置（排除主菜单、商店、过渡场景等）
            if (evt.Type != SceneType.Level && evt.Type != SceneType.Lobby)
                return;
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            MonsterCombatGroup.Logger.LogInfo($"场景切换：{evt.SceneName}，重置状态。");
            foreach (ICombatHandler handler in _handlers)
            {
                if (handler is IResettable resettable)
                    resettable.ResetState();
            }
        }

        private void OnJoinedRoom()
        {
            foreach (ICombatHandler handler in _handlers)
            {
                if (handler is IResettable resettable)
                    resettable.ResetState();
            }
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
                _handlers.Clear();
                _isInitialized = false;
            }
        }
    }

    public interface IResettable
    {
        void ResetState();
    }
}