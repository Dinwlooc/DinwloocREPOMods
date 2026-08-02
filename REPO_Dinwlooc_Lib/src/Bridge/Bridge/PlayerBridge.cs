using System;
using System.Collections.Generic;
using System.Reflection;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using Dinwlooc.Common.Reflection;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    public class PlayerBridge : BridgeSingleton<PlayerBridge>, IPlayerBridge, IEnergyBridge
    {
        private const float DefaultHealth = 100f;
        private const float DefaultRegenRate = 2f;
        private const float ArenaMultiplier = 5f;
        private const float CrouchMovePenalty = 0.5f;

        private PlayerBridge() { }

        private PlayerController GetController(PlayerAvatar player)
        {
            if (player == null || !player.isLocal) return null;
            return PlayerController.instance;
        }

        // ========== IPlayerBridge ==========
        public PlayerAvatar GetLocalPlayer() => PlayerController.instance?.playerAvatarScript;

        public List<PlayerAvatar> GetAllPlayers()
        {
            List<PlayerAvatar> list = new List<PlayerAvatar>();
            if (GameDirector.instance == null) return list;
            foreach (PlayerAvatar p in GameDirector.instance.PlayerList)
            {
                if (p != null && !p.isDisabled)
                    list.Add(p);
            }
            return list;
        }

        public void HealPlayer(PlayerAvatar player, int amount, bool effect = true)
        {
            if (player == null || player.playerHealth == null || amount <= 0) return;
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer()) return;
            if (player.photonView.IsMine)
                player.playerHealth.Heal(amount, effect);
            else
                player.playerHealth.HealOther(amount, effect);
        }

        public int GetPlayerHP(string steamID)
        {
            if (StatsManager.instance == null) return (int)DefaultHealth;
            return StatsManager.instance.playerHealth.TryGetValue(steamID, out int hp) ? hp : (int)DefaultHealth;
        }

        public void SetPlayerHP(string steamID, int newHP)
        {
            if (StatsManager.instance != null)
                StatsManager.instance.playerHealth[steamID] = newHP;
        }

        public T GetComponentOnPlayer<T>(PlayerAvatar player) where T : Component
        {
            if (player == null) return null;
            return player.GetComponent<T>();
        }

        // ========== IEnergyBridge ==========
        public float GetCurrentEnergy(PlayerAvatar player)
        {
            PlayerController ctrl = GetController(player);
            if (ctrl == null) return 0f;
            return ctrl.EnergyCurrent;
        }

        public float GetMaxEnergy(PlayerAvatar player)
        {
            PlayerController ctrl = GetController(player);
            if (ctrl == null) return 100f;
            return ctrl.EnergyStart;
        }

        public void SetEnergy(PlayerAvatar player, float value)
        {
            PlayerController ctrl = GetController(player);
            if (ctrl == null) return;
            float max = ctrl.EnergyStart;
            ctrl.EnergyCurrent = Mathf.Clamp(value, 0f, max);
        }

        public void AddEnergy(PlayerAvatar player, float delta)
        {
            if (player == null) return;
            float current = GetCurrentEnergy(player);
            SetEnergy(player, current + delta);
        }

        public bool CanRegen(PlayerAvatar player)
        {
            if (!player.isLocal) return false;
            PlayerController ctrl = PlayerController.instance;
            if (ctrl == null) return false;
            if (ctrl.sprinting) return false;
            if (GetSprintRechargeTimer(player) > 0f) return false;
            if (player.physGrabber.grabState == PhysGrabber.GrabState.Climb) return false;
            return true;
        }

        public float GetStandingRegenRate(PlayerAvatar player)
        {
            if (!player.isLocal) return 0f;
            PlayerController ctrl = PlayerController.instance;
            if (ctrl == null) return DefaultRegenRate;
            float baseRate = DefaultRegenRate;
            try
            {
                FieldInfo field = ReflectionCache.PlayerController_sprintRechargeAmount;
                if (field != null)
                    baseRate = (float)field.GetValue(ctrl);
            }
            catch { /* 忽略 */ }
            if (SemiFunc.RunIsArena()) baseRate *= ArenaMultiplier;
            return baseRate;
        }

        public float GetSprintDrainRate(PlayerAvatar player)
        {
            if (!player.isLocal) return 0f;
            PlayerController ctrl = PlayerController.instance;
            if (ctrl == null) return 1f;
            float drain = ctrl.EnergySprintDrain;
            drain += ctrl.SprintSpeedUpgrades;
            return drain;
        }

        public float GetSprintRechargeTimer(PlayerAvatar player)
        {
            if (!player.isLocal) return 0f;
            PlayerController ctrl = PlayerController.instance;
            if (ctrl == null) return 0f;
            try
            {
                FieldInfo field = ReflectionCache.PlayerController_sprintRechargeTimer;
                if (field != null)
                    return (float)field.GetValue(ctrl);
            }
            catch { /* 忽略 */ }
            return 0f;
        }

        public void ResetSprintRechargeTimer(PlayerAvatar player)
        {
            if (!player.isLocal) return;
            PlayerController ctrl = PlayerController.instance;
            if (ctrl == null) return;
            try
            {
                FieldInfo field = ReflectionCache.PlayerController_sprintRechargeTimer;
                if (field != null)
                    field.SetValue(ctrl, GetSprintRechargeTime(player));
            }
            catch { /* 忽略 */ }
        }

        public float GetSprintRechargeTime(PlayerAvatar player)
        {
            if (!player.isLocal) return 1f;
            PlayerController ctrl = PlayerController.instance;
            if (ctrl == null) return 1f;
            try
            {
                FieldInfo field = ReflectionCache.PlayerController_sprintRechargeTime;
                if (field != null)
                    return (float)field.GetValue(ctrl);
            }
            catch { /* 忽略 */ }
            return 1f;
        }

        public int GetCrouchRestUpgradeLevel(PlayerAvatar player)
        {
            if (player == null) return 0;
            string steamID = SemiFunc.PlayerGetSteamID(player);
            try
            {
                Dictionary<string, int> upgrades = BridgeLocator.Upgrade.FetchUpgrades(steamID);
                return upgrades.TryGetValue("playerUpgradeCrouchRest", out int level) ? level : 0;
            }
            catch { return 0; }
        }

        public float GetCrouchRegenRate(PlayerAvatar player)
        {
            if (player == null) return 0f;
            if (!player.isCrouching && !player.isCrawling) return 0f;
            if (player.isSliding) return 0f;

            if (player.isTumbling)
            {
                if (player.tumble.notMovingTimer < 1f) return 0f;
                if (player.physGrabber.grabState == PhysGrabber.GrabState.Climb) return 0f;
            }

            if (!player.isLocal) return 0f;

            float rate = 1f + player.upgradeCrouchRest;
            if (player.isMoving) rate *= CrouchMovePenalty;
            return rate;
        }

        public bool IsCrouchRestActive(PlayerAvatar player)
        {
            if (player == null) return false;
            return player.upgradeCrouchRestActive;
        }

        public float GetCurrentRegenRate(PlayerAvatar player)
        {
            float total = 0f;
            if (CanRegen(player))
                total += GetStandingRegenRate(player);
            total += GetCrouchRegenRate(player);
            return total;
        }

        public void ApplyRegenTick(PlayerAvatar player, float deltaTime)
        {
            float rate = GetCurrentRegenRate(player);
            if (rate > 0f)
                AddEnergy(player, rate * deltaTime);
        }
    }
}