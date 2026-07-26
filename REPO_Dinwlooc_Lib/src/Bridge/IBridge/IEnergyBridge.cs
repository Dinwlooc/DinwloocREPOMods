// 文件：Dinwlooc.Common/src/Bridge/IBridge/IEnergyBridge.cs
using UnityEngine;

namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// 玩家体力（能量）控制桥接接口。
    /// 提供无条件读写、原版规则查询、下蹲恢复感知及自动恢复触发。
    /// </summary>
    public interface IEnergyBridge
    {
        // -------- 基础属性 ----------
        /// <summary>获取当前体力值（0 ~ 上限）</summary>
        float GetCurrentEnergy(PlayerAvatar player);

        /// <summary>获取体力上限（含升级加成）</summary>
        float GetMaxEnergy(PlayerAvatar player);

        /// <summary>直接设置体力值（钳制到 [0, 上限]），无条件执行，仅主机/单机有效</summary>
        void SetEnergy(PlayerAvatar player, float value);

        /// <summary>增加（或减少）体力，不检查任何条件，自动钳制，仅主机/单机有效</summary>
        void AddEnergy(PlayerAvatar player, float delta);

        // -------- 自然恢复（站立/走动）规则 ----------
        /// <summary>当前是否允许按原版自然恢复（即：非冲刺、冷却已过、非攀爬）</summary>
        bool CanRegen(PlayerAvatar player);

        /// <summary>获取自然恢复速度（即 sprintRechargeAmount，默认 2 体力/秒），已考虑竞技场倍率（×5）</summary>
        float GetStandingRegenRate(PlayerAvatar player);

        /// <summary>获取冲刺消耗速度（体力/秒），已计入 SprintSpeedUpgrades</summary>
        float GetSprintDrainRate(PlayerAvatar player);

        /// <summary>获取冲刺后的冷却剩余时间（sprintRechargeTimer），0 表示冷却已结束</summary>
        float GetSprintRechargeTimer(PlayerAvatar player);

        /// <summary>重置冲刺冷却计时器（设为满值），强制进入冷却</summary>
        void ResetSprintRechargeTimer(PlayerAvatar player);

        /// <summary>获取冷却总时长（sprintRechargeTime，默认 1 秒）</summary>
        float GetSprintRechargeTime(PlayerAvatar player);

        // -------- 下蹲额外恢复（CrouchRest 升级） ----------
        /// <summary>获取当前下蹲恢复等级（playerUpgradeCrouchRest）</summary>
        int GetCrouchRestUpgradeLevel(PlayerAvatar player);

        /// <summary>获取下蹲/爬行时的额外恢复速度（仅当处于下蹲/爬行且非滑铲时有效，否则返回0），
        /// 已包含升级加成和移动减半系数，仅本地玩家有效</summary>
        float GetCrouchRegenRate(PlayerAvatar player);

        /// <summary>是否正在享受下蹲恢复效果（对应 upgradeCrouchRestActive）</summary>
        bool IsCrouchRestActive(PlayerAvatar player);

        // -------- 总恢复速率（整合） ----------
        /// <summary>获取当前实际恢复速度（自然恢复 + 下蹲额外恢复，各自满足条件），单位：体力/秒</summary>
        float GetCurrentRegenRate(PlayerAvatar player);

        // -------- 按原版规则恢复 ----------
        /// <summary>按照原版恢复逻辑，应用一次恢复增量（内部调用 GetCurrentRegenRate * deltaTime）</summary>
        void ApplyRegenTick(PlayerAvatar player, float deltaTime);
    }
}