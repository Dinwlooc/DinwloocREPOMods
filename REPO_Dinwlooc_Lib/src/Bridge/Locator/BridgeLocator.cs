using System;
using System.Collections.Generic;
using Dinwlooc.Common.IBridge;

namespace Dinwlooc.Common.Bridge
{
    public static class BridgeLocator
    {
        private static readonly Dictionary<Type, object> _customBridges = new();
        private static readonly object _lock = new();

        public static void Register<T>(T instance) where T : class
        {
            lock (_lock)
            {
                _customBridges[typeof(T)] = instance;
                Core.CommonPlugin.Logger.LogInfo($"Registered bridge for {typeof(T).Name}");
            }
        }

        public static T? Get<T>() where T : class
        {
            Type type = typeof(T);
            lock (_lock)
            {
                if (_customBridges.TryGetValue(type, out object? custom))
                    return custom as T;
                return null;
            }
        }

        // 便捷属性
        public static IPlayerBridge Player => Get<IPlayerBridge>()
            ?? throw new InvalidOperationException("IPlayerBridge not registered.");
        public static IGameStateBridge GameState => Get<IGameStateBridge>()
            ?? throw new InvalidOperationException("IGameStateBridge not registered.");
        public static IItemBridge Item => Get<IItemBridge>()
            ?? throw new InvalidOperationException("IItemBridge not registered.");
        public static IHealthPackBridge HealthPack => Get<IHealthPackBridge>()
            ?? throw new InvalidOperationException("IHealthPackBridge not registered.");
        public static ITruckBridge Truck => Get<ITruckBridge>()
            ?? throw new InvalidOperationException("ITruckBridge not registered.");
        public static ISaveLoadBridge SaveLoad => Get<ISaveLoadBridge>()
            ?? throw new InvalidOperationException("ISaveLoadBridge not registered.");
        public static INetworkBridge Network => Get<INetworkBridge>()
            ?? throw new InvalidOperationException("INetworkBridge not registered.");
        public static IUpgradeBridge Upgrade => Get<IUpgradeBridge>()
            ?? throw new InvalidOperationException("IUpgradeBridge not registered.");
        public static IEnemyBridge Enemy => Get<IEnemyBridge>()
            ?? throw new InvalidOperationException("IEnemyBridge not registered.");
        public static IEnergyBridge Energy => Get<IEnergyBridge>()
            ?? throw new InvalidOperationException("IEnergyBridge not registered.");
        public static IMenuBridge Menu => Get<IMenuBridge>()
            ?? throw new InvalidOperationException("IMenuBridge not registered.");
        public static ISlideBridge Slide => Get<ISlideBridge>()
    ?? throw new InvalidOperationException("ISlideBridge not registered.");
        public static IMovementOverrideBridge MovementOverride => Get<IMovementOverrideBridge>()
    ?? throw new InvalidOperationException("IMovementOverrideBridge not registered.");
    }
}