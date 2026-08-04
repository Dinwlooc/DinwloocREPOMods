// 文件：Dinwlooc.Common.Bridge/BridgeLocator.cs
using System;
using System.Collections.Generic;
using Dinwlooc.Common.IBridge;

namespace Dinwlooc.Common.Bridge
{
    public static class BridgeLocator
    {
        private static readonly Dictionary<Type, Lazy<object>> _lazyInstances = new Dictionary<Type, Lazy<object>>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 注册一个桥接工厂。工厂委托只有在第一次调用 Get&lt;T&gt;() 时才会执行。
        /// 这是实现懒加载的标准方式。
        /// </summary>
        public static void Register<T>(Func<T> factory) where T : class
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            Type type = typeof(T);
            lock (_lock)
            {
                _lazyInstances[type] = new Lazy<object>(() => factory());
                Core.CommonPlugin.Logger.LogInfo(string.Format("Registered lazy bridge for {0}", type.Name));
            }
        }

        /// <summary>
        /// 获取已注册的桥接实例。若尚未实例化，将在此处触发工厂委托。
        /// </summary>
        public static T Get<T>() where T : class
        {
            Type type = typeof(T);
            lock (_lock)
            {
                if (_lazyInstances.TryGetValue(type, out Lazy<object> lazy))
                {
                    return lazy.Value as T;
                }
                return null;
            }
        }

        // ==================== 静态属性（便捷访问） ====================
        public static IPlayerBridge Player
        {
            get
            {
                IPlayerBridge bridge = Get<IPlayerBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("IPlayerBridge not registered.");
                }
                return bridge;
            }
        }

        public static IGameStateBridge GameState
        {
            get
            {
                IGameStateBridge bridge = Get<IGameStateBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("IGameStateBridge not registered.");
                }
                return bridge;
            }
        }

        public static IItemBridge Item
        {
            get
            {
                IItemBridge bridge = Get<IItemBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("IItemBridge not registered.");
                }
                return bridge;
            }
        }

        public static IHealthPackBridge HealthPack
        {
            get
            {
                IHealthPackBridge bridge = Get<IHealthPackBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("IHealthPackBridge not registered.");
                }
                return bridge;
            }
        }

        public static ITruckBridge Truck
        {
            get
            {
                ITruckBridge bridge = Get<ITruckBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("ITruckBridge not registered.");
                }
                return bridge;
            }
        }

        public static ISaveLoadBridge SaveLoad
        {
            get
            {
                ISaveLoadBridge bridge = Get<ISaveLoadBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("ISaveLoadBridge not registered.");
                }
                return bridge;
            }
        }

        public static INetworkBridge Network
        {
            get
            {
                INetworkBridge bridge = Get<INetworkBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("INetworkBridge not registered.");
                }
                return bridge;
            }
        }

        public static IUpgradeBridge Upgrade
        {
            get
            {
                IUpgradeBridge bridge = Get<IUpgradeBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("IUpgradeBridge not registered.");
                }
                return bridge;
            }
        }

        public static IEnemyBridge Enemy
        {
            get
            {
                IEnemyBridge bridge = Get<IEnemyBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("IEnemyBridge not registered.");
                }
                return bridge;
            }
        }

        public static IEnergyBridge Energy
        {
            get
            {
                IEnergyBridge bridge = Get<IEnergyBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("IEnergyBridge not registered.");
                }
                return bridge;
            }
        }

        public static IMenuBridge Menu
        {
            get
            {
                IMenuBridge bridge = Get<IMenuBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("IMenuBridge not registered.");
                }
                return bridge;
            }
        }

        public static ISlideBridge Slide
        {
            get
            {
                ISlideBridge bridge = Get<ISlideBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("ISlideBridge not registered.");
                }
                return bridge;
            }
        }

        public static IMovementOverrideBridge MovementOverride
        {
            get
            {
                IMovementOverrideBridge bridge = Get<IMovementOverrideBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("IMovementOverrideBridge not registered.");
                }
                return bridge;
            }
        }

        public static IEnemyModifierBridge EnemyModifier
        {
            get
            {
                IEnemyModifierBridge bridge = Get<IEnemyModifierBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("IEnemyModifierBridge not registered.");
                }
                return bridge;
            }
        }

        // ==================== 新增 Moon 相关（懒加载） ====================
        public static IMoonBridge Moon
        {
            get
            {
                IMoonBridge bridge = Get<IMoonBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("IMoonBridge not registered.");
                }
                return bridge;
            }
        }

        public static IMoonUIBridge MoonUI
        {
            get
            {
                IMoonUIBridge bridge = Get<IMoonUIBridge>();
                if (bridge == null)
                {
                    throw new InvalidOperationException("IMoonUIBridge not registered.");
                }
                return bridge;
            }
        }
    }
}