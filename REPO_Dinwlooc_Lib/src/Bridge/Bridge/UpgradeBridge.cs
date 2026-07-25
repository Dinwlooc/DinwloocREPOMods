// Dinwlooc.Common/Bridge/UpgradeBridge.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using Dinwlooc.Common.src.Bridge.IBridge;
using REPOLib.Modules;  // 软引用，如果不存在则忽略

namespace Dinwlooc.Common.Bridge
{
    public class UpgradeBridge : IUpgradeBridge
    {
        private static UpgradeBridge? _instance;
        public static UpgradeBridge Instance => _instance ??= new UpgradeBridge();
        private UpgradeBridge() { }

        private Dictionary<string, Item>? _upgradeItemCache;
        private readonly object _cacheLock = new();

        // 原生升级类型映射
        private static readonly Dictionary<string, string> KeyToComponentType = new()
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

        public void RefreshUpgradeItemCache()
        {
            lock (_cacheLock)
            {
                _upgradeItemCache = new Dictionary<string, Item>();
                if (StatsManager.instance == null) return;

                foreach (var entry in StatsManager.instance.itemDictionary)
                {
                    var item = entry.Value;
                    if (item.itemType != SemiFunc.itemType.item_upgrade) continue;
                    var prefab = item.prefab?.Prefab;
                    if (prefab == null) continue;

                    // 尝试 REPOLib 自定义升级
                    var repolibComp = prefab.GetComponent<REPOLibItemUpgrade>();
                    if (repolibComp != null && !string.IsNullOrEmpty(repolibComp.UpgradeId))
                    {
                        if (!_upgradeItemCache.ContainsKey(repolibComp.UpgradeId))
                            _upgradeItemCache[repolibComp.UpgradeId] = item;
                        continue;
                    }

                    // 原生升级匹配
                    foreach (var kvp in KeyToComponentType)
                    {
                        if (_upgradeItemCache.ContainsKey(kvp.Key)) continue;
                        var type = typeof(PlayerAvatar).Assembly.GetType(kvp.Value);
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
            var raw = StatsManager.instance?.FetchPlayerUpgrades(steamID);
            return raw != null ? new Dictionary<string, int>(raw) : new Dictionary<string, int>();
        }

        public Item? FindItemByUpgradeKey(string upgradeKey)
        {
            if (_upgradeItemCache == null) RefreshUpgradeItemCache();
            return _upgradeItemCache?.TryGetValue(upgradeKey, out var item) == true ? item : null;
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