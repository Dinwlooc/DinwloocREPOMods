using BepInEx.Configuration;
using Dinwlooc.Common.Helpers;

namespace QuickReload
{
    public class QuickReloadConfig : ConfigBase<QuickReloadConfig>
    {
        // 随机切换开关
        public ConfigEntry<bool> ReloadRandomScene { get; private set; } = null!;

        // 重载按钮
        public ConfigEntry<bool> ReloadButtonEnabled { get; private set; } = null!;
        public ConfigEntry<int> ReloadButtonPosX { get; private set; } = null!;
        public ConfigEntry<int> ReloadButtonPosY { get; private set; } = null!;

        // 商店按钮
        public ConfigEntry<bool> ShopButtonEnabled { get; private set; } = null!;
        public ConfigEntry<int> ShopButtonPosX { get; private set; } = null!;
        public ConfigEntry<int> ShopButtonPosY { get; private set; } = null!;

        public override void Bind(ConfigFile config)
        {
            base.Bind(config); // 总开关（Enabled）

            ReloadRandomScene = config.Bind(
                "General",
                "ReloadRandomScene",
                false,
                "启用时，在关卡/商店中“快速重载”会随机切换到同类型场景。"
            );

            ReloadButtonEnabled = config.Bind(
                "UI",
                "ReloadButtonEnabled",
                true,
                "是否显示“快速重载”按钮。"
            );
            ReloadButtonPosX = config.Bind(
                "UI",
                "ReloadButtonPosX",
                176,
                "“快速重载”按钮的 X 偏移。"
            );
            ReloadButtonPosY = config.Bind(
                "UI",
                "ReloadButtonPosY",
                125,
                "“快速重载”按钮的 Y 偏移。"
            );

            ShopButtonEnabled = config.Bind(
                "UI",
                "ShopButtonEnabled",
                true,
                "是否显示“返回商店”按钮。"
            );
            ShopButtonPosX = config.Bind(
                "UI",
                "ShopButtonPosX",
                176,
                "“返回商店”按钮的 X 偏移。"
            );
            ShopButtonPosY = config.Bind(
                "UI",
                "ShopButtonPosY",
                85,
                "“返回商店”按钮的 Y 偏移。"
            );
        }
    }
}