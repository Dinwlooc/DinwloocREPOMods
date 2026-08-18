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
        private readonly bool _enableBatteryDrain;

        private readonly IEnemyBridge _enemyBridge;
        private readonly IPlayerBridge _playerBridge;
        private readonly IItemBridge _itemBridge;
        private readonly IEnemyModifierBridge _modifier;
        private readonly IEnemyHealthBridge _healthBridge;

        public LeaderAbilityHandler()
        {
            MonsterCombatGroupConfig config = MonsterCombatGroupConfig.Instance;
            _enabled = config.EnableLeaderMechanic.Value;
            _enableBatteryDrain = config.EnableBatteryDrainOnLeaderHurt.Value;

            _enemyBridge = BridgeLocator.Enemy;
            _playerBridge = BridgeLocator.Player;
            _itemBridge = BridgeLocator.Item;
            _modifier = BridgeLocator.Get<IEnemyModifierBridge>();
            _healthBridge = BridgeLocator.Get<IEnemyHealthBridge>();

            if (_modifier == null)
                MonsterCombatGroup.Logger.LogWarning("IEnemyModifierBridge 未注册，部分功能降级。");
            if (_healthBridge == null)
                MonsterCombatGroup.Logger.LogWarning("IEnemyHealthBridge 未注册，伤害免疫功能不可用。");

            MonsterCombatGroup.Logger.LogInfo("LeaderAbilityHandler 已初始化（仅处理领队受击）。");
        }

        public void Process(float deltaTime)
        {
            // 无需每帧操作，缓存由外部统一刷新
        }

        /// <summary>
        /// 处理领队受击（由分发器调用）。
        /// </summary>
        public void HandleHurt(int instanceId, int moonLevel)
        {
            if (!_enabled)
                return;

            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            EnemyParent enemy = EnemyCacheService.GetEnemyById(instanceId);
            if (enemy == null)
                return;

            // ---- 月相二：领队强制起身并唤醒所有守卫 ----
            if (moonLevel >= 2)
            {
                ResistanceManager.ForceResetStun(enemy, _modifier);
                foreach (int guardId in LeaderState.GuardInstanceIds)
                {
                    EnemyParent guard = EnemyCacheService.GetEnemyById(guardId);
                    if (guard != null)
                    {
                        ResistanceManager.ForceResetStun(guard, _modifier);
                    }
                }
                // 月相二领队不设置任何抵抗，不记录
            }
            else // 月相一
            {
                MoonPhaseResistConfig.ResistParams parameters = MoonPhaseResistConfig.GetLeaderParams(moonLevel);
                if (parameters.NormalDuration > 0f || parameters.StrongDuration > 0f)
                {
                    ResistanceManager.ProcessResist(
                        enemy,
                        instanceId,
                        parameters.StrongDuration,
                        parameters.NormalDuration,
                        parameters.Cooldown,
                        _modifier);
                }
            }

            // ---- 领队伤害免疫（月相一：0.25秒，月相二：0.5秒） ----
            if (_healthBridge != null)
            {
                float immunityDuration = (moonLevel >= 2) ? 0.5f : 0.25f;
                _healthBridge.SetDamageResistance(enemy, 1f, immunityDuration);
            }

            // ---- 电量扣除（所有月相） ----
            if (_enableBatteryDrain)
            {
                HandleLeaderHurt(enemy, moonLevel);
            }
        }

        private void HandleLeaderHurt(EnemyParent leader, int moonLevel)
        {
            if (_healthBridge == null)
                return;

            float maxHealth = (float)_healthBridge.GetMaxHealth(leader);
            float currentHealth = (float)_healthBridge.GetCurrentHealth(leader);
            if (maxHealth <= 0f)
                return;

            float lossRatio = 1f - (currentHealth / maxHealth);
            if (lossRatio <= 0f)
                return;

            Enemy enemyComp = leader.Enemy;
            if (enemyComp == null)
                return;

            PlayerAvatar targetPlayer = enemyComp.TargetPlayerAvatar;
            if (targetPlayer == null || targetPlayer.isDisabled)
                return;

            float ratio = lossRatio * 0.5f;
            if (moonLevel >= 2)
            {
                ratio = Mathf.Max(0.1f, ratio);
            }

            ItemBattery battery = _itemBridge.GetHeldItemBattery(targetPlayer);
            if (battery == null)
                return;

            float currentLife = battery.batteryLife;
            float newLife = Mathf.Max(0f, currentLife * (1f - ratio));
            int newLifePercent = Mathf.RoundToInt(newLife);
            if (newLifePercent < 0)
                newLifePercent = 0;
            battery.SetBatteryLife(newLifePercent);
        }

        public void ResetState()
        {
            // 无需额外清理
        }

        public void Dispose()
        {
            // 无需额外清理
        }
    }
}