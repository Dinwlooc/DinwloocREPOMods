using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace SuperEnergy
{
    public class StaminaBoostHandler : IEnergyHandler
    {
        private readonly IEnergyBridge _energyBridge;
        private readonly IPlayerBridge _playerBridge;

        public StaminaBoostHandler()
        {
            _energyBridge = BridgeLocator.Energy;
            _playerBridge = BridgeLocator.Player;
        }

        public void Process(bool isHost, float deltaTime)
        {
            SuperEnergyConfig config = SuperEnergyConfig.Instance;
            if (!config.StaminaBoostEnabled.Value)
                return;

            PlayerAvatar player = _playerBridge.GetLocalPlayer();
            if (player == null || !player.isLocal)
                return;

            StaminaSyncConfig syncConfig = StaminaConfigManager.Instance.GetEffectiveConfig();
            if (syncConfig == null)
                return;

            int percent = syncConfig.Percent;
            bool comp = syncConfig.CompensateWhenDisabled;
            bool crouch = syncConfig.EnableCrouchBoost;

            float multiplier = 1f + percent / 100f;

            float standingRate = _energyBridge.GetStandingRegenRate(player);
            bool canRegen = _energyBridge.CanRegen(player);
            float amplifiedStanding = 0f;

            if (canRegen)
                amplifiedStanding = standingRate * multiplier;
            else if (comp)
                amplifiedStanding = standingRate * multiplier;

            float crouchRate = _energyBridge.GetCrouchRegenRate(player);
            float amplifiedCrouch = 0f;
            if (crouchRate > 0f && crouch)
                amplifiedCrouch = crouchRate * multiplier;
            else if (crouchRate > 0f && !crouch)
                amplifiedCrouch = crouchRate;

            float totalRate = amplifiedStanding + amplifiedCrouch;
            if (totalRate > 0f)
                _energyBridge.AddEnergy(player, totalRate * deltaTime);
        }
    }
}