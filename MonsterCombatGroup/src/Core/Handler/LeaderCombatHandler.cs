using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using MonsterCombatGroup.State;
using UnityEngine;

namespace MonsterCombatGroup.Handler
{
    /// <summary>
    /// 领队指挥状态管理：锁定玩家、定时召集攻击、追击次数管理。
    /// </summary>
    public class LeaderCombatHandler : ICombatHandler, IResettable
    {
        private readonly bool _enabled;
        private readonly float _commandInterval;
        private readonly int _maxAttackCount;

        private readonly IEnemyBridge _enemyBridge;
        private readonly IEnemyModifierBridge? _modifier;
        private readonly IGameStateBridge _gameState;

        private PlayerAvatar? _lockedTarget;
        private int _attackCountRemaining;
        private float _nextCommandTime;
        private bool _subscribed = false;

        public LeaderCombatHandler()
        {
            var cfg = MonsterCombatGroupConfig.Instance;
            _enabled = cfg.EnableLeaderMechanic.Value;
            _commandInterval = cfg.CommandInterval.Value;
            _maxAttackCount = cfg.CommandAttackCount.Value;

            _enemyBridge = BridgeLocator.Enemy;
            _modifier = BridgeLocator.Get<IEnemyModifierBridge>();
            _gameState = BridgeLocator.GameState;

            if (_modifier == null)
                MonsterCombatGroup.Logger.LogWarning("IEnemyModifierBridge 未注册，指挥攻击功能降级。");

            EventBus.Subscribe<EnemyVisionEvent>(OnEnemyVision);
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            _subscribed = true;
            MonsterCombatGroup.Logger.LogInfo("LeaderCombatHandler 已初始化。");
        }

        public void Process(float deltaTime)
        {
            if (!_enabled) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (_gameState.IsMainMenu() || !_gameState.IsLevelLoaded()) return;

            // 检查领队是否存在
            if (!LeaderState.HasLeader)
            {
                if (LeaderState.IsCommanding) EndCommand();
                return;
            }

            EnemyParent? leaderParent = GetEnemyParentById(LeaderState.LeaderInstanceId);
            if (leaderParent == null || !_enemyBridge.IsEnemyValid(leaderParent))
            {
                if (LeaderState.IsCommanding) EndCommand();
                return;
            }

            // 如果锁定目标死亡或无效，结束指挥
            if (_lockedTarget != null && _lockedTarget.isDisabled)
            {
                EndCommand();
                return;
            }

            // 检查攻击次数
            if (LeaderState.IsCommanding && _attackCountRemaining <= 0)
            {
                EndCommand();
                return;
            }

            // 定时召集攻击
            if (LeaderState.IsCommanding && _attackCountRemaining > 0)
            {
                if (Time.time >= _nextCommandTime)
                {
                    _nextCommandTime = Time.time + _commandInterval;
                    ExecuteCommand();
                }
            }
        }

        private void OnEnemyVision(EnemyVisionEvent evt)
        {
            if (!_enabled) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            // 仅领队的视觉事件触发锁定
            int id = evt.InstanceId;
            if (!LeaderState.IsLeader(id)) return;

            // 如果已在指挥状态且有目标，不重新锁定
            if (LeaderState.IsCommanding && _lockedTarget != null) return;

            EnemyParent? leader = GetEnemyParentById(id);
            if (leader == null) return;

            Enemy? enemy = leader.Enemy;
            if (enemy == null || enemy.Vision == null) return;
            PlayerAvatar? target = enemy.Vision.onVisionTriggeredPlayer;
            if (target == null || target.isDisabled) return;

            LockTarget(target);
        }

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            // 如果领队死亡，结束指挥
            int id = evt.InstanceId;
            if (LeaderState.IsLeader(id))
            {
                if (LeaderState.IsCommanding) EndCommand();
            }
        }

        private void LockTarget(PlayerAvatar target)
        {
            if (_lockedTarget == target && LeaderState.IsCommanding) return;

            _lockedTarget = target;
            _attackCountRemaining = _maxAttackCount;
            LeaderState.SetCommanding(true);
            _nextCommandTime = Time.time + _commandInterval;
            MonsterCombatGroup.Logger.LogInfo($"领队锁定玩家 {target.name}，指挥开始，剩余攻击次数 {_attackCountRemaining}");
        }

        private void EndCommand()
        {
            LeaderState.SetCommanding(false);
            _lockedTarget = null;
            _attackCountRemaining = 0;
            MonsterCombatGroup.Logger.LogInfo("领队指挥结束");
        }

        private void ExecuteCommand()
        {
            if (_lockedTarget == null || _lockedTarget.isDisabled)
            {
                EndCommand();
                return;
            }

            // 选择一名普通怪物（非领队、非护卫）
            EnemyParent? selected = SelectRandomMonster();
            List<EnemyParent> attackers = new List<EnemyParent>();
            if (selected != null)
                attackers.Add(selected);

            // 加入所有存活护卫
            foreach (int guardId in LeaderState.GuardInstanceIds)
            {
                EnemyParent? guard = GetEnemyParentById(guardId);
                if (guard != null && _enemyBridge.IsEnemyValid(guard))
                    attackers.Add(guard);
            }

            if (attackers.Count == 0)
            {
                _attackCountRemaining--;
                return;
            }

            // 命令所有攻击者追击目标
            foreach (EnemyParent attacker in attackers)
            {
                if (_modifier != null)
                    _modifier.ForceChase(attacker, _lockedTarget);
                else
                    attacker.Enemy?.SetChaseTarget(_lockedTarget);
            }

            _attackCountRemaining--;
            MonsterCombatGroup.Logger.LogDebug($"指挥攻击：{attackers.Count} 个怪物追击 {_lockedTarget.name}，剩余 {_attackCountRemaining} 次");
        }

        private EnemyParent? SelectRandomMonster()
        {
            List<EnemyParent> candidates = new List<EnemyParent>();
            IReadOnlyList<EnemyParent> all = _enemyBridge.GetAllEnemies();
            if (all == null) return null;

            foreach (EnemyParent ep in all)
            {
                if (!_enemyBridge.IsEnemyValid(ep)) continue;
                int id = ep.GetInstanceID();
                if (id == LeaderState.LeaderInstanceId) continue;
                if (LeaderState.IsGuard(id)) continue;
                candidates.Add(ep);
            }

            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
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
            EndCommand();
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                EventBus.Unsubscribe<EnemyVisionEvent>(OnEnemyVision);
                EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
                _subscribed = false;
            }
            EndCommand();
        }
    }
}