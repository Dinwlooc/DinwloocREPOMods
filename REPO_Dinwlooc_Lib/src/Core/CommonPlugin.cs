using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Bridge;
using UnityEngine;

namespace Dinwlooc.Common.Core;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("nickklmao.menulib")]
public class CommonPlugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger { get; private set; } = null!;

    private void Awake()
    {
        Logger = base.Logger;

        // 1. 初始化桥接（通过 BridgeLocator 自动选择原生或 REPOLib 增强实现）
        _ = BridgeLocator.Player; // 触发静态构造和检测

        // 2. 挂载公共服务（MonoBehaviour 宿主）
        var go = new GameObject(nameof(CommonService));
        DontDestroyOnLoad(go);
        go.AddComponent<CommonService>();

        Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded.");
    }
}