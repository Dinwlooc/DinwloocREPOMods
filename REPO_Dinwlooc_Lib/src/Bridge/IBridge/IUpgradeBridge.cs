using System.Collections.Generic;

namespace Dinwlooc.Common.IBridge;

/// <summary>
/// 升级系统桥接，依赖 REPOLib。
/// </summary>
public interface IUpgradeBridge
{
    void RefreshUpgradeItemCache();
    Dictionary<string, int> FetchUpgrades(string steamID);
    Item? FindItemByUpgradeKey(string upgradeKey);
    void ClearUpgradeStat(string steamID, string upgradeKey);
    void AddPurchasedItem(string itemName, int count);
}