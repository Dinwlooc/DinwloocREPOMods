using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using UnityEngine;
using System.Collections.Generic;

namespace QuickReload
{
    [BepInPlugin("Dinwlooc.QuickReload", "QuickReload", "1.0.0")]
    [BepInDependency("Dinwlooc.Common", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("nickklmao.menulib")]
    public class QuickReload : BaseUnityPlugin
    {
        internal static QuickReload Instance { get; private set; } = null!;
        public new static ManualLogSource Logger { get; private set; } = null!;

        private QuickReloadService _service = null!;
        private QuickReloadMenuController _menuController = null!;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            gameObject.transform.parent = null;
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            RegisterTranslations();

            QuickReloadConfig.Instance.Initialize(Config);

            var gameState = BridgeLocator.GameState;
            var saveLoad = BridgeLocator.SaveLoad;
            var network = BridgeLocator.Network;

            _service = new QuickReloadService(gameState, saveLoad, network);
            _menuController = new QuickReloadMenuController(_service, Logger);

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }

        private void RegisterTranslations()
        {
            var translations = new Dictionary<string, string>
            {
                ["Quick Reload"] = "快速重载",
                ["Go to Shop"] = "返回商店",
                ["Reload Random Scene"] = "随机切换场景",
                ["Reload Button Enabled"] = "显示重载按钮",
                ["Reload Button Pos X"] = "重载按钮 X 偏移",
                ["Reload Button Pos Y"] = "重载按钮 Y 偏移",
                ["Shop Button Enabled"] = "显示商店按钮",
                ["Shop Button Pos X"] = "商店按钮 X 偏移",
                ["Shop Button Pos Y"] = "商店按钮 Y 偏移"
            };

            TranslationManager.RegisterTranslations(
                Info.Metadata.GUID,
                "zh",
                1,
                translations
            );
        }
    }
}