using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    /// <summary>
    /// 外观覆盖实现，通过本地玩家组件调用原版覆盖方法。
    /// </summary>
    public class AppearanceOverrideBridge : BridgeSingleton<AppearanceOverrideBridge>, IAppearanceOverrideBridge
    {
        private const float DefaultTime = 0.1f;

        private AppearanceOverrideBridge() { }

        private PlayerAvatar GetLocalPlayer()
        {
            if (PlayerController.instance == null)
            {
                CommonPlugin.Logger.LogWarning("[AppearanceOverrideBridge] PlayerController.instance is null.");
                return null;
            }
            return PlayerController.instance.playerAvatarScript;
        }

        public void OverridePupilSize(float multiplier, int priority, float springSpeedIn, float dampIn, float springSpeedOut, float dampOut, float time = DefaultTime)
        {
            PlayerAvatar player = GetLocalPlayer();
            if (player == null) return;
            player.OverridePupilSize(multiplier, priority, springSpeedIn, dampIn, springSpeedOut, dampOut, time);
        }

        public void OverrideTTSPosition(float time = DefaultTime)
        {
            PlayerAvatar player = GetLocalPlayer();
            if (player == null) return;
            player.OverrideTTSPosition(time);
        }

        public void EyeMaterialOverride(PlayerHealth.EyeOverrideState state, float time, int priority)
        {
            PlayerAvatar player = GetLocalPlayer();
            if (player == null || player.playerHealth == null) return;
            player.playerHealth.EyeMaterialOverride(state, time, priority);
        }

        public void MaterialEffectOverride(PlayerHealth.Effect effect)
        {
            PlayerAvatar player = GetLocalPlayer();
            if (player == null || player.playerHealth == null) return;
            player.playerHealth.MaterialEffectOverride(effect);
        }
    }
}