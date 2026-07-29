using BepInEx.Logging;
using Dinwlooc.Common.Helpers;

namespace QuickReload
{
    public class QuickReloadMenuController
    {
        private readonly QuickReloadService _service;
        private readonly ManualLogSource _logger;

        public QuickReloadMenuController(QuickReloadService service, ManualLogSource logger)
        {
            _service = service;
            _logger = logger;

            var config = QuickReloadConfig.Instance;

            // 添加“快速重载”按钮（文本使用英文，便于翻译插件识别）
            MenuHelper.AddEscapeMenuButton(
                text: "Quick Reload",
                onClick: OnQuickReloadClicked,
                enabledConfig: config.ReloadButtonEnabled,
                posXConfig: config.ReloadButtonPosX,
                posYConfig: config.ReloadButtonPosY
            );

            // 添加“返回商店”按钮（文本使用英文）
            MenuHelper.AddEscapeMenuButton(
                text: "Go to Shop",
                onClick: OnGoToShopClicked,
                enabledConfig: config.ShopButtonEnabled,
                posXConfig: config.ShopButtonPosX,
                posYConfig: config.ShopButtonPosY
            );
        }

        private void OnQuickReloadClicked()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                _logger.LogWarning("只有主机或单人模式可以使用快速重载。");
                return;
            }

            if (SemiFunc.IsMainMenu())
            {
                _logger.LogWarning("主菜单不能进行场景重载。");
                return;
            }

            _service.ReloadCurrentScene();
        }

        private void OnGoToShopClicked()
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                _logger.LogWarning("只有主机或单人模式可以返回商店。");
                return;
            }

            if (SemiFunc.IsMainMenu())
            {
                _logger.LogWarning("主菜单不能返回商店。");
                return;
            }

            if (SemiFunc.RunIsShop())
            {
                _logger.LogInfo("已在商店中。");
                return;
            }

            _service.GoToShop();
        }
    }
}