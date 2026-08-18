using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using MonsterCombatGroup.State;
using UnityEngine;

namespace MonsterCombatGroup.Handler
{
    public class LeaderElectionHandler : ICombatHandler, IResettable
    {
        private readonly bool _enabled;
        private readonly float _electionCooldown;
        private readonly float _leaderHealthMult;
        private readonly float _guardHealthMult;

        private readonly IEnemyBridge _enemyBridge;
        private readonly IGameStateBridge _gameState;
        private readonly IEnemyModifierBridge _modifier;
        private readonly IEnemyHealthBridge _healthBridge;

        private readonly Dictionary<int, int> _baseHealthCache = new Dictionary<int, int>();
        private float _lastElectionTime = -float.MaxValue;
        private bool _subscribed = false;

        public LeaderElectionHandler()
        {
            MonsterCombatGroupConfig config = MonsterCombatGroupConfig.Instance;
            _enabled = config.EnableLeaderMechanic.Value;
            _electionCooldown = config.ElectionCooldownSeconds.Value;
            _leaderHealthMult = config.LeaderHealthMultiplier.Value;
            _guardHealthMult = config.GuardHealthMultiplier.Value;

            _enemyBridge = BridgeLocator.Enemy;
            _gameState = BridgeLocator.GameState;
            _modifier = BridgeLocator.Get<IEnemyModifierBridge>();
            _healthBridge = BridgeLocator.Get<IEnemyHealthBridge>();

            if (_modifier == null)
                MonsterCombatGroup.Logger.LogWarning("IEnemyModifierBridge 未注册，属性修改不可用。");
            if (_healthBridge == null)
                MonsterCombatGroup.Logger.LogWarning("IEnemyHealthBridge 未注册，基础血量记录可能不完整。");

            EventBus.Subscribe<EnemySpawnedEvent>(OnEnemySpawned);
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Subscribe<EnemyDespawnEvent>(OnEnemyDespawn);
            _subscribed = true;

            MonsterCombatGroup.Logger.LogInfo($"LeaderElectionHandler 已初始化，冷却 {_electionCooldown}s。");
        }

        public void Process(float deltaTime)
        {
            if (!_enabled)
                return;

            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            if (_gameState.IsMainMenu() || !_gameState.IsLevelLoaded())
                return;

            if (!LeaderState.HasLeader)
                TryElectLeader();
        }

        private void TryElectLeader()
        {
            int moonLevel = BridgeLocator.Moon.GetCurrentMoonLevel();
            if (moonLevel < 1)
                return;

            if (!LeaderState.IsCooldownElapsed(_electionCooldown))
                return;

            float timeSinceLastElection = Time.time - _lastElectionTime;
            if (timeSinceLastElection < _electionCooldown)
            {
                MonsterCombatGroup.Logger.LogDebug($"选举冷却中（全局），剩余 {_electionCooldown - timeSinceLastElection:F1}s");
                return;
            }

            // 获取所有有效怪物（使用缓存服务）
            IReadOnlyList<EnemyParent> allEnemies = EnemyCacheService.GetAllEnemies();
            List<EnemyParent> valid = new List<EnemyParent>();
            foreach (EnemyParent enemy in allEnemies)
            {
                if (_enemyBridge.IsEnemyValid(enemy))
                {
                    int id = enemy.GetInstanceID();
                    if (!LeaderState.IsLeader(id) && !LeaderState.IsGuard(id))
                        valid.Add(enemy);
                }
            }

            if (valid.Count < 4)
                return;

            // ---- 选举前恢复旧角色血量 ----
            RestoreCurrentRolesToBaseHealth();

            // 随机选择领队和守卫
            int leaderIndex = Random.Range(0, valid.Count);
            EnemyParent leader = valid[leaderIndex];
            valid.RemoveAt(leaderIndex);

            if (valid.Count < 2)
                return;

            int guard1Index = Random.Range(0, valid.Count);
            EnemyParent guard1 = valid[guard1Index];
            valid.RemoveAt(guard1Index);

            int guard2Index = Random.Range(0, valid.Count);
            EnemyParent guard2 = valid[guard2Index];

            // 清空旧角色状态（已经在 RestoreCurrentRolesToBaseHealth 中恢复血量，这里只清状态）
            LeaderState.ClearAll();

            int leaderId = leader.GetInstanceID();
            int guard1Id = guard1.GetInstanceID();
            int guard2Id = guard2.GetInstanceID();

            LeaderState.SetLeader(leaderId);
            LeaderState.AddGuard(guard1Id);
            LeaderState.AddGuard(guard2Id);

            // 获取基础血量（若未记录，则从当前最大血量获取并记录）
            int baseLeader = GetBaseHealth(leader);
            int baseGuard1 = GetBaseHealth(guard1);
            int baseGuard2 = GetBaseHealth(guard2);

            int newMaxLeader = (int)(baseLeader * _leaderHealthMult);
            int newMaxGuard1 = (int)(baseGuard1 * _guardHealthMult);
            int newMaxGuard2 = (int)(baseGuard2 * _guardHealthMult);

            MonsterSyncManager.UpdateMonsterMaxHealth(leader, newMaxLeader);
            MonsterSyncManager.UpdateMonsterMaxHealth(guard1, newMaxGuard1);
            MonsterSyncManager.UpdateMonsterMaxHealth(guard2, newMaxGuard2);

            _lastElectionTime = Time.time;
            MonsterCombatGroup.Logger.LogInfo($"选举领队 {leaderId}，护卫 {guard1Id}, {guard2Id}，冷却 {_electionCooldown}s");
        }

        /// <summary>
        /// 恢复当前所有角色（领队和守卫）的基础血量。
        /// </summary>
        private void RestoreCurrentRolesToBaseHealth()
        {
            if (LeaderState.HasLeader)
            {
                int leaderId = LeaderState.LeaderInstanceId;
                EnemyParent leader = EnemyCacheService.GetEnemyById(leaderId);
                if (leader != null && _enemyBridge.IsEnemyValid(leader))
                    RevertToBaseHealth(leaderId);
            }

            foreach (int guardId in LeaderState.GuardInstanceIds)
            {
                EnemyParent guard = EnemyCacheService.GetEnemyById(guardId);
                if (guard != null && _enemyBridge.IsEnemyValid(guard))
                    RevertToBaseHealth(guardId);
            }
        }

        private int GetBaseHealth(EnemyParent enemy)
        {
            int id = enemy.GetInstanceID();
            if (_baseHealthCache.TryGetValue(id, out int cached))
                return cached;

            // 若未记录，则使用当前最大血量作为基础（但不应发生，因为生成时会记录）
            int currentMax = _healthBridge != null ? _healthBridge.GetMaxHealth(enemy) : enemy.Enemy.Health.health;
            _baseHealthCache[id] = currentMax;
            return currentMax;
        }

        private void RevertToBaseHealth(int instanceId)
        {
            if (!_baseHealthCache.TryGetValue(instanceId, out int baseHealth))
                return;

            if (_modifier == null)
                return;

            EnemyParent enemy = EnemyCacheService.GetEnemyById(instanceId);
            if (enemy != null && _enemyBridge.IsEnemyValid(enemy))
            {
                _modifier.SetHealth(enemy, baseHealth);
                MonsterCombatGroup.Logger.LogDebug($"恢复怪物 {instanceId} 血量至基础值 {baseHealth}");
            }
        }

        private void OnEnemySpawned(EnemySpawnedEvent evt)
        {
            // 记录该怪物的初始最大血量（作为基础）
            int instanceId = evt.InstanceId;
            EnemyParent enemy = EnemyCacheService.GetEnemyById(instanceId);
            if (enemy == null)
                return;

            int maxHealth = _healthBridge != null ? _healthBridge.GetMaxHealth(enemy) : enemy.Enemy.Health.health;
            _baseHealthCache[instanceId] = maxHealth;

            TryElectLeader();
        }

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            int instanceId = evt.InstanceId;
            // 清除角色状态（不恢复血量，因为怪物已死亡）
            LeaderState.ClearRole(instanceId);
            // 移除基础血量缓存，避免残留
            _baseHealthCache.Remove(instanceId);
            TryElectLeader();
        }

        private void OnEnemyDespawn(EnemyDespawnEvent evt)
        {
            int instanceId = evt.InstanceId;
            LeaderState.ClearRole(instanceId);
            _baseHealthCache.Remove(instanceId);
            TryElectLeader();
        }

        public void ResetState()
        {
            // 恢复所有存活怪物的基础血量
            if (_modifier != null)
            {
                IReadOnlyList<EnemyParent> allEnemies = EnemyCacheService.GetAllEnemies();
                if (allEnemies != null)
                {
                    foreach (EnemyParent enemy in allEnemies)
                    {
                        if (enemy == null)
                            continue;
                        int id = enemy.GetInstanceID();
                        if (_baseHealthCache.TryGetValue(id, out int baseHealth))
                        {
                            _modifier.SetHealth(enemy, baseHealth);
                            MonsterCombatGroup.Logger.LogDebug($"关卡重载：恢复怪物 {id} 血量至 {baseHealth}");
                        }
                    }
                }
            }

            _baseHealthCache.Clear();
            LeaderState.ClearAll();
            _lastElectionTime = -float.MaxValue;
            MonsterCombatGroup.Logger.LogInfo("LeaderElectionHandler 状态重置，所有怪物血量已恢复。");
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                EventBus.Unsubscribe<EnemySpawnedEvent>(OnEnemySpawned);
                EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
                EventBus.Unsubscribe<EnemyDespawnEvent>(OnEnemyDespawn);
                _subscribed = false;
            }

            _baseHealthCache.Clear();
            LeaderState.ClearAll();
        }
    }
}