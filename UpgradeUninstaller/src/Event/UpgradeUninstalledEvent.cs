using System.Collections.Generic;

namespace UpgradeUninstaller
{
    /// <summary>
    /// 当升级卸载完成并返还物品后发布的事件
    /// </summary>
    public readonly struct UpgradeUninstalledEvent
    {
        /// <summary>升级Key -> 返还总数</summary>
        public readonly IReadOnlyDictionary<string, int> RefundedItems;

        public UpgradeUninstalledEvent(Dictionary<string, int> refundedItems)
        {
            RefundedItems = refundedItems;
        }
    }
}