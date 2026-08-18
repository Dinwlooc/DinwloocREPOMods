using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using MonsterCombatGroup.State;
using UnityEngine;

namespace MonsterCombatGroup.Handler
{
    /// <summary>
    /// 普通怪物受击处理（仅当领队存在时生效）。
    /// </summary>
    public class NormalMonsterHandler : IResettable
    {
        private readonly bool _enabled;
        private readonly IEnemyBridge _enemyBridge;
        private readonly IEnemyModifierBridge _modifier;

        public NormalMonsterHandler()
        {
            MonsterCombatGroupConfig config = MonsterCombatGroupConfig.Instance;
            _enabled = config.EnableLeaderMechanic.Value;

            _enemyBridge = BridgeLocator.Enemy;
            _modifier = BridgeLocator.Get<IEnemyModifierBridge>();

            if (_modifier == null)
                MonsterCombatGroup.Logger.LogWarning("IEnemyModifierBridge 未注册，普通怪物抵抗功能降级。");

            MonsterCombatGroup.Logger.LogInfo("NormalMonsterHandler 已初始化。");
        }

        /// <summary>
        /// 处理普通怪物受击（由分发器调用）。
        /// </summary>
        public void HandleHurt(int instanceId, int moonLevel)
        {
            if (!_enabled)
                return;

            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            if (!LeaderState.HasLeader) // 仅在领队存在时生效
                return;

            EnemyParent enemy = EnemyCacheService.GetEnemyById(instanceId);
            if (enemy == null)
                return;

            MoonPhaseResistConfig.ResistParams parameters = MoonPhaseResistConfig.GetNormalParams(moonLevel);
            if (parameters.NormalDuration <= 0f && parameters.StrongDuration <= 0f)
                return;

            ResistanceManager.ProcessResist(
                enemy,
                instanceId,
                parameters.StrongDuration,
                parameters.NormalDuration,
                parameters.Cooldown,
                _modifier);
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