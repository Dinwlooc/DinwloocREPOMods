// Dinwlooc.Common/Core/VisionEventGenerator.cs
using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    public class VisionEventGenerator : EventGeneratorBase<MonsterVisibilityChangedEvent>
    {
        private const float VISION_RANGE = 999f;

        private static VisionEventGenerator _instance;
        public static VisionEventGenerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject(nameof(VisionEventGenerator));
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<VisionEventGenerator>();
                }
                return _instance;
            }
        }

        private IEnemyBridge _enemyBridge = null;
        private IPlayerBridge _playerBridge = null;
        private HashSet<int> _previousSeen = new HashSet<int>();

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
            if (!SemiFunc.RunIsLevel())
                return;

            PlayerAvatar localPlayer = _playerBridge.GetLocalPlayer();
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

            IReadOnlyList<EnemyParent> allEnemies = _enemyBridge.GetAllEnemies();
            if (allEnemies == null || allEnemies.Count == 0)
                return;

            List<int> seenIDs = new List<int>();
            foreach (EnemyParent ep in allEnemies)
            {
                if (!_enemyBridge.IsEnemyValid(ep))
                    continue;
                Vector3 enemyPos = _enemyBridge.GetEnemyPosition(ep);
                if (SemiFunc.PlayerVisionCheck(enemyPos, VISION_RANGE, localPlayer, false))
                {
                    seenIDs.Add(_enemyBridge.GetEnemyInstanceId(ep));
                }
            }

            HashSet<int> newSet = new HashSet<int>(seenIDs);
            if (_previousSeen.SetEquals(newSet))
                return;

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