using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.src.Bridge.IBridge;
using UnityEngine;

namespace SuperEnergy
{
    public class StaminaBoostHandler : IEnergyHandler
    {
        private readonly IPlayerBridge _playerBridge;

        public StaminaBoostHandler()
        {
            _playerBridge = BridgeLocator.Player;
        }

        public void Process(bool isHost, float deltaTime)
        {
            var config = SuperEnergyConfig.Instance;
            if (!config.EnableStaminaBoost.Value) return;
            int multiplier = config.StaminaMultiplier.Value;
            if (multiplier <= 1) return;

            var player = _playerBridge.GetLocalPlayer();
            if (player == null || !player.isLocal) return;
            if (PlayerController.instance == null) return;

            const float baseRegen = 5f;
            float extra = baseRegen * (multiplier - 1f) * deltaTime;
            PlayerController.instance.EnergyCurrent = Mathf.Min(
                PlayerController.instance.EnergyStart,
                PlayerController.instance.EnergyCurrent + extra
            );
        }
    }
}