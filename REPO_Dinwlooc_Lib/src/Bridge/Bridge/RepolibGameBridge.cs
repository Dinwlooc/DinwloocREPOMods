using System.Collections.Generic;
using REPOLib.Modules;

namespace Dinwlooc.Common.Bridge;

public class RepolibGameBridge : NativeGameBridge
{
    private static RepolibGameBridge? _instance;
    public static new RepolibGameBridge Instance => _instance ??= new RepolibGameBridge();

    private RepolibGameBridge() { }

    public override void RefreshUpgradeItemCache()
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

                // 1. REPOLib 自定义升级（优先）
                var repolibComp = prefab.GetComponent<REPOLibItemUpgrade>();
                if (repolibComp != null && !string.IsNullOrEmpty(repolibComp.UpgradeId))
                {
                    if (!_upgradeItemCache.ContainsKey(repolibComp.UpgradeId))
                        _upgradeItemCache[repolibComp.UpgradeId] = item;
                    continue;
                }

                // 2. 回退到原生升级（使用基类的定义）
                foreach (var kvp in UpgradeDefinitions.KeyToComponentType)
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
}