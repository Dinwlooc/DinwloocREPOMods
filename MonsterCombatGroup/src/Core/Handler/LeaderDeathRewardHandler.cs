using System.Collections.Generic;
using System.Reflection;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using Dinwlooc.Common.Reflection;
using MonsterCombatGroup.State;
using UnityEngine;

namespace MonsterCombatGroup.Handler
{
    /// <summary>
    /// 领队死亡奖励处理器。
    /// 任何怪物死亡时，若领队不存在，则启动奖励保护（无敌）。
    /// 保护持续 RewardDuration 秒，期间通过手持检测维护新生成的 Valuable。
    /// </summary>
    public class LeaderDeathRewardHandler : ICombatHandler, IResettable
    {
        // ---- 常量 ----
        private const float HANDHELD_CHECK_INTERVAL = 0.5f;

        // ---- 配置 ----
        private readonly bool _enabled;
        private readonly float _rewardDuration;

        // ---- 桥接 ----
        private readonly IEnemyBridge _enemyBridge;
        private readonly IPlayerBridge _playerBridge;
        private readonly IItemBridge _itemBridge;
        private readonly IGameStateBridge _gameState;

        // ---- 状态 ----
        private bool _rewardActive = false;
        private float _rewardEndTime = 0f;
        private float _nextHandheldCheckTime = 0f;

        // ---- 反射 ----
        private static FieldInfo? _indestructibleTimerField;
        private static bool _reflectionCached = false;

        private bool _subscribed = false;

        public LeaderDeathRewardHandler()
        {
            MonsterCombatGroupConfig config = MonsterCombatGroupConfig.Instance;
            _enabled = config.EnableLeaderDeathReward.Value;
            _rewardDuration = config.RewardDuration.Value;

            _enemyBridge = BridgeLocator.Enemy;
            _playerBridge = BridgeLocator.Player;
            _itemBridge = BridgeLocator.Item;
            _gameState = BridgeLocator.GameState;

            CacheReflection();

            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            _subscribed = true;

            MonsterCombatGroup.Logger.LogInfo($"LeaderDeathRewardHandler 已初始化，奖励时长 {_rewardDuration}s。");
        }

        private static void CacheReflection()
        {
            if (_reflectionCached) return;
            _indestructibleTimerField = ReflectionCache.GetField(
                typeof(EnemyValuable),
                "indestructibleTimer",
                BindingFlags.NonPublic | BindingFlags.Instance);
            _reflectionCached = true;
            if (_indestructibleTimerField == null)
                MonsterCombatGroup.Logger.LogWarning("无法获取 EnemyValuable.indestructibleTimer 字段，奖励功能不可用。");
        }

        public void Process(float deltaTime)
        {
            if (!_enabled) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (_gameState.IsMainMenu() || !_gameState.IsLevelLoaded()) return;

            if (_rewardActive && Time.time >= _rewardEndTime)
            {
                _rewardActive = false;
                MonsterCombatGroup.Logger.LogInfo("领队死亡奖励已过期。");
                return;
            }

            if (!_rewardActive) return;

            if (Time.time >= _nextHandheldCheckTime)
            {
                _nextHandheldCheckTime = Time.time + HANDHELD_CHECK_INTERVAL;
                CheckHandheldValuables();
            }
        }

        private void CheckHandheldValuables()
        {
            if (_indestructibleTimerField == null) return;

            List<PlayerAvatar> players = _playerBridge.GetAllPlayers();
            foreach (PlayerAvatar player in players)
            {
                if (player == null || player.isDisabled) continue;

                EnemyValuable? valuable = _itemBridge.GetHeldValuable(player);
                if (valuable == null) continue;

                try
                {
                    float remaining = _rewardEndTime - Time.time;
                    float timerValue = Mathf.Max(remaining, 5f);
                    _indestructibleTimerField.SetValue(valuable, timerValue);
                    PhysGrabObjectImpactDetector detector = valuable.GetComponentInChildren<PhysGrabObjectImpactDetector>();
                    if (detector != null)
                        detector.destroyDisable = true;
                }
                catch { /* 忽略单个错误 */ }
            }
        }

        private void ActivateReward()
        {
            if (_indestructibleTimerField == null) return;

            EnemyValuable[] allValuables = Object.FindObjectsByType<EnemyValuable>(FindObjectsSortMode.None);
            float baseTimer = _rewardDuration + 5f;
            foreach (EnemyValuable valuable in allValuables)
            {
                if (valuable == null) continue;
                try
                {
                    _indestructibleTimerField.SetValue(valuable, baseTimer);
                    PhysGrabObjectImpactDetector detector = valuable.GetComponentInChildren<PhysGrabObjectImpactDetector>();
                    if (detector != null)
                        detector.destroyDisable = true;
                }
                catch { /* 忽略 */ }
            }
            MonsterCombatGroup.Logger.LogInfo($"领队死亡奖励激活，立即保护 {allValuables.Length} 个 Valuable");

            _rewardActive = true;
            _rewardEndTime = Time.time + _rewardDuration;
            _nextHandheldCheckTime = Time.time + HANDHELD_CHECK_INTERVAL;
            MonsterCombatGroup.Logger.LogInfo($"领队死亡奖励启动，持续 {_rewardDuration}s");
        }

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            if (!_enabled) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            // 检查领队是否还存在
            if (LeaderState.HasLeader)
                return;

            if (_rewardActive)
                return;

            // 激活奖励（保护 Valuable）
            ActivateReward();
        }

        public void ResetState()
        {
            _rewardActive = false;
            _rewardEndTime = 0f;
            _nextHandheldCheckTime = 0f;
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
                _subscribed = false;
            }
            ResetState();
        }
    }
}