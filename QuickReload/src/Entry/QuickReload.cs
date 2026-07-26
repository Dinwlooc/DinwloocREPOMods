using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace QuickReload
{
    [BepInPlugin("Dinwlooc.QuickReload", "QuickReload", "1.0.0")]
    [BepInDependency("Dinwlooc.Common", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("nickklmao.menulib")] // 用于 MenuHelper
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

            // 初始化配置
            QuickReloadConfig.Instance.Initialize(Config);

            // 通过 BridgeLocator 获取所需桥接接口
            var gameState = BridgeLocator.GameState;
            var saveLoad = BridgeLocator.SaveLoad;
            var network = BridgeLocator.Network;

            _service = new QuickReloadService(gameState, saveLoad, network);
            _menuController = new QuickReloadMenuController(_service, Logger);

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }
    }
}