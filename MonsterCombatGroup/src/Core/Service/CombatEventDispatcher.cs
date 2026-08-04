using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using MonsterCombatGroup.Handler;
using MonsterCombatGroup.State;
using UnityEngine;

namespace MonsterCombatGroup
{
    /// <summary>
    /// 受击事件分发器，根据怪物身份调用对应的处理器。
    /// </summary>
    public class CombatEventDispatcher : ICombatHandler, IResettable
    {
        private readonly LeaderAbilityHandler _leaderHandler;
        private readonly GuardAbilityHandler _guardHandler;
        private readonly NormalMonsterHandler _normalHandler;
        private readonly IGameStateBridge _gameState;
        private bool _subscribed = false;

        public CombatEventDispatcher(
            LeaderAbilityHandler leaderHandler,
            GuardAbilityHandler guardHandler,
            NormalMonsterHandler normalHandler)
        {
            _leaderHandler = leaderHandler;
            _guardHandler = guardHandler;
            _normalHandler = normalHandler;
            _gameState = BridgeLocator.GameState;

            EventBus.Subscribe<EnemyHurtEvent>(OnEnemyHurt);
            _subscribed = true;
            MonsterCombatGroup.Logger.LogInfo("CombatEventDispatcher 已初始化。");
        }

        private void OnEnemyHurt(EnemyHurtEvent evt)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (_gameState.IsMainMenu() || !_gameState.IsLevelLoaded()) return;

            int moonLevel = BridgeLocator.Moon.GetCurrentMoonLevel();
            if (moonLevel < 1) return; // 月相1以上才激活

            int id = evt.InstanceId;
            // 获取怪物实例（由于分发器不维护缓存，由各处理器自行获取，但为避免重复查找，我们可以直接传id，各处理器内部通过EnemyBridge获取）
            // 这里我们直接传递 id，让处理器自己去查找，但为了效率，可以在分发器中尝试获取，但会增加耦合。
            // 决定让各处理器通过自己的缓存或桥接获取，所以只传 id。

            if (LeaderState.IsLeader(id))
            {
                _leaderHandler.HandleHurt(id, moonLevel);
            }
            else if (LeaderState.IsGuard(id))
            {
                _guardHandler.HandleHurt(id, moonLevel);
            }
            else
            {
                // 普通怪物
                _normalHandler.HandleHurt(id, moonLevel);
            }
        }

        public void Process(float deltaTime)
        {
            // 分发器无需每帧处理
        }

        public void ResetState()
        {
            // 重置所有子处理器状态
            _leaderHandler.ResetState();
            _guardHandler.ResetState();
            _normalHandler.ResetState();
            ResistanceManager.Reset(); // 全局重置
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                EventBus.Unsubscribe<EnemyHurtEvent>(OnEnemyHurt);
                _subscribed = false;
            }
            _leaderHandler.Dispose();
            _guardHandler.Dispose();
            _normalHandler.Dispose();
        }
    }
}