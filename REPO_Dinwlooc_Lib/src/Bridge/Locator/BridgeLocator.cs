using System;
using System.Collections.Generic;
using Dinwlooc.Common.src.Bridge.IBridge;

namespace Dinwlooc.Common.Bridge;

public static class BridgeLocator
{
    private static readonly Dictionary<Type, object> _customBridges = new();
    private static NativeGameBridge? _instance;
    private static readonly object _lock = new();

    private static NativeGameBridge DefaultInstance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        try
                        {
                            var repolibType = Type.GetType("REPOLib.Modules.REPOLibItemUpgrade, REPOLib");
                            if (repolibType != null)
                            {
                                _instance = RepolibGameBridge.Instance;
                                Core.CommonPlugin.Logger.LogInfo("Using REPOLib-enhanced game bridge.");
                            }
                            else
                            {
                                _instance = NativeGameBridge.Instance;
                                Core.CommonPlugin.Logger.LogInfo("Using native game bridge (REPOLib not detected).");
                            }
                        }
                        catch
                        {
                            _instance = NativeGameBridge.Instance;
                            Core.CommonPlugin.Logger.LogWarning("Failed to detect REPOLib, falling back to native bridge.");
                        }
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 注册自定义桥接实现，可覆盖默认实现。
    /// </summary>
    /// <typeparam name="T">桥接接口类型</typeparam>
    /// <param name="instance">实现实例</param>
    public static void Register<T>(T instance) where T : class
    {
        lock (_lock)
        {
            _customBridges[typeof(T)] = instance;
            Core.CommonPlugin.Logger.LogInfo($"Registered custom bridge for {typeof(T).Name}");
        }
    }

    /// <summary>
    /// 获取桥接实例。优先返回注册的自定义实现，否则返回默认实现。
    /// </summary>
    /// <typeparam name="T">桥接接口类型</typeparam>
    /// <returns>桥接实例，若未找到则返回 null</returns>
    public static T? Get<T>() where T : class
    {
        Type type = typeof(T);
        lock (_lock)
        {
            if (_customBridges.TryGetValue(type, out object? custom))
                return custom as T;

            if (DefaultInstance is T defaultBridge)
                return defaultBridge;

            return null;
        }
    }

    // ---- 便捷属性（推荐使用 Get<T>() 以保持一致性） ----
    public static IPlayerBridge Player => Get<IPlayerBridge>()!;
    public static IGameStateBridge GameState => Get<IGameStateBridge>()!;
    public static IItemBridge Item => Get<IItemBridge>()!;
    public static IHealthPackBridge HealthPack => Get<IHealthPackBridge>()!;
    public static ITruckBridge Truck => Get<ITruckBridge>()!;
    public static ISaveLoadBridge SaveLoad => Get<ISaveLoadBridge>()!;
    public static INetworkBridge Network => Get<INetworkBridge>()!;
    public static IUpgradeBridge Upgrade => Get<IUpgradeBridge>()!;
    public static IEnemyBridge Enemy => Get<IEnemyBridge>()!;
}