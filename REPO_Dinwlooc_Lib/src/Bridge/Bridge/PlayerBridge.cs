using System;
using System.Collections.Generic;
using System.Reflection;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    public class PlayerBridge : IPlayerBridge, IEnergyBridge
    {
        private static PlayerBridge? _instance;
        public static PlayerBridge Instance => _instance ??= new PlayerBridge();
        private PlayerBridge() { }

        // 反射字段缓存（用于自然恢复相关）
        private static FieldInfo? _sprintRechargeAmountField;
        private static FieldInfo? _sprintRechargeTimerField;
        private static FieldInfo? _sprintRechargeTimeField;
        private static FieldInfo? _energySprintDrainField;
        private static FieldInfo? _sprintSpeedUpgradesField;

        static PlayerBridge()
        {
            var type = typeof(PlayerController);
            _sprintRechargeAmountField = type.GetField("sprintRechargeAmount", BindingFlags.Instance | BindingFlags.NonPublic);
            _sprintRechargeTimerField = type.GetField("sprintRechargeTimer", BindingFlags.Instance | BindingFlags.NonPublic);
            _sprintRechargeTimeField = type.GetField("sprintRechargeTime", BindingFlags.Instance | BindingFlags.NonPublic);
            _energySprintDrainField = type.GetField("EnergySprintDrain", BindingFlags.Instance | BindingFlags.Public);
            _sprintSpeedUpgradesField = type.GetField("SprintSpeedUpgrades", BindingFlags.Instance | BindingFlags.Public);
        }

        // 获取 PlayerController 实例（仅本地玩家有效）
        private PlayerController? GetController(PlayerAvatar player)
        {
            if (player == null || !player.isLocal) return null;
            return PlayerController.instance;
        }

        // ========== IPlayerBridge ==========
        public PlayerAvatar? GetLocalPlayer() => PlayerController.instance?.playerAvatarScript;
        public List<PlayerAvatar> GetAllPlayers()
        {
            var list = new List<PlayerAvatar>();
            if (GameDirector.instance == null) return list;
            foreach (var p in GameDirector.instance.PlayerList)
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
            if (StatsManager.instance == null) return 100;
            return StatsManager.instance.playerHealth.TryGetValue(steamID, out int hp) ? hp : 100;
        }

        public void SetPlayerHP(string steamID, int newHP)
        {
            if (StatsManager.instance != null)
                StatsManager.instance.playerHealth[steamID] = newHP;
        }

        public T? GetComponentOnPlayer<T>(PlayerAvatar player) where T : Component
        {
            if (player == null) return null;
            return player.GetComponent<T>();
        }

        // ========== IEnergyBridge ==========
        // ---- 基础属性 ----
        public float GetCurrentEnergy(PlayerAvatar player)
        {
            var ctrl = GetController(player);
            if (ctrl == null) return 0f;
            return ctrl.EnergyCurrent;
        }

        public float GetMaxEnergy(PlayerAvatar player)
        {
            var ctrl = GetController(player);
            if (ctrl == null) return 100f;
            return ctrl.EnergyStart;
        }

        public void SetEnergy(PlayerAvatar player, float value)
        {
            var ctrl = GetController(player);
            if (ctrl == null) return;
            // 体力是本地玩家属性，任何人可以直接修改自己的体力，无需主机权限
            float max = ctrl.EnergyStart;
            ctrl.EnergyCurrent = Mathf.Clamp(value, 0f, max);
        }

        public void AddEnergy(PlayerAvatar player, float delta)
        {
            if (player == null) return;
            float current = GetCurrentEnergy(player);
            SetEnergy(player, current + delta);
        }

        // ---- 自然恢复规则 ----
        public bool CanRegen(PlayerAvatar player)
        {
            if (!player.isLocal) return false;
            var ctrl = PlayerController.instance;
            if (ctrl == null) return false;
            if (ctrl.sprinting) return false;
            if (GetSprintRechargeTimer(player) > 0f) return false;
            if (player.physGrabber.grabState == PhysGrabber.GrabState.Climb) return false;
            return true;
        }

        public float GetStandingRegenRate(PlayerAvatar player)
        {
            if (!player.isLocal) return 0f;
            var ctrl = PlayerController.instance;
            if (ctrl == null) return 2f;
            float baseRate = 2f;
            try
            {
                if (_sprintRechargeAmountField != null)
                    baseRate = (float)_sprintRechargeAmountField.GetValue(ctrl);
            }
            catch { /* 忽略 */ }
            if (SemiFunc.RunIsArena()) baseRate *= 5f;
            return baseRate;
        }

        public float GetSprintDrainRate(PlayerAvatar player)
        {
            if (!player.isLocal) return 0f;
            var ctrl = PlayerController.instance;
            if (ctrl == null) return 1f;
            float drain = ctrl.EnergySprintDrain;
            drain += ctrl.SprintSpeedUpgrades;
            return drain;
        }

        public float GetSprintRechargeTimer(PlayerAvatar player)
        {
            if (!player.isLocal) return 0f;
            var ctrl = PlayerController.instance;
            if (ctrl == null) return 0f;
            try
            {
                if (_sprintRechargeTimerField != null)
                    return (float)_sprintRechargeTimerField.GetValue(ctrl);
            }
            catch { /* 忽略 */ }
            return 0f;
        }

        public void ResetSprintRechargeTimer(PlayerAvatar player)
        {
            if (!player.isLocal) return;
            var ctrl = PlayerController.instance;
            if (ctrl == null) return;
            try
            {
                if (_sprintRechargeTimerField != null)
                    _sprintRechargeTimerField.SetValue(ctrl, GetSprintRechargeTime(player));
            }
            catch { /* 忽略 */ }
        }

        public float GetSprintRechargeTime(PlayerAvatar player)
        {
            if (!player.isLocal) return 1f;
            var ctrl = PlayerController.instance;
            if (ctrl == null) return 1f;
            try
            {
                if (_sprintRechargeTimeField != null)
                    return (float)_sprintRechargeTimeField.GetValue(ctrl);
            }
            catch { /* 忽略 */ }
            return 1f;
        }

        // ---- 下蹲额外恢复 ----
        public int GetCrouchRestUpgradeLevel(PlayerAvatar player)
        {
            if (player == null) return 0;
            string steamID = SemiFunc.PlayerGetSteamID(player);
            try
            {
                var upgrades = BridgeLocator.Upgrade.FetchUpgrades(steamID);
                return upgrades.TryGetValue("playerUpgradeCrouchRest", out int level) ? level : 0;
            }
            catch { return 0; }
        }

        public float GetCrouchRegenRate(PlayerAvatar player)
        {
            if (player == null) return 0f;
            // 必须下蹲/爬行且非滑铲
            if (!player.isCrouching && !player.isCrawling) return 0f;
            if (player.isSliding) return 0f;

            // 翻滚限制
            if (player.isTumbling)
            {
                if (player.tumble.notMovingTimer < 1f) return 0f;
                if (player.physGrabber.grabState == PhysGrabber.GrabState.Climb) return 0f;
            }

            // 仅本地玩家有效
            if (!player.isLocal) return 0f;

            float rate = 1f + player.upgradeCrouchRest;
            if (player.isMoving) rate *= 0.5f;
            return rate;
        }

        public bool IsCrouchRestActive(PlayerAvatar player)
        {
            if (player == null) return false;
            return player.upgradeCrouchRestActive;
        }

        // ---- 总恢复速率（整合） ----
        public float GetCurrentRegenRate(PlayerAvatar player)
        {
            float total = 0f;
            if (CanRegen(player))
                total += GetStandingRegenRate(player);
            total += GetCrouchRegenRate(player);
            return total;
        }

        // ---- 按原版规则恢复 ----
        public void ApplyRegenTick(PlayerAvatar player, float deltaTime)
        {
            float rate = GetCurrentRegenRate(player);
            if (rate > 0f)
                AddEnergy(player, rate * deltaTime);
        }
    }
}