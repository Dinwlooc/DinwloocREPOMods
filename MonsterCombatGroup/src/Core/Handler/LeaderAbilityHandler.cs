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
        // ---- 配置 ----
        private readonly bool _enabled;
        private readonly bool _enableBatteryDrain;

        // ---- 桥接 ----
        private readonly IEnemyBridge _enemyBridge;
        private readonly IPlayerBridge _playerBridge;
        private readonly IItemBridge _itemBridge;
        private readonly IEnemyModifierBridge? _modifier;

        // ---- 缓存 ----
        private readonly Dictionary<int, EnemyParent> _enemyCache = new Dictionary<int, EnemyParent>();
        private float _nextCacheRefreshTime = 0f;
        private const float CACHE_REFRESH_INTERVAL = 0.5f;

        public LeaderAbilityHandler()
        {
            MonsterCombatGroupConfig cfg = MonsterCombatGroupConfig.Instance;
            _enabled = cfg.EnableLeaderMechanic.Value;
            _enableBatteryDrain = cfg.EnableBatteryDrainOnLeaderHurt.Value;

            _enemyBridge = BridgeLocator.Enemy;
            _playerBridge = BridgeLocator.Player;
            _itemBridge = BridgeLocator.Item;
            _modifier = BridgeLocator.Get<IEnemyModifierBridge>();

            if (_modifier == null)
                MonsterCombatGroup.Logger.LogWarning("IEnemyModifierBridge 未注册，部分功能降级。");

            MonsterCombatGroup.Logger.LogInfo("LeaderAbilityHandler 已初始化（仅处理领队受击）。");
        }

        public void Process(float deltaTime)
        {
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

        /// <summary>
        /// 处理领队受击（由分发器调用）。
        /// </summary>
        public void HandleHurt(int instanceId, int moonLevel)
        {
            if (!_enabled) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            if (!_enemyCache.TryGetValue(instanceId, out EnemyParent? enemy))
                return;

            // ---- 月相二：领队强制起身并唤醒所有守卫 ----
            if (moonLevel >= 2)
            {
                ResistanceManager.ForceResetStun(enemy, _modifier);
                foreach (int guardId in LeaderState.GuardInstanceIds)
                {
                    if (_enemyCache.TryGetValue(guardId, out EnemyParent? guard))
                    {
                        ResistanceManager.ForceResetStun(guard, _modifier);
                    }
                }
                // 月相二领队不设置任何抵抗，不记录
            }
            else // 月相一
            {
                MoonPhaseResistConfig.ResistParams p = MoonPhaseResistConfig.GetLeaderParams(moonLevel);
                if (p.NormalDuration > 0f || p.StrongDuration > 0f)
                {
                    ResistanceManager.ProcessResist(enemy, instanceId, p.StrongDuration, p.NormalDuration, p.Cooldown, _modifier);
                }
            }

            // ---- 电量扣除（所有月相） ----
            if (_enableBatteryDrain)
            {
                HandleLeaderHurt(enemy, moonLevel);
            }
        }

        private void HandleLeaderHurt(EnemyParent leader, int moonLevel)
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

            float ratio = lossRatio * 0.5f;
            if (moonLevel >= 2)
            {
                ratio = Mathf.Max(0.1f, ratio);
            }

            ItemBattery? battery = _itemBridge.GetHeldItemBattery(targetPlayer);
            if (battery == null) return;

            float currentLife = battery.batteryLife;
            float newLife = Mathf.Max(0f, currentLife * (1f - ratio));
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
            _enemyCache.Clear();
        }
    }
}