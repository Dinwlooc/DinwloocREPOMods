using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Helpers;
using UpgradeUninstaller.src.Core.Services;
using System.Collections.Generic;

namespace UpgradeUninstaller
{
    [BepInPlugin("Dinwlooc.UpgradeUninstaller", "UpgradeUninstaller", "1.0.0")]
    [BepInDependency("REPOLib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Dinwlooc.Common", BepInDependency.DependencyFlags.HardDependency)]
    public class UpgradeUninstaller : BaseUnityPlugin
    {
        internal static UpgradeUninstaller Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;

        private UninstallController _controller = null!;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            RegisterTranslations();

            UninstallConfig.Instance.Initialize(Config);

            var calculator = new UninstallCalculator();
            _controller = new UninstallController(calculator, Logger);

            MenuHelper.AddEscapeMenuButton(
                text: "Uninstall Upgrades",
                onClick: OnUninstallButtonClicked,
                enabledConfig: UninstallConfig.Instance.Enabled,
                posXConfig: UninstallConfig.Instance.PosX,
                posYConfig: UninstallConfig.Instance.PosY
            );

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} loaded!");
        }

        private void RegisterTranslations()
        {
            var translations = new Dictionary<string, string>
            {
                ["Uninstall Upgrades"] = "卸载升级",
                ["Uninstall Button Enabled"] = "显示卸载按钮",
                ["Uninstall Button Pos X"] = "按钮 X 偏移",
                ["Uninstall Button Pos Y"] = "按钮 Y 偏移"
            };

            TranslationManager.RegisterTranslations(
                Info.Metadata.GUID,
                "zh",
                1,
                translations
            );
        }

        private void OnUninstallButtonClicked()
        {
            if (!BridgeLocator.GameState.IsInTransit())
            {
                Logger.LogInfo("Uninstall only available in the truck (transit state).");
                return;
            }

            Logger.LogInfo("Uninstall button clicked, executing...");
            _controller.Execute();
        }

        private void OnDestroy()
        {
            // 如有需要可释放资源
        }
    }
}