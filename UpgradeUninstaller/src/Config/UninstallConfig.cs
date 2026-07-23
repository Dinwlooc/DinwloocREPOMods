using BepInEx.Configuration;
using Dinwlooc.Common.Helpers;

namespace UpgradeUninstaller
{
    public class UninstallConfig : ModConfig<UninstallConfig>
    {
        protected override void Bind(ConfigFile config)
        {
            // 直接为基类属性赋值，覆盖默认键名和默认值
            Enabled = config.Bind("UI", "UninstallButtonEnabled", true,
                "是否显示“卸载升级”按钮。");
            PosX = config.Bind("UI", "UninstallButtonPosX", 276,
                "按钮 X 偏移（整数）。");
            PosY = config.Bind("UI", "UninstallButtonPosY", 65,
                "按钮 Y 偏移（整数）。");
        }
    }
}