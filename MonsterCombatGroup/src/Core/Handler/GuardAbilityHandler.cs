using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using MonsterCombatGroup.State;
using UnityEngine;

namespace MonsterCombatGroup.Handler
{
    public class GuardAbilityHandler : ICombatHandler, IResettable
    {
        private readonly bool _enabled;
        private readonly IEnemyBridge _enemyBridge;
        private readonly IEnemyModifierBridge? _modifier;
        private readonly IGameStateBridge _gameState;

        private readonly Dictionary<int, EnemyParent> _enemyCache = new Dictionary<int, EnemyParent>();
        private float _nextCacheRefreshTime = 0f;
        private const float CACHE_REFRESH_INTERVAL = 0.5f;

        public GuardAbilityHandler()
        {
            MonsterCombatGroupConfig cfg = MonsterCombatGroupConfig.Instance;
            _enabled = cfg.EnableLeaderMechanic.Value;

            _enemyBridge = BridgeLocator.Enemy;
            _modifier = BridgeLocator.Get<IEnemyModifierBridge>();
            _gameState = BridgeLocator.GameState;

            if (_modifier == null)
                MonsterCombatGroup.Logger.LogWarning("IEnemyModifierBridge 未注册，护卫能力降级。");

            MonsterCombatGroup.Logger.LogInfo("GuardAbilityHandler 已初始化（守卫受击逻辑已整合）。");
        }

        public void Process(float deltaTime)
        {
            if (!_enabled) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (_gameState.IsMainMenu() || !_gameState.IsLevelLoaded()) return;

            if (Time.time >= _nextCacheRefreshTime)
            {
                _nextCacheRefreshTime = Time.time + CACHE_REFRESH_INTERVAL;
                RefreshEnemyCache();
            }

            // 协同追击
            if (!LeaderState.HasLeader) return;

            EnemyParent? leaderParent = GetEnemyParentById(LeaderState.LeaderInstanceId);
            if (leaderParent == null || !_enemyBridge.IsEnemyValid(leaderParent))
                return;

            Enemy? leaderEnemy = leaderParent.Enemy;
            if (leaderEnemy == null) return;

            PlayerAvatar? chaseTarget = leaderEnemy.TargetPlayerAvatar;

            foreach (int guardId in LeaderState.GuardInstanceIds)
            {
                EnemyParent? guardParent = GetEnemyParentById(guardId);
                if (guardParent == null || !_enemyBridge.IsEnemyValid(guardParent))
                    continue;

                Enemy? guardEnemy = guardParent.Enemy;
                if (guardEnemy == null) continue;

                if (guardEnemy.TargetPlayerAvatar != chaseTarget)
                {
                    if (chaseTarget != null && !chaseTarget.isDisabled)
                    {
                        if (_modifier != null)
                            _modifier.ForceChase(guardParent, chaseTarget);
                        else
                            guardEnemy.SetChaseTarget(chaseTarget);
                    }
                }
            }
        }

        private void RefreshEnemyCache()
        {
            _enemyCache.Clear();
            IReadOnlyList<EnemyParent> allEnemies = _enemyBridge.GetAllEnemies();
            if (allEnemies == null) return;
            foreach (EnemyParent ep in allEnemies)
            {
                if (ep != null)
                {
                    int id = ep.GetInstanceID();
                    _enemyCache[id] = ep;
                }
            }
        }

        /// <summary>
        /// 处理守卫受击（由分发器调用）。
        /// </summary>
        public void HandleHurt(int instanceId, int moonLevel)
        {
            if (!_enabled) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            if (!_enemyCache.TryGetValue(instanceId, out EnemyParent? enemy))
                return;

            MoonPhaseResistConfig.ResistParams p = MoonPhaseResistConfig.GetGuardParams(moonLevel);
            if (p.NormalDuration <= 0f && p.StrongDuration <= 0f)
                return;

            bool triggered = ResistanceManager.ProcessResist(enemy, instanceId, p.StrongDuration, p.NormalDuration, p.Cooldown, _modifier);

            // 月相二：如果触发了完整效果，刷新另一位守卫的冷却
            if (triggered && moonLevel >= 2)
            {
                int otherGuardId = -1;
                foreach (int guardId in LeaderState.GuardInstanceIds)
                {
                    if (guardId != instanceId)
                    {
                        otherGuardId = guardId;
                        break;
                    }
                }
                if (otherGuardId != -1)
                {
                    ResistanceManager.RefreshCooldownForGuard(otherGuardId, p.Cooldown);
                }
            }
        }

        private EnemyParent? GetEnemyParentById(int instanceId)
        {
            IReadOnlyList<EnemyParent> all = _enemyBridge.GetAllEnemies();
            if (all == null) return null;
            foreach (EnemyParent ep in all)
            {
                if (ep != null && ep.GetInstanceID() == instanceId)
                    return ep;
            }
            return null;
        }

        public void ResetState()
        {
            _enemyCache.Clear();
            _nextCacheRefreshTime = 0f;
        }

        public void Dispose()
        {
            _enemyCache.Clear();
        }
    }
}