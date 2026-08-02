using System;
using System.Collections.Generic;
using System.Reflection;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using REPOLib.Modules;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    public class UpgradeBridge : BridgeSingleton<UpgradeBridge>, IUpgradeBridge
    {
        private Dictionary<string, Item> _upgradeItemCache;
        private readonly object _cacheLock = new object();

        private static readonly Dictionary<string, string> KeyToComponentType = new Dictionary<string, string>
        {
            { "playerUpgradeHealth", "ItemUpgradePlayerHealth" },
            { "playerUpgradeStamina", "ItemUpgradePlayerEnergy" },
            { "playerUpgradeExtraJump", "ItemUpgradePlayerExtraJump" },
            { "playerUpgradeSpeed", "ItemUpgradePlayerSprintSpeed" },
            { "playerUpgradeStrength", "ItemUpgradePlayerGrabStrength" },
            { "playerUpgradeRange", "ItemUpgradePlayerGrabRange" },
            { "playerUpgradeThrow", "ItemUpgradePlayerThrowStrength" },
            { "playerUpgradeLaunch", "ItemUpgradePlayerTumbleLaunch" },
            { "playerUpgradeTumbleClimb", "ItemUpgradePlayerTumbleClimb" },
            { "playerUpgradeCrouchRest", "ItemUpgradePlayerCrouchRest" },
            { "playerUpgradeDeathHeadBattery", "ItemUpgradeDeathHeadBattery" },
            { "playerUpgradeTumbleWings", "ItemUpgradePlayerTumbleWings" },
            { "playerUpgradeMapPlayerCount", "ItemUpgradeMapPlayerCount" },
        };

        private UpgradeBridge() { }

        public void RefreshUpgradeItemCache()
        {
            lock (_cacheLock)
            {
                _upgradeItemCache = new Dictionary<string, Item>();
                if (StatsManager.instance == null) return;

                // 修正：StatsManager.itemDictionary 实际类型为 Dictionary<string, Item>
                foreach (KeyValuePair<string, Item> entry in StatsManager.instance.itemDictionary)
                {
                    Item item = entry.Value;
                    if (item.itemType != SemiFunc.itemType.item_upgrade) continue;
                    GameObject prefab = item.prefab?.Prefab;
                    if (prefab == null) continue;

                    REPOLibItemUpgrade repolibComp = prefab.GetComponent<REPOLibItemUpgrade>();
                    if (repolibComp != null && !string.IsNullOrEmpty(repolibComp.UpgradeId))
                    {
                        if (!_upgradeItemCache.ContainsKey(repolibComp.UpgradeId))
                            _upgradeItemCache[repolibComp.UpgradeId] = item;
                        continue;
                    }

                    foreach (KeyValuePair<string, string> kvp in KeyToComponentType)
                    {
                        if (_upgradeItemCache.ContainsKey(kvp.Key)) continue;
                        Type type = typeof(PlayerAvatar).Assembly.GetType(kvp.Value);
                        if (type != null && prefab.GetComponent(type) != null)
                        {
                            _upgradeItemCache[kvp.Key] = item;
                            break;
                        }
                    }
                }
            }
        }

        public Dictionary<string, int> FetchUpgrades(string steamID)
        {
            Dictionary<string, int> raw = StatsManager.instance?.FetchPlayerUpgrades(steamID);
            return raw != null ? new Dictionary<string, int>(raw) : new Dictionary<string, int>();
        }

        public Item FindItemByUpgradeKey(string upgradeKey)
        {
            if (_upgradeItemCache == null) RefreshUpgradeItemCache();
            return _upgradeItemCache?.TryGetValue(upgradeKey, out Item item) == true ? item : null;
        }

        public void ClearUpgradeStat(string steamID, string upgradeKey)
        {
            if (PunManager.instance != null)
                PunManager.instance.UpdateStat(upgradeKey, steamID, 0);
        }

        public void AddPurchasedItem(string itemName, int count)
        {
            if (count <= 0 || StatsManager.instance == null) return;
            int current = StatsManager.instance.itemsPurchased.TryGetValue(itemName, out int val) ? val : 0;
            StatsManager.instance.itemsPurchased[itemName] = current + count;
        }
    }
}