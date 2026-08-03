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
        private readonly IEnemyModifierBridge? _modifier;

        private readonly Dictionary<int, int> _baseHealthCache = new Dictionary<int, int>();
        private bool _subscribed = false;

        // 上次选举成功的时间戳（用于全局冷却）
        private float _lastElectionTime = -float.MaxValue;

        public LeaderElectionHandler()
        {
            MonsterCombatGroupConfig cfg = MonsterCombatGroupConfig.Instance;
            _enabled = cfg.EnableLeaderMechanic.Value;
            _electionCooldown = cfg.ElectionCooldownSeconds.Value;
            _leaderHealthMult = cfg.LeaderHealthMultiplier.Value;
            _guardHealthMult = cfg.GuardHealthMultiplier.Value;

            _enemyBridge = BridgeLocator.Enemy;
            _gameState = BridgeLocator.GameState;
            _modifier = BridgeLocator.Get<IEnemyModifierBridge>();

            if (_modifier == null)
                MonsterCombatGroup.Logger.LogWarning("IEnemyModifierBridge 未注册，属性修改不可用。");

            EventBus.Subscribe<EnemySpawnedEvent>(OnEnemySpawned);
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Subscribe<EnemyDespawnEvent>(OnEnemyDespawn);
            _subscribed = true;

            MonsterCombatGroup.Logger.LogInfo($"LeaderElectionHandler 已初始化，冷却 {_electionCooldown}s。");
        }

        public void Process(float deltaTime)
        {
            if (!_enabled) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (_gameState.IsMainMenu() || !_gameState.IsLevelLoaded()) return;

            // 只有在没有领队时才尝试选举
            if (!LeaderState.HasLeader)
                TryElectLeader();
        }

        private void TryElectLeader()
        {
            // 检查领队死亡冷却（LeaderState 管理）
            if (!LeaderState.IsCooldownElapsed(_electionCooldown))
                return;

            // 检查全局选举冷却（防止连续选举）
            float timeSinceLastElection = Time.time - _lastElectionTime;
            if (timeSinceLastElection < _electionCooldown)
            {
                MonsterCombatGroup.Logger.LogDebug($"选举冷却中（全局），剩余 {_electionCooldown - timeSinceLastElection:F1}s");
                return;
            }

            List<EnemyParent> valid = GetValidMonsters();
            if (valid.Count < 4)
                return;

            int leaderIdx = Random.Range(0, valid.Count);
            EnemyParent leader = valid[leaderIdx];
            valid.RemoveAt(leaderIdx);

            if (valid.Count < 2) return;

            int guard1Idx = Random.Range(0, valid.Count);
            EnemyParent guard1 = valid[guard1Idx];
            valid.RemoveAt(guard1Idx);

            int guard2Idx = Random.Range(0, valid.Count);
            EnemyParent guard2 = valid[guard2Idx];

            LeaderState.ClearAll();

            int leaderId = leader.GetInstanceID();
            int guard1Id = guard1.GetInstanceID();
            int guard2Id = guard2.GetInstanceID();

            LeaderState.SetLeader(leaderId);
            LeaderState.AddGuard(guard1Id);
            LeaderState.AddGuard(guard2Id);

            // 计算新最大血量
            int baseHealthLeader = GetBaseHealth(leader);
            int baseHealthGuard1 = GetBaseHealth(guard1);
            int baseHealthGuard2 = GetBaseHealth(guard2);

            int newMaxLeader = (int)(baseHealthLeader * _leaderHealthMult);
            int newMaxGuard1 = (int)(baseHealthGuard1 * _guardHealthMult);
            int newMaxGuard2 = (int)(baseHealthGuard2 * _guardHealthMult);

            // 同步最大血量到所有客户端（房主调用）
            MonsterSyncManager.UpdateMonsterMaxHealth(leader, newMaxLeader);
            MonsterSyncManager.UpdateMonsterMaxHealth(guard1, newMaxGuard1);
            MonsterSyncManager.UpdateMonsterMaxHealth(guard2, newMaxGuard2);

            // 设置当前血量（满血），由房主本地执行，游戏网络同步会传达当前血量
            if (_modifier != null)
            {
                _modifier.SetHealth(leader, newMaxLeader);
                _modifier.SetHealth(guard1, newMaxGuard1);
                _modifier.SetHealth(guard2, newMaxGuard2);
            }

            // 记录选举成功时间
            _lastElectionTime = Time.time;
            MonsterCombatGroup.Logger.LogInfo($"选举领队 {leaderId}，护卫 {guard1Id}, {guard2Id}，冷却 {_electionCooldown}s");
        }

        private int GetBaseHealth(EnemyParent enemy)
        {
            int id = enemy.GetInstanceID();
            if (_baseHealthCache.TryGetValue(id, out int cached))
                return cached;

            int currentHealth = enemy.Enemy?.Health?.health ?? 100;
            _baseHealthCache[id] = currentHealth;
            return currentHealth;
        }

        private void RevertToBaseHealth(int instanceId)
        {
            if (!_baseHealthCache.TryGetValue(instanceId, out int baseHealth)) return;
            if (_modifier == null) return;

            EnemyParent? enemy = GetEnemyParentById(instanceId);
            if (enemy != null)
                _modifier.SetHealth(enemy, baseHealth);
        }

        private List<EnemyParent> GetValidMonsters()
        {
            List<EnemyParent> result = new List<EnemyParent>();
            IReadOnlyList<EnemyParent> all = _enemyBridge.GetAllEnemies();
            if (all == null) return result;
            foreach (EnemyParent ep in all)
            {
                if (_enemyBridge.IsEnemyValid(ep))
                {
                    int id = ep.GetInstanceID();
                    if (!LeaderState.IsLeader(id) && !LeaderState.IsGuard(id))
                        result.Add(ep);
                }
            }
            return result;
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

        private void OnEnemySpawned(EnemySpawnedEvent evt) => TryElectLeader();

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            int id = evt.InstanceId;
            RevertToBaseHealth(id);
            LeaderState.ClearRole(id);
            TryElectLeader();
        }

        private void OnEnemyDespawn(EnemyDespawnEvent evt)
        {
            int id = evt.InstanceId;
            RevertToBaseHealth(id);
            LeaderState.ClearRole(id);
            TryElectLeader();
        }

        public void ResetState()
        {
            // 恢复所有缓存怪物的血量
            if (_modifier != null)
            {
                IReadOnlyList<EnemyParent> allEnemies = _enemyBridge.GetAllEnemies();
                if (allEnemies != null)
                {
                    foreach (EnemyParent ep in allEnemies)
                    {
                        if (ep == null) continue;
                        int id = ep.GetInstanceID();
                        if (_baseHealthCache.TryGetValue(id, out int baseHealth))
                        {
                            _modifier.SetHealth(ep, baseHealth);
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