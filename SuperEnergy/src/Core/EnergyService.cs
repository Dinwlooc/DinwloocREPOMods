using System.Collections.Generic;
using UnityEngine;

namespace SuperEnergy
{
    public class EnergyService : MonoBehaviour
    {
        private RepoGameBridge _bridge = null!;
        private readonly List<IEnergyHandler> _handlers = new();
        private float _nextTickTime = 0f;
        private const float TickInterval = 0.5f;

        private void Awake()
        {
            _bridge = RepoGameBridge.Instance;
            DontDestroyOnLoad(gameObject);

            _handlers.Add(new ItemChargingHandler(_bridge));
            _handlers.Add(new PlayerHealHandler(_bridge));
            _handlers.Add(new DeathHeadReviveHandler(_bridge));
            _handlers.Add(new StaminaBoostHandler(_bridge));
        }

        private void Update()
        {
            if (SemiFunc.IsMainMenu() || SemiFunc.RunIsLobbyMenu()) return;
            if (!SemiFunc.RunIsLevel()) return;

            if (Time.time >= _nextTickTime)
            {
                _nextTickTime = Time.time + TickInterval;
                bool isHost = SemiFunc.IsMasterClientOrSingleplayer();
                foreach (var handler in _handlers)
                    handler.Process(isHost, TickInterval);
            }
        }
    }
}