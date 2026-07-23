using System;
using System.Collections.Generic;

namespace UpgradeUninstaller
{
    public class UninstallCalculator
    {
        public UninstallResult Calculate(
            Dictionary<string, Dictionary<string, int>> allUpgrades,
            Dictionary<string, int> currentHealthMap)
        {
            var result = new UninstallResult();

            foreach (var kvp in allUpgrades)
            {
                string steamID = kvp.Key;
                var upgrades = kvp.Value;
                var healthUninstalled = 0;
                // 初始化结果中的数据结构
                result.NewHealthMap[steamID] = currentHealthMap.GetValueOrDefault(steamID, 100);
                result.StatsToClear[steamID] = new List<string>();
                // ---------- 处理血量升级 ----------
                if (upgrades.TryGetValue("playerUpgradeHealth", out int healthLevel) && healthLevel > 0)
                {
                    int currentHP = result.NewHealthMap[steamID];
                    // 最多拆到剩下 1 点血（每级 +20 HP）
                    int maxCanUninstall = Math.Max(0, (currentHP - 1) / 20);
                    int actualUninstall = Math.Min(healthLevel, maxCanUninstall);
                    healthUninstalled = actualUninstall;
                    // 更新血量
                    result.NewHealthMap[steamID] = currentHP - actualUninstall * 20;
                    // 计算剩余的血量等级
                    int remainingHealthLevel = healthLevel - actualUninstall;
                    if (remainingHealthLevel <= 0)
                    {
                        // 拆完了，需要把该玩家的血量升级记录清零
                        result.StatsToClear[steamID].Add("playerUpgradeHealth");
                    }
                    // 如果 remainingHealthLevel > 0，则不用清零（保留剩余等级），也不额外处理
                    // 无论是否拆完，实际卸掉的数量都要退还成升级物品
                    if (actualUninstall > 0)
                    {
                        if (!result.TotalItemsToRefund.ContainsKey("playerUpgradeHealth"))
                            result.TotalItemsToRefund["playerUpgradeHealth"] = 0;
                        result.TotalItemsToRefund["playerUpgradeHealth"] += actualUninstall;
                    }
                }
                // ---------- 处理其他升级（非血量） ----------
                foreach (var up in upgrades)
                {
                    string key = up.Key;
                    int level = up.Value;
                    if (key == "playerUpgradeHealth") continue; // 已处理
                    if (level <= 0) continue;
                    // 非血量升级：全部卸掉，返还对应数量的升级物品，并标记为需要清零
                    result.StatsToClear[steamID].Add(key);

                    if (!result.TotalItemsToRefund.ContainsKey(key))
                        result.TotalItemsToRefund[key] = 0;
                    result.TotalItemsToRefund[key] += level;
                }
            }
            return result;
        }
    }
}