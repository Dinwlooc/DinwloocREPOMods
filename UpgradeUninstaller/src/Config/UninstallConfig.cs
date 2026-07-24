using BepInEx.Configuration;
using Dinwlooc.Common.Helpers;

namespace UpgradeUninstaller
{
    public class UninstallConfig : MenuConfigBase<UninstallConfig>
    {
        public override void Bind(ConfigFile config)
        {
            // 完全自定义键名和默认值，不调用 base.Bind
            Enabled = config.Bind("UI", "UninstallButtonEnabled", true,
                "是否显示“卸载升级”按钮。");
            PosX = config.Bind("UI", "UninstallButtonPosX", 276,
                "按钮 X 偏移（整数）。");
            PosY = config.Bind("UI", "UninstallButtonPosY", 65,
                "按钮 Y 偏移（整数）。");
        }
    }
}