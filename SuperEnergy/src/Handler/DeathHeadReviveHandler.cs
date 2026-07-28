using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace SuperEnergy
{
    public class DeathHeadReviveHandler : IEnergyHandler
    {
        private readonly IGameStateBridge _gameState;
        private readonly Dictionary<PlayerDeathHead, float> _timers = new();

        public DeathHeadReviveHandler()
        {
            _gameState = BridgeLocator.GameState;
        }

        public void Process(bool isHost, float deltaTime)
        {
            if (!isHost) return;
            var config = SuperEnergyConfig.Instance;
            if (!config.DeathHeadReviveEnabled.Value) return;

            int required = config.DeathHeadReviveRequiredTime.Value;
            if (required < 0) return;

            var deathHeads = Object.FindObjectsByType<PlayerDeathHead>(FindObjectsSortMode.None);

            if (required == 0)
            {
                foreach (var head in deathHeads)
                {
                    if (head == null || head.playerAvatar == null) continue;
                    if (head.spectated)
                        head.playerAvatar.Revive(false);
                }
                _timers.Clear();
                return;
            }

            var toRemove = new List<PlayerDeathHead>();
            foreach (var kv in _timers)
            {
                var head = kv.Key;
                if (head == null || head.playerAvatar == null || !head.triggered)
                {
                    if (head != null)
                        toRemove.Add(head);
                }
            }
            foreach (var key in toRemove)
                _timers.Remove(key);

            foreach (var head in deathHeads)
            {
                if (head == null || head.playerAvatar == null) continue;
                if (!head.spectated) continue;

                if (!_timers.TryGetValue(head, out float current))
                    current = 0f;
                current += deltaTime;
                _timers[head] = current;

                if (current >= required)
                {
                    _timers.Remove(head);
                    head.playerAvatar.Revive(false);
                }
            }
        }
    }
}