using BepInEx.Logging;
using MenuLib;
using UnityEngine;

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

            if (QuickReload.ReloadButtonEnabled?.Value ?? true)
            {
                float posX = QuickReload.ReloadButtonPosX?.Value ?? 216;
                float posY = QuickReload.ReloadButtonPosY?.Value ?? 125;
                MenuAPI.AddElementToEscapeMenu((parent) =>
                {
                    var button = MenuAPI.CreateREPOButton(
                        "快速重载",
                        OnQuickReloadClicked,
                        parent,
                        new Vector2(posX, posY)
                    );
                    button.gameObject.SetActive(true);
                });
            }

            if (QuickReload.ShopButtonEnabled?.Value ?? true)
            {
                float posX = QuickReload.ShopButtonPosX?.Value ?? 216;
                float posY = QuickReload.ShopButtonPosY?.Value ?? 85;
                MenuAPI.AddElementToEscapeMenu((parent) =>
                {
                    var button = MenuAPI.CreateREPOButton(
                        "返回商店",
                        OnGoToShopClicked,
                        parent,
                        new Vector2(posX, posY)
                    );
                    button.gameObject.SetActive(true);
                });
            }
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