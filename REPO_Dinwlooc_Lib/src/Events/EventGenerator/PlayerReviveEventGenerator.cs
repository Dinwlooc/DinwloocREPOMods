// Dinwlooc.Common/Core/PlayerReviveEventGenerator.cs
using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    public class PlayerReviveEventGenerator : EventGeneratorBase<PlayerRevivedEvent>
    {
        private const int DEAD_HEALTH_THRESHOLD = 0;

        private static PlayerReviveEventGenerator _instance;
        public static PlayerReviveEventGenerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject(nameof(PlayerReviveEventGenerator));
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PlayerReviveEventGenerator>();
                }
                return _instance;
            }
        }

        private IPlayerBridge _playerBridge = null;
        private Dictionary<int, int> _lastHealth = new Dictionary<int, int>();
        private Dictionary<int, int> _currentHealth = new Dictionary<int, int>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _playerBridge = BridgeLocator.Player;
        }

        protected override void GenerateEvent()
        {
            if (!SemiFunc.RunIsLevel())
                return;

            List<PlayerAvatar> players = _playerBridge.GetAllPlayers();
            if (players == null || players.Count == 0)
            {
                _lastHealth.Clear();
                return;
            }

            _currentHealth.Clear();
            foreach (PlayerAvatar player in players)
            {
                if (player == null)
                    continue;
                int instanceId = player.GetInstanceID();
                int health = player.playerHealth?.health ?? DEAD_HEALTH_THRESHOLD;
                _currentHealth[instanceId] = health;
            }

            foreach (KeyValuePair<int, int> kv in _currentHealth)
            {
                int id = kv.Key;
                int current = kv.Value;
                if (!_lastHealth.TryGetValue(id, out int last))
                    continue;

                if (last <= DEAD_HEALTH_THRESHOLD && current > DEAD_HEALTH_THRESHOLD)
                {
                    PlayerAvatar player = players.Find(p => p != null && p.GetInstanceID() == id);
                    if (player != null)
                        EventBus.Publish(new PlayerRevivedEvent(player));
                }
            }

            (_lastHealth, _currentHealth) = (_currentHealth, _lastHealth);
        }
    }
}