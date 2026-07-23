using System.Collections.Generic;

namespace UpgradeUninstaller
{
	/// <summary>
	/// 卸载计算后的结果 DTO（纯数据）
	/// </summary>
	public class UninstallResult
	{
		// 玩家 -> 新的血量值
		public Dictionary<string, int> NewHealthMap { get; set; } = new Dictionary<string, int>();
		// 玩家 -> 需要清零的升级 Key 列表（包括所有非血量升级，以及被拆完的血量升级）
		public Dictionary<string, List<string>> StatsToClear { get; set; } = new Dictionary<string, List<string>>();
		// 升级 Key -> 需要返还的升级物品总数量（汇总所有玩家）
		public Dictionary<string, int> TotalItemsToRefund { get; set; } = new Dictionary<string, int>();
	}
}