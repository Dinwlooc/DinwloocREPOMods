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
        private readonly IEnemyModifierBridge _modifier;
        private readonly IGameStateBridge _gameState;

        public GuardAbilityHandler()
        {
            MonsterCombatGroupConfig config = MonsterCombatGroupConfig.Instance;
            _enabled = config.EnableLeaderMechanic.Value;

            _enemyBridge = BridgeLocator.Enemy;
            _modifier = BridgeLocator.Get<IEnemyModifierBridge>();
            _gameState = BridgeLocator.GameState;

            if (_modifier == null)
                MonsterCombatGroup.Logger.LogWarning("IEnemyModifierBridge 未注册，护卫能力降级。");

            MonsterCombatGroup.Logger.LogInfo("GuardAbilityHandler 已初始化（守卫受击逻辑已整合）。");
        }

        public void Process(float deltaTime)
        {
            if (!_enabled)
                return;

            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            if (_gameState.IsMainMenu() || !_gameState.IsLevelLoaded())
                return;

            // 协同追击逻辑
            if (!LeaderState.HasLeader)
                return;

            EnemyParent leaderParent = EnemyCacheService.GetEnemyById(LeaderState.LeaderInstanceId);
            if (leaderParent == null || !_enemyBridge.IsEnemyValid(leaderParent))
                return;

            Enemy leaderEnemy = leaderParent.Enemy;
            if (leaderEnemy == null)
                return;

            PlayerAvatar chaseTarget = leaderEnemy.TargetPlayerAvatar;

            foreach (int guardId in LeaderState.GuardInstanceIds)
            {
                EnemyParent guardParent = EnemyCacheService.GetEnemyById(guardId);
                if (guardParent == null || !_enemyBridge.IsEnemyValid(guardParent))
                    continue;

                Enemy guardEnemy = guardParent.Enemy;
                if (guardEnemy == null)
                    continue;

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

        /// <summary>
        /// 处理守卫受击（由分发器调用）。
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

            MoonPhaseResistConfig.ResistParams parameters = MoonPhaseResistConfig.GetGuardParams(moonLevel);
            if (parameters.NormalDuration <= 0f && parameters.StrongDuration <= 0f)
                return;

            bool triggered = ResistanceManager.ProcessResist(
                enemy,
                instanceId,
                parameters.StrongDuration,
                parameters.NormalDuration,
                parameters.Cooldown,
                _modifier);

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
                    ResistanceManager.RefreshCooldownForGuard(otherGuardId, parameters.Cooldown);
                }
            }
        }

        public void ResetState()
        {
            // 无需额外清理，缓存由 EnemyCacheService 管理
        }

        public void Dispose()
        {
            // 无需额外清理
        }
    }
}