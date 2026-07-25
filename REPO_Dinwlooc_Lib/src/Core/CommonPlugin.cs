// Dinwlooc.Common/Core/CommonPlugin.cs
using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.src.Bridge.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("nickklmao.menulib")]
    public class CommonPlugin : BaseUnityPlugin
    {
        internal new static ManualLogSource Logger { get; private set; } = null!;

        private void Awake()
        {
            Logger = base.Logger;

            // 注册所有桥接器（单例）
            BridgeLocator.Register<IGameStateBridge>(CoreBridge.Instance);
            BridgeLocator.Register<ISaveLoadBridge>(CoreBridge.Instance);
            BridgeLocator.Register<INetworkBridge>(CoreBridge.Instance);

            BridgeLocator.Register<IItemBridge>(ItemBridge.Instance);
            BridgeLocator.Register<IHealthPackBridge>(ItemBridge.Instance);

            BridgeLocator.Register<IPlayerBridge>(PlayerBridge.Instance);
            BridgeLocator.Register<IEnergyBridge>(PlayerBridge.Instance);

            BridgeLocator.Register<ITruckBridge>(TruckBridge.Instance);
            BridgeLocator.Register<IUpgradeBridge>(UpgradeBridge.Instance);
            BridgeLocator.Register<IEnemyBridge>(EnemyBridge.Instance);

            // 挂载公共服务
            var go = new GameObject(nameof(CommonService));
            DontDestroyOnLoad(go);
            go.AddComponent<CommonService>();

            Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded.");
        }
    }
}