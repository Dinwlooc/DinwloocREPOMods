using UnityEngine;

namespace Dinwlooc.Common.src.Bridge.IBridge;

public interface IItemBridge
{
    ItemBattery? GetHeldItemBattery(PlayerAvatar player);
    void ChargeItemBattery(ItemBattery battery, int amountPercent);
    void SetItemBatteryCharge(ItemBattery battery, int amountPercent);
}