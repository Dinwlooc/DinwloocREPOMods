using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;   // 新增：EventBus
using Dinwlooc.Common.Events;

namespace UpgradeUninstaller.src.Core.Services
{
    public class UninstallController
    {
        private readonly UninstallCalculator _calculator;
        private readonly ManualLogSource _logger;

        public UninstallController(UninstallCalculator calculator, ManualLogSource logger)
        {
            _calculator = calculator;
            _logger = logger;
        }

        public void Execute()
        {
            // 1. 权限校验
            if (!BridgeLocator.GameState.IsMasterClientOrSingleplayer())
            {
                _logger.LogWarning("Only the host can uninstall upgrades for all players.");
                return;
            }

            var upgradeBridge = BridgeLocator.Upgrade;
            if (upgradeBridge == null)
            {
                _logger.LogError("Upgrade bridge not available.");
                return;
            }

            // 2. 刷新升级物品缓存
            upgradeBridge.RefreshUpgradeItemCache();

            // 3. 获取所有玩家数据
            var playerHealth = StatsManager.instance.playerHealth;
            if (playerHealth == null || playerHealth.Count == 0)
            {
                _logger.LogError("No players found in playerHealth.");
                return;
            }

            var allUpgrades = new Dictionary<string, Dictionary<string, int>>();
            var currentHealthMap = new Dictionary<string, int>();

            foreach (var kvp in playerHealth)
            {
                string steamID = kvp.Key;
                int currentHP = kvp.Value;

                var upgrades = upgradeBridge.FetchUpgrades(steamID);
                if (upgrades.Count > 0)
                    allUpgrades[steamID] = upgrades;

                currentHealthMap[steamID] = currentHP;
            }

            if (allUpgrades.Count == 0)
            {
                _logger.LogInfo("No upgrades to uninstall.");
                return;
            }

            // 4. 过滤掉没有对应可购买物品的升级
            var filteredUpgrades = new Dictionary<string, Dictionary<string, int>>();
            foreach (var kvp in allUpgrades)
            {
                string steamID = kvp.Key;
                var upgrades = kvp.Value;
                var validUpgrades = new Dictionary<string, int>();

                foreach (var up in upgrades)
                {
                    string key = up.Key;
                    int level = up.Value;
                    if (upgradeBridge.FindItemByUpgradeKey(key) != null)
                        validUpgrades[key] = level;
                    else
                        _logger.LogWarning($"Upgrade '{key}' has no purchasable item, skipping uninstall for player {steamID}.");
                }

                if (validUpgrades.Count > 0)
                    filteredUpgrades[steamID] = validUpgrades;
            }

            if (filteredUpgrades.Count == 0)
            {
                _logger.LogInfo("No valid upgrades to uninstall (all missing items).");
                return;
            }

            // 5. 核心计算
            var result = _calculator.Calculate(filteredUpgrades, currentHealthMap);

            // 6. 应用变更
            var playerBridge = BridgeLocator.Player;
            foreach (var hpKvp in result.NewHealthMap)
            {
                string steamID = hpKvp.Key;
                int newHP = hpKvp.Value;
                playerBridge.SetPlayerHP(steamID, newHP);
                _logger.LogInfo($"Player {steamID}: HP set to {newHP}.");
            }

            foreach (var clearKvp in result.StatsToClear)
            {
                string steamID = clearKvp.Key;
                foreach (string key in clearKvp.Value)
                {
                    upgradeBridge.ClearUpgradeStat(steamID, key);
                    _logger.LogInfo($"Cleared {key} for player {steamID}.");
                }
            }

            foreach (var refundKvp in result.TotalItemsToRefund)
            {
                string upgradeKey = refundKvp.Key;
                int totalCount = refundKvp.Value;
                if (totalCount <= 0) continue;

                var item = upgradeBridge.FindItemByUpgradeKey(upgradeKey);
                if (item == null)
                {
                    _logger.LogWarning($"Cannot find Item for {upgradeKey}, skipping refund.");
                    continue;
                }

                upgradeBridge.AddPurchasedItem(item.name, totalCount);
                _logger.LogInfo($"Refunded {totalCount} x {item.name}.");
            }

            // 7. 保存并同步
            BridgeLocator.SaveLoad.SaveCurrentProgress();
            BridgeLocator.Network.SyncDictionariesToClients();

            // 8. 发布事件（在场景重载前，供其他模块响应）
            EventBus.Publish(new UpgradeUninstalledEvent(result.TotalItemsToRefund));

            // 9. 重载场景
            BridgeLocator.SaveLoad.RestartScene();
            _logger.LogInfo("Upgrade uninstall completed. Scene reloading...");
        }
    }
}