using System;
using BepInEx.Configuration;
using Dinwlooc.Common.Bridge;

namespace Dinwlooc.Common.Helpers
{
    public static class MenuHelper
    {
        /// <summary>
        /// 向 ESC 菜单添加按钮，若 MenuLib 未安装则静默忽略。
        /// </summary>
        public static void AddEscapeMenuButton(
            string text,
            Action onClick,
            ConfigEntry<bool>? enabledConfig = null,
            ConfigEntry<int>? posXConfig = null,
            ConfigEntry<int>? posYConfig = null)
        {
            BridgeLocator.Menu.AddEscapeMenuButton(text, onClick, enabledConfig, posXConfig, posYConfig);
        }
    }
}