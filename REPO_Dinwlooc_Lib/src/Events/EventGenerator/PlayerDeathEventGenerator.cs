using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    public class PlayerDeathEventGenerator : EventGeneratorBase<PlayerDiedEvent>
    {
        private static PlayerDeathEventGenerator? _instance;
        public static PlayerDeathEventGenerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject(nameof(PlayerDeathEventGenerator));
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PlayerDeathEventGenerator>();
                }
                return _instance;
            }
        }

        private IPlayerBridge _playerBridge = null!;
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
            if (!SemiFunc.RunIsLevel()) return;

            List<PlayerAvatar> players = _playerBridge.GetAllPlayers();
            if (players == null || players.Count == 0)
            {
                _lastHealth.Clear();
                return;
            }

            // 复用字典
            _currentHealth.Clear();
            foreach (PlayerAvatar p in players)
            {
                if (p == null) continue;
                int id = p.GetInstanceID();
                int health = p.playerHealth?.health ?? 0;
                _currentHealth[id] = health;
            }

            foreach (KeyValuePair<int, int> kv in _currentHealth)
            {
                int id = kv.Key;
                int current = kv.Value;
                if (_lastHealth.TryGetValue(id, out int last))
                {
                    if (last > 0 && current <= 0)
                    {
                        PlayerAvatar? player = players.Find(p => p != null && p.GetInstanceID() == id);
                        if (player != null)
                        {
                            EventBus.Publish(new PlayerDiedEvent(player));
                        }
                    }
                }
            }

            // 交换引用，而非复制
            (_lastHealth, _currentHealth) = (_currentHealth, _lastHealth);
        }
    }
}