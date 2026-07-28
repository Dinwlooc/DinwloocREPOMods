using UnityEngine;

namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// 物品相关操作桥接接口。
    /// </summary>
    public interface IItemBridge
    {
        /// <summary>获取玩家手持物品的电池组件（若有）。</summary>
        ItemBattery? GetHeldItemBattery(PlayerAvatar player);

        /// <summary>为手持电池充电（增加百分比）。</summary>
        void ChargeItemBattery(ItemBattery battery, int amountPercent);

        /// <summary>设置手持电池的电量百分比（绝对值）。</summary>
        void SetItemBatteryCharge(ItemBattery battery, int amountPercent);

        /// <summary>
        /// 获取玩家手持物品的 EnemyValuable 组件（若有）。
        /// 用于领队死亡奖励等场景，避免直接操作物理对象。
        /// </summary>
        EnemyValuable? GetHeldValuable(PlayerAvatar player);
    }
}