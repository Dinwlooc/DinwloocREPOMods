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
        private readonly IEnemyModifierBridge? _modifier;

        private readonly Dictionary<int, EnemyParent> _enemyCache = new Dictionary<int, EnemyParent>();
        private float _nextCacheRefreshTime = 0f;
        private const float CACHE_REFRESH_INTERVAL = 0.5f;

        public NormalMonsterHandler()
        {
            MonsterCombatGroupConfig cfg = MonsterCombatGroupConfig.Instance;
            _enabled = cfg.EnableLeaderMechanic.Value;

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
            if (!_enabled) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!LeaderState.HasLeader) return; // 仅在领队存在时生效

            if (Time.time >= _nextCacheRefreshTime)
            {
                _nextCacheRefreshTime = Time.time + CACHE_REFRESH_INTERVAL;
                RefreshCache();
            }

            if (!_enemyCache.TryGetValue(instanceId, out EnemyParent? enemy))
                return;

            MoonPhaseResistConfig.ResistParams p = MoonPhaseResistConfig.GetNormalParams(moonLevel);
            // 若配置无效（全部为0），则跳过
            if (p.NormalDuration <= 0f && p.StrongDuration <= 0f)
                return;

            ResistanceManager.ProcessResist(enemy, instanceId, p.StrongDuration, p.NormalDuration, p.Cooldown, _modifier);
        }

        private void RefreshCache()
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