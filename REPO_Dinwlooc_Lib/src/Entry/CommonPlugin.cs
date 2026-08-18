// 文件：Dinwlooc.Common.Core/CommonPlugin.cs
using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("nickklmao.menulib", BepInDependency.DependencyFlags.SoftDependency)]
    // 注意：AutoHookGenPatcher 作为 Patcher 安装，不在插件加载列表中，因此无须 BepInDependency。
    // DRL 通过类型检测和 MMHOOK 文件检测判断其可用性。
    public class CommonPlugin : BaseUnityPlugin
    {
        internal new static ManualLogSource Logger { get; private set; } = null;

        // ---------- 配置项 ----------
        public static ConfigEntry<bool> UseHookAcceleration { get; private set; }

        // ---------- 运行时检测结果 ----------
        public static bool IsHookGenAvailable { get; private set; }

        // ---------- 常量 ----------
        private const string ConfigSectionPerformance = "Performance";
        private const string ConfigKeyUseHookAcceleration = "UseHookAcceleration";
        private const string ConfigDescriptionUseHookAcceleration =
            "启用后，DRL 会在首次订阅事件时尝试使用基于 AutoHookGenPatcher 的高性能 Hook 生成器（需安装 AutoHookGenPatcher）。若不可用，则自动降级为轮询模式。";

        // 注意：程序集名称应为 "BepInEx.MonoMod.AutoHookGenPatcher"（日志中可见）
        private const string AutoHookGenPatcherTypeName = "AutoHookGenPatcher.Patcher, BepInEx.MonoMod.AutoHookGenPatcher";
        private const string MMHOOK_AssemblyCSharp_RelativePath = "MMHOOK/MMHOOK_Assembly-CSharp.dll";

        private void Awake()
        {
            Logger = base.Logger;

            // ---- 1. 绑定配置项 ----
            UseHookAcceleration = Config.Bind(
                ConfigSectionPerformance,
                ConfigKeyUseHookAcceleration,
                false,
                ConfigDescriptionUseHookAcceleration
            );

            // ---- 2. 检测 HookGen 是否可用 ----
            IsHookGenAvailable = CheckHookGenAvailability();

            if (UseHookAcceleration.Value && !IsHookGenAvailable)
            {
                Logger.LogWarning(
                    "AutoHookGenPatcher is not detected. Hook acceleration is disabled. " +
                    "To enable high-performance event generation, ensure AutoHookGenPatcher is installed as a BepInEx Patcher (in BepInEx/patchers/) and has generated MMHOOK_Assembly-CSharp.dll."
                );
            }
            else if (UseHookAcceleration.Value && IsHookGenAvailable)
            {
                Logger.LogInfo("Hook acceleration is available and enabled.");
            }

            // ---- 3. 注册桥接（懒加载工厂） ----
            BridgeLocator.Register<IGameStateBridge>(() => CoreBridge.Instance);
            BridgeLocator.Register<ISaveLoadBridge>(() => CoreBridge.Instance);
            BridgeLocator.Register<INetworkBridge>(() => CoreBridge.Instance);

            BridgeLocator.Register<IItemBridge>(() => ItemBridge.Instance);
            BridgeLocator.Register<IHealthPackBridge>(() => ItemBridge.Instance);

            BridgeLocator.Register<IPlayerBridge>(() => PlayerBridge.Instance);
            BridgeLocator.Register<IEnergyBridge>(() => PlayerBridge.Instance);

            BridgeLocator.Register<ITruckBridge>(() => TruckBridge.Instance);
            BridgeLocator.Register<IUpgradeBridge>(() => UpgradeBridge.Instance);
            BridgeLocator.Register<IEnemyBridge>(() => EnemyBridge.Instance);
            BridgeLocator.Register<ISlideBridge>(() => SlideBridge.Instance);
            BridgeLocator.Register<IMenuBridge>(() => MenuBridge.Instance);
            BridgeLocator.Register<IEnemyModifierBridge>(() => EnemyBridge.Instance);
            BridgeLocator.Register<IEnemyHealthBridge>(() => EnemyBridge.Instance);
            BridgeLocator.Register<IMovementOverrideBridge>(() => MovementOverrideBridge.Instance);

            BridgeLocator.Register<IMoonBridge>(() => MoonBridge.Instance);
            BridgeLocator.Register<IMoonUIBridge>(() => MoonBridge.Instance);

            // ---- 4. 公共服务挂载（MonoBehaviour 必须主动创建） ----
            GameObject serviceObject = new GameObject(nameof(CommonService));
            DontDestroyOnLoad(serviceObject);
            serviceObject.AddComponent<CommonService>();

            Logger.LogInfo(string.Format("{0} v{1} loaded.", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION));
        }

        // ---------- 检测逻辑 ----------
        private static bool CheckHookGenAvailability()
        {
            // 方法 1：检测 AutoHookGenPatcher.Patcher 类型是否存在（需要正确的程序集名称）
            Type patcherType = Type.GetType(AutoHookGenPatcherTypeName, false);
            if (patcherType != null)
            {
                Logger.LogDebug("Detected AutoHookGenPatcher.Patcher type in AppDomain.");
                return true;
            }

            // 方法 2：检测 MMHOOK_Assembly-CSharp.dll 文件是否存在
            string mmhookFullPath = Path.Combine(Paths.PluginPath, MMHOOK_AssemblyCSharp_RelativePath);
            if (File.Exists(mmhookFullPath))
            {
                Logger.LogDebug($"Detected MMHOOK file at: {mmhookFullPath}");
                return true;
            }

            // 可选：扫描整个 plugins/MMHOOK 目录，防止文件名差异
            string mmhookFolder = Path.Combine(Paths.PluginPath, "MMHOOK");
            if (Directory.Exists(mmhookFolder))
            {
                string[] mmhookFiles = Directory.GetFiles(mmhookFolder, "MMHOOK_*.dll", SearchOption.TopDirectoryOnly);
                if (mmhookFiles.Length > 0)
                {
                    Logger.LogDebug($"Found {mmhookFiles.Length} MMHOOK file(s) in {mmhookFolder}, assuming HookGen available.");
                    return true;
                }
            }

            return false;
        }
    }
}