using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using MonsterCombatGroup.State;
using UnityEngine;

namespace MonsterCombatGroup.Handler
{
    public class LeaderAbilityHandler : ICombatHandler, IResettable
    {
        private readonly bool _enabled;
        private readonly float _immunityDuration;
        private readonly bool _enableBatteryDrain;

        private readonly IEnemyBridge _enemyBridge;
        private readonly IPlayerBridge _playerBridge;
        private readonly IItemBridge _itemBridge;
        private readonly IEnemyModifierBridge? _modifier;

        private readonly Dictionary<int, EnemyParent> _enemyCache = new Dictionary<int, EnemyParent>();
        private float _nextCacheRefreshTime = 0f;
        private const float CACHE_REFRESH_INTERVAL = 0.5f;

        private bool _subscribed = false;

        public LeaderAbilityHandler()
        {
            MonsterCombatGroupConfig cfg = MonsterCombatGroupConfig.Instance;
            _enabled = cfg.EnableLeaderMechanic.Value;
            _immunityDuration = cfg.StunImmunityDuration.Value;
            _enableBatteryDrain = cfg.EnableBatteryDrainOnLeaderHurt.Value;

            _enemyBridge = BridgeLocator.Enemy;
            _playerBridge = BridgeLocator.Player;
            _itemBridge = BridgeLocator.Item;
            _modifier = BridgeLocator.Get<IEnemyModifierBridge>();

            if (_modifier == null)
                MonsterCombatGroup.Logger.LogWarning("IEnemyModifierBridge 未注册，眩晕免疫功能不可用。");

            EventBus.Subscribe<EnemyHurtEvent>(OnEnemyHurt);
            _subscribed = true;
            MonsterCombatGroup.Logger.LogInfo("LeaderAbilityHandler 已初始化。");
        }

        public void Process(float deltaTime)
        {
            // 定期刷新敌人缓存（使用与 Service 相同的 TickInterval）
            if (Time.time >= _nextCacheRefreshTime)
            {
                _nextCacheRefreshTime = Time.time + CACHE_REFRESH_INTERVAL;
                RefreshEnemyCache();
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

        private void OnEnemyHurt(EnemyHurtEvent evt)
        {
            if (!_enabled) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            int id = evt.InstanceId;

            // 从缓存中获取 EnemyParent，避免遍历全部敌人
            if (!_enemyCache.TryGetValue(id, out EnemyParent? enemy))
                return;

            // 1. 全局抗性：若领队在指挥状态，所有怪物受击获得抗性
            if (LeaderState.IsCommanding && _modifier != null)
            {
                float globalImmunity = MonsterCombatGroupConfig.Instance.GlobalStunImmunityDuration.Value;
                if (globalImmunity > 0f)
                {
                    _modifier.ApplyStunImmunity(enemy, globalImmunity);
                }
            }

            // 2. 领队专属
            if (LeaderState.IsLeader(id))
            {
                if (_enableBatteryDrain)
                    HandleLeaderHurt(enemy);

                if (_modifier != null)
                {
                    if (_immunityDuration > 0f)
                        _modifier.ApplyStunImmunity(enemy, _immunityDuration);

                    float extraPerGuard = MonsterCombatGroupConfig.Instance.LeaderExtraStunImmunityPerGuard.Value;
                    int guardCount = LeaderState.GuardCount;
                    float extraImmunity = extraPerGuard * guardCount;
                    if (extraImmunity > 0f)
                    {
                        _modifier.ApplyStunImmunity(enemy, extraImmunity);
                    }
                }
            }
        }

        private void HandleLeaderHurt(EnemyParent leader)
        {
            EnemyHealth? health = leader.Enemy?.Health;
            if (health == null) return;

            float maxHealth = health.health;
            float currentHealth = health.healthCurrent;
            if (maxHealth <= 0f) return;

            float lossRatio = 1f - (currentHealth / maxHealth);
            if (lossRatio <= 0f) return;

            Enemy? enemyComp = leader.Enemy;
            if (enemyComp == null) return;

            PlayerAvatar? targetPlayer = enemyComp.TargetPlayerAvatar;
            if (targetPlayer == null || targetPlayer.isDisabled) return;

            // 扣除率减半
            float adjustedLossRatio = lossRatio * 0.5f;
            ItemBattery? battery = _itemBridge.GetHeldItemBattery(targetPlayer);
            if (battery == null) return;

            float currentLife = battery.batteryLife;
            float newLife = Mathf.Max(0f, currentLife * (1f - adjustedLossRatio));
            int newLifePercent = Mathf.RoundToInt(newLife);
            if (newLifePercent < 0) newLifePercent = 0;
            battery.SetBatteryLife(newLifePercent);
        }

        public void ResetState()
        {
            _enemyCache.Clear();
            _nextCacheRefreshTime = 0f;
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                EventBus.Unsubscribe<EnemyHurtEvent>(OnEnemyHurt);
                _subscribed = false;
            }
            _enemyCache.Clear();
        }
    }
}