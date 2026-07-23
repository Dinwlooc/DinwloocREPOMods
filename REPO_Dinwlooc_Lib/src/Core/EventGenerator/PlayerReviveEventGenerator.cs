using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.src.Bridge.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    /// <summary>
    /// 检测玩家复活并发布 PlayerRevivedEvent
    /// </summary>
    public class PlayerReviveEventGenerator : EventGeneratorBase<PlayerRevivedEvent>
    {
        private static PlayerReviveEventGenerator? _instance;
        public static PlayerReviveEventGenerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(nameof(PlayerReviveEventGenerator));
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PlayerReviveEventGenerator>();
                }
                return _instance;
            }
        }

        private IPlayerBridge _playerBridge = null!;
        private Dictionary<int, int> _lastHealth = new(); // instanceId -> health

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

            var players = _playerBridge.GetAllPlayers();
            if (players == null || players.Count == 0)
            {
                _lastHealth.Clear();
                return;
            }

            var currentHealth = new Dictionary<int, int>();
            foreach (var p in players)
            {
                if (p == null) continue;
                int id = p.GetInstanceID();
                int health = p.playerHealth?.health ?? 0;
                currentHealth[id] = health;
            }

            // 检测复活（上次健康 <= 0 且 当前健康 > 0）
            foreach (var kv in currentHealth)
            {
                int id = kv.Key;
                int current = kv.Value;
                if (_lastHealth.TryGetValue(id, out int last))
                {
                    if (last <= 0 && current > 0)
                    {
                        PlayerAvatar? player = players.Find(p => p != null && p.GetInstanceID() == id);
                        if (player != null)
                        {
                            EventBus.Publish(new PlayerRevivedEvent(player));
                        }
                    }
                }
            }

            _lastHealth = currentHealth;
        }
    }
}