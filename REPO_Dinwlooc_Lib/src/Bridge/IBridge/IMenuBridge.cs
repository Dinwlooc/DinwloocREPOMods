using BepInEx.Configuration;

namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// UI 菜单桥接接口，旨在提供与 MenuLib 核心功能一致的能力抽象。
    /// 当前仅实现 ESC 菜单按钮，未来可扩展窗口、标签、输入框等。
    /// 若 MenuLib 不存在，所有操作静默忽略。
    /// </summary>
    public interface IMenuBridge
    {
        /// <summary>
        /// 向 ESC 菜单添加一个按钮。
        /// </summary>
        /// <param name="text">按钮文字</param>
        /// <param name="onClick">点击回调</param>
        /// <param name="enabledConfig">可选，若配置值为 false 则不添加</param>
        /// <param name="posXConfig">可选，X 偏移</param>
        /// <param name="posYConfig">可选，Y 偏移</param>
        void AddEscapeMenuButton(
            string text,
            System.Action onClick,
            ConfigEntry<bool>? enabledConfig = null,
            ConfigEntry<int>? posXConfig = null,
            ConfigEntry<int>? posYConfig = null);
    }
}