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
        private readonly float _immunityDuration;
        private readonly bool _enableStunRecovery;

        private readonly IEnemyBridge _enemyBridge;
        private readonly IEnemyModifierBridge? _modifier;
        private readonly IGameStateBridge _gameState;
        private bool _subscribed = false;

        public GuardAbilityHandler()
        {
            var cfg = MonsterCombatGroupConfig.Instance;
            _enabled = cfg.EnableLeaderMechanic.Value;
            _immunityDuration = cfg.StunImmunityDuration.Value;
            _enableStunRecovery = cfg.EnableGuardStunRecoveryOnHurt.Value;

            _enemyBridge = BridgeLocator.Enemy;
            _modifier = BridgeLocator.Get<IEnemyModifierBridge>();
            _gameState = BridgeLocator.GameState;

            if (_modifier == null)
                MonsterCombatGroup.Logger.LogWarning("IEnemyModifierBridge 未注册，护卫能力降级。");

            EventBus.Subscribe<EnemyHurtEvent>(OnEnemyHurt);
            _subscribed = true;
            MonsterCombatGroup.Logger.LogInfo("GuardAbilityHandler 已初始化。");
        }

        public void Process(float deltaTime)
        {
            if (!_enabled) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (_gameState.IsMainMenu() || !_gameState.IsLevelLoaded()) return;

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

        private void OnEnemyHurt(EnemyHurtEvent evt)
        {
            if (!_enabled) return;
            if (!_enableStunRecovery) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            int id = evt.InstanceId;
            if (!LeaderState.IsGuard(id)) return;

            if (_modifier == null) return;

            EnemyParent? guard = GetEnemyParentById(id);
            if (guard == null) return;

            _modifier.ResetStun(guard);
            if (_immunityDuration > 0f)
                _modifier.ApplyStunImmunity(guard, _immunityDuration);
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

        public void ResetState() { }

        public void Dispose()
        {
            if (_subscribed)
            {
                EventBus.Unsubscribe<EnemyHurtEvent>(OnEnemyHurt);
                _subscribed = false;
            }
        }
    }
}