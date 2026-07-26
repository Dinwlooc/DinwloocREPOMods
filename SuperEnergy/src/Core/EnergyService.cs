using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace SuperEnergy
{
    public class EnergyService : MonoBehaviour
    {
        private readonly List<IEnergyHandler> _handlers = new();
        private float _nextTickTime = 0f;
        private const float TickInterval = 0.5f;
        private IGameStateBridge _gameState = null!;

        private void Awake()
        {
            _gameState = BridgeLocator.GameState;
            DontDestroyOnLoad(gameObject);

            _handlers.Add(new ItemChargingHandler());
            _handlers.Add(new PlayerHealHandler());
            _handlers.Add(new DeathHeadReviveHandler());
            _handlers.Add(new StaminaBoostHandler());
        }

        private void Update()
        {
            if (SemiFunc.IsMainMenu() || SemiFunc.RunIsLobbyMenu()) return;
            if (!SemiFunc.RunIsLevel()) return;

            if (Time.time >= _nextTickTime)
            {
                _nextTickTime = Time.time + TickInterval;
                bool isHost = _gameState.IsMasterClientOrSingleplayer();
                foreach (var handler in _handlers)
                    handler.Process(isHost, TickInterval);
            }
        }
    }
}