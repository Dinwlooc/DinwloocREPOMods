using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Helpers;
using UpgradeUninstaller.src.Core.Services;

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

            // 初始化配置
            UninstallConfig.Instance.Initialize(Config);

            // 创建控制器
            var calculator = new UninstallCalculator();
            _controller = new UninstallController(calculator, Logger);

            // 添加菜单按钮
            MenuHelper.AddEscapeMenuButton(
                text: "卸载升级",
                onClick: OnUninstallButtonClicked,
                enabledConfig: UninstallConfig.Instance.Enabled,
                posXConfig: UninstallConfig.Instance.PosX,
                posYConfig: UninstallConfig.Instance.PosY
            );

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} loaded!");
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