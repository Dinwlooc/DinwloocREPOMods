using UnityEngine;

namespace SuperEnergy
{
    public class StaminaBoostHandler : IEnergyHandler
    {
        private readonly RepoGameBridge _bridge;

        public StaminaBoostHandler(RepoGameBridge bridge) => _bridge = bridge;

        public void Process(bool isHost, float deltaTime)
        {
            if (!SuperEnergy.EnableStaminaBoost?.Value ?? false) return;
            int multiplier = SuperEnergy.StaminaMultiplier?.Value ?? 2;
            if (multiplier <= 1) return;

            var player = _bridge.GetLocalPlayer();
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