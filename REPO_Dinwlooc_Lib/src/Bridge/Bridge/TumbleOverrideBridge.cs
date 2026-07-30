using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    /// <summary>
    /// 翻滚覆盖实现，通过本地玩家的 PlayerTumble 和 PlayerAvatar 调用原版覆盖方法。
    /// </summary>
    public class TumbleOverrideBridge : ITumbleOverrideBridge
    {
        private static TumbleOverrideBridge? _instance;
        public static TumbleOverrideBridge Instance => _instance ??= new TumbleOverrideBridge();

        private TumbleOverrideBridge() { }

        private PlayerAvatar? GetLocalPlayer()
        {
            if (PlayerController.instance == null)
            {
                CommonPlugin.Logger.LogWarning("[TumbleOverrideBridge] PlayerController.instance is null.");
                return null;
            }
            return PlayerController.instance.playerAvatarScript;
        }

        private PlayerTumble? GetTumble()
        {
            PlayerAvatar? player = GetLocalPlayer();
            if (player == null) return null;
            return player.tumble;
        }

        public void OverrideEnemyHurt(float time)
        {
            PlayerTumble? tumble = GetTumble();
            if (tumble == null) return;
            tumble.OverrideEnemyHurt(time);
        }

        public void OverrideLookAtCamera(float time, float speed = 5f, float dampen = 3f)
        {
            PlayerTumble? tumble = GetTumble();
            if (tumble == null) return;
            tumble.OverrideLookAtCamera(time, speed, dampen);
        }

        public void OverrideDisableLookAtCamera(float time)
        {
            PlayerTumble? tumble = GetTumble();
            if (tumble == null) return;
            tumble.OverrideDisableLookAtCamera(time);
        }

        public void OverrideTumbleUIDisable(float time)
        {
            PlayerTumble? tumble = GetTumble();
            if (tumble == null) return;
            tumble.OverrideTumbleUIDisable(time);
        }

        public void OverrideDisableTumbleMoveSound(float time)
        {
            PlayerTumble? tumble = GetTumble();
            if (tumble == null) return;
            tumble.OverrideDisableTumbleMoveSound(time);
        }

        public void OverrideTumble(bool active)
        {
            PlayerTumble? tumble = GetTumble();
            if (tumble == null) return;
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer())
            {
                CommonPlugin.Logger.LogWarning("[TumbleOverrideBridge] OverrideTumble requires host.");
                return;
            }
            tumble.TumbleOverride(active); // 注意：原版方法名为 TumbleOverride，不是 OverrideTumble
        }

        public void OverrideTumbleTime(float time)
        {
            PlayerTumble? tumble = GetTumble();
            if (tumble == null) return;
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer())
            {
                CommonPlugin.Logger.LogWarning("[TumbleOverrideBridge] OverrideTumbleTime requires host.");
                return;
            }
            tumble.TumbleOverrideTime(time);
        }

        public void OverrideDisableEnemyInvestigate(float time = 1f)
        {
            PlayerAvatar? player = GetLocalPlayer();
            if (player == null) return;
            player.OverrideDisableEnemyInvestigate(time);
        }
    }
}