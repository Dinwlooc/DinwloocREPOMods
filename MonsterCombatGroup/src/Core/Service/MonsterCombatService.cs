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

            // 订阅场景切换事件
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

            // 清空同步缓存（房主）
            MonsterSyncManager.ClearState();

            // 所有客户端（包括房主）在进入关卡时初始化同步缓存
            // 这会触发 CacheManager.GetOrCreateSyncCache，自动处理网络就绪和全量同步
            if (evt.Type == SceneType.Level)
            {
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

            // 重置同步管理器
            MonsterSyncManager.Reset();
        }
    }

    public interface IResettable
    {
        void ResetState();
    }
}