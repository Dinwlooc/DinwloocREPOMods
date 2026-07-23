using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.src.Bridge.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    public class VisionEventGenerator : EventGeneratorBase<MonsterVisibilityChangedEvent>
    {
        private static VisionEventGenerator? _instance;
        public static VisionEventGenerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(nameof(VisionEventGenerator));
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<VisionEventGenerator>();
                }
                return _instance;
            }
        }

        private IEnemyBridge _enemyBridge = null!;
        private IPlayerBridge _playerBridge = null!;
        private HashSet<int> _previousSeen = new();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _enemyBridge = BridgeLocator.Enemy;
            _playerBridge = BridgeLocator.Player;
        }

        protected override void GenerateEvent()
        {
            if (!SemiFunc.RunIsLevel()) return;

            var localPlayer = _playerBridge.GetLocalPlayer();
            if (localPlayer == null || localPlayer.isDisabled)
            {
                if (_previousSeen.Count > 0)
                {
                    foreach (int id in _previousSeen)
                        EventBus.Publish(new MonsterVisibilityChangedEvent(id, false));
                    _previousSeen.Clear();
                }
                return;
            }

            var allEnemies = _enemyBridge.GetAllEnemies();
            if (allEnemies == null || allEnemies.Count == 0)
            {
                // 无敌人时不清除 _previousSeen（避免重复发布）
                return;
            }

            var seenIDs = new List<int>();
            foreach (var ep in allEnemies)
            {
                if (!_enemyBridge.IsEnemyValid(ep)) continue;
                Vector3 enemyPos = _enemyBridge.GetEnemyPosition(ep);
                if (SemiFunc.PlayerVisionCheck(enemyPos, 999f, localPlayer, false))
                {
                    seenIDs.Add(_enemyBridge.GetEnemyInstanceId(ep));
                }
            }

            HashSet<int> newSet = new(seenIDs);
            if (!_previousSeen.SetEquals(newSet))
            {
                foreach (int id in newSet)
                    if (!_previousSeen.Contains(id))
                        EventBus.Publish(new MonsterVisibilityChangedEvent(id, true));
                foreach (int id in _previousSeen)
                    if (!newSet.Contains(id))
                        EventBus.Publish(new MonsterVisibilityChangedEvent(id, false));

                _previousSeen = newSet;
            }
        }
    }
}