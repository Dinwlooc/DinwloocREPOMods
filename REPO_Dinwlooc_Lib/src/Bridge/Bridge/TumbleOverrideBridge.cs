using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    /// <summary>
    /// 翻滚覆盖实现，通过本地玩家的 PlayerTumble 和 PlayerAvatar 调用原版覆盖方法。
    /// </summary>
    public class TumbleOverrideBridge : BridgeSingleton<TumbleOverrideBridge>, ITumbleOverrideBridge
    {
        private const float DefaultTime = 0.1f;
        private const float DefaultLookSpeed = 5f;
        private const float DefaultDampen = 3f;
        private const float DefaultInvestigateTime = 1f;

        private TumbleOverrideBridge() { }

        private PlayerAvatar GetLocalPlayer()
        {
            if (PlayerController.instance == null)
            {
                CommonPlugin.Logger.LogWarning("[TumbleOverrideBridge] PlayerController.instance is null.");
                return null;
            }
            return PlayerController.instance.playerAvatarScript;
        }

        private PlayerTumble GetTumble()
        {
            PlayerAvatar player = GetLocalPlayer();
            if (player == null) return null;
            return player.tumble;
        }

        public void OverrideEnemyHurt(float time)
        {
            PlayerTumble tumble = GetTumble();
            if (tumble == null) return;
            tumble.OverrideEnemyHurt(time);
        }

        public void OverrideLookAtCamera(float time, float speed = DefaultLookSpeed, float dampen = DefaultDampen)
        {
            PlayerTumble tumble = GetTumble();
            if (tumble == null) return;
            tumble.OverrideLookAtCamera(time, speed, dampen);
        }

        public void OverrideDisableLookAtCamera(float time)
        {
            PlayerTumble tumble = GetTumble();
            if (tumble == null) return;
            tumble.OverrideDisableLookAtCamera(time);
        }

        public void OverrideTumbleUIDisable(float time)
        {
            PlayerTumble tumble = GetTumble();
            if (tumble == null) return;
            tumble.OverrideTumbleUIDisable(time);
        }

        public void OverrideDisableTumbleMoveSound(float time)
        {
            PlayerTumble tumble = GetTumble();
            if (tumble == null) return;
            tumble.OverrideDisableTumbleMoveSound(time);
        }

        public void OverrideTumble(bool active)
        {
            PlayerTumble tumble = GetTumble();
            if (tumble == null) return;
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer())
            {
                CommonPlugin.Logger.LogWarning("[TumbleOverrideBridge] OverrideTumble requires host.");
                return;
            }
            tumble.TumbleOverride(active);
        }

        public void OverrideTumbleTime(float time)
        {
            PlayerTumble tumble = GetTumble();
            if (tumble == null) return;
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer())
            {
                CommonPlugin.Logger.LogWarning("[TumbleOverrideBridge] OverrideTumbleTime requires host.");
                return;
            }
            tumble.TumbleOverrideTime(time);
        }

        public void OverrideDisableEnemyInvestigate(float time = DefaultInvestigateTime)
        {
            PlayerAvatar player = GetLocalPlayer();
            if (player == null) return;
            player.OverrideDisableEnemyInvestigate(time);
        }
    }
}