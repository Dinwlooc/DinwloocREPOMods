// 文件：Dinwlooc.Common.Core/CommonPlugin.cs
using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("nickklmao.menulib", BepInDependency.DependencyFlags.SoftDependency)]
    public class CommonPlugin : BaseUnityPlugin
    {
        internal new static ManualLogSource Logger { get; private set; } = null;

        private void Awake()
        {
            Logger = base.Logger;

            // ---- 核心桥接（懒加载工厂） ----
            BridgeLocator.Register<IGameStateBridge>(() => CoreBridge.Instance);
            BridgeLocator.Register<ISaveLoadBridge>(() => CoreBridge.Instance);
            BridgeLocator.Register<INetworkBridge>(() => CoreBridge.Instance);

            // ---- 物品桥接 ----
            BridgeLocator.Register<IItemBridge>(() => ItemBridge.Instance);
            BridgeLocator.Register<IHealthPackBridge>(() => ItemBridge.Instance);

            // ---- 玩家桥接 ----
            BridgeLocator.Register<IPlayerBridge>(() => PlayerBridge.Instance);
            BridgeLocator.Register<IEnergyBridge>(() => PlayerBridge.Instance);

            // ---- 功能桥接 ----
            BridgeLocator.Register<ITruckBridge>(() => TruckBridge.Instance);
            BridgeLocator.Register<IUpgradeBridge>(() => UpgradeBridge.Instance);
            BridgeLocator.Register<IEnemyBridge>(() => EnemyBridge.Instance);
            BridgeLocator.Register<ISlideBridge>(() => SlideBridge.Instance);
            BridgeLocator.Register<IMenuBridge>(() => MenuBridge.Instance);
            BridgeLocator.Register<IEnemyModifierBridge>(() => EnemyModifierBridge.Instance);
            BridgeLocator.Register<IMovementOverrideBridge>(() => MovementOverrideBridge.Instance);

            // ---- 月相桥接（懒加载） ----
            BridgeLocator.Register<IMoonBridge>(() => MoonBridge.Instance);
            BridgeLocator.Register<IMoonUIBridge>(() => MoonBridge.Instance);

            // ---- 公共服务挂载（MonoBehaviour 必须主动创建） ----
            GameObject serviceObject = new GameObject(nameof(CommonService));
            DontDestroyOnLoad(serviceObject);
            serviceObject.AddComponent<CommonService>();

            Logger.LogInfo(string.Format("{0} v{1} loaded.", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION));
        }
    }
}