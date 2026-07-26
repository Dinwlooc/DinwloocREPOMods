// StaminaBoostHandler.cs
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace SuperEnergy
{
    public class StaminaBoostHandler : IEnergyHandler
    {
        private IEnergyBridge _energyBridge = null!;
        private IPlayerBridge _playerBridge = null!;

        public StaminaBoostHandler()
        {
            _energyBridge = BridgeLocator.Energy;
            _playerBridge = BridgeLocator.Player;
        }

        public void Process(bool isHost, float deltaTime)
        {
            var config = SuperEnergyConfig.Instance;
            if (!config.EnableStaminaBoost.Value) return;

            var player = _playerBridge.GetLocalPlayer();
            if (player == null || !player.isLocal) return;

            if (!SuperEnergy.TryGetEffectiveConfig(out var remoteConfig))
                return;

            int percent = remoteConfig.Percent;
            bool comp = remoteConfig.CompensateWhenDisabled;
            bool crouch = remoteConfig.EnableCrouchBoost;

            float multiplier = 1f + percent / 100f;

            float standingRate = _energyBridge.GetStandingRegenRate(player);
            bool canRegen = _energyBridge.CanRegen(player);
            float amplifiedStanding = 0f;

            if (canRegen)
            {
                amplifiedStanding = standingRate * multiplier;
            }
            else if (comp)
            {
                amplifiedStanding = standingRate * multiplier;
            }

            float crouchRate = _energyBridge.GetCrouchRegenRate(player);
            float amplifiedCrouch = 0f;
            if (crouchRate > 0f && crouch)
            {
                amplifiedCrouch = crouchRate * multiplier;
            }
            else if (crouchRate > 0f && !crouch)
            {
                amplifiedCrouch = crouchRate;
            }

            float totalRate = amplifiedStanding + amplifiedCrouch;
            if (totalRate > 0f)
            {
                _energyBridge.AddEnergy(player, totalRate * deltaTime);
            }
        }
    }
}