// 文件：Dinwlooc.Common.IBridge/IMoonUIBridge.cs
namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// 月相 UI 原子控制接口。
    /// 所有方法均为无状态调用，仅操作当前 UI 实例。
    /// </summary>
    public interface IMoonUIBridge
    {
        bool IsMoonUIActive();
        void ForceShowMoonUI();
        void RefreshMoonUI();
        void ForceHideMoonUI();
        void ResetMoonUIState();
        void CheckAndShowIfChanged();
    }
}