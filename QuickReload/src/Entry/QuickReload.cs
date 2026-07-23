using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace QuickReload
{
    [BepInPlugin("Dinwlooc.QuickReload", "QuickReload", "1.0.0")]
    [BepInDependency("nickklmao.menulib")]
    public class QuickReload : BaseUnityPlugin
    {
        internal static QuickReload Instance { get; private set; } = null!;
        public new static ManualLogSource Logger { get; private set; } = null!;

        public static ConfigEntry<bool>? ReloadRandomScene { get; private set; }

        public static ConfigEntry<bool>? ReloadButtonEnabled { get; private set; }
        public static ConfigEntry<bool>? ShopButtonEnabled { get; private set; }

        // 改为 int 类型，步长自动为 1
        public static ConfigEntry<int>? ReloadButtonPosX { get; private set; }
        public static ConfigEntry<int>? ReloadButtonPosY { get; private set; }
        public static ConfigEntry<int>? ShopButtonPosX { get; private set; }
        public static ConfigEntry<int>? ShopButtonPosY { get; private set; }

        private RepoGameBridge _gameBridge = null!;
        private QuickReloadService _service = null!;
        private QuickReloadMenuController _menuController = null!;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            gameObject.transform.parent = null;
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            ReloadRandomScene = Config.Bind(
                "General",
                "ReloadRandomScene",
                false,
                new ConfigDescription("启用时，在关卡/商店中“快速重载”会随机切换到同类型场景。")
            );

            ReloadButtonEnabled = Config.Bind(
                "UI",
                "ReloadButtonEnabled",
                true,
                new ConfigDescription("是否显示“快速重载”按钮。")
            );
            ShopButtonEnabled = Config.Bind(
                "UI",
                "ShopButtonEnabled",
                true,
                new ConfigDescription("是否显示“返回商店”按钮。")
            );

            ReloadButtonPosX = Config.Bind(
                "UI",
                "ReloadButtonPosX",
                176,
                new ConfigDescription("“快速重载”按钮的 X 偏移（整数）。")
            );
            ReloadButtonPosY = Config.Bind(
                "UI",
                "ReloadButtonPosY",
                125,
                new ConfigDescription("“快速重载”按钮的 Y 偏移（整数）。")
            );
            ShopButtonPosX = Config.Bind(
                "UI",
                "ShopButtonPosX",
                176,
                new ConfigDescription("“返回商店”按钮的 X 偏移（整数）。")
            );
            ShopButtonPosY = Config.Bind(
                "UI",
                "ShopButtonPosY",
                85,
                new ConfigDescription("“返回商店”按钮的 Y 偏移（整数）。")
            );

            _gameBridge = RepoGameBridge.Instance;
            _service = new QuickReloadService(_gameBridge);
            _menuController = new QuickReloadMenuController(_service, Logger);

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }
    }
}