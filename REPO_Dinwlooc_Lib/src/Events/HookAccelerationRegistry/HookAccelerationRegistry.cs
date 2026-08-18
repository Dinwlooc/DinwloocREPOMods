// Dinwlooc.Common/Core/HookAccelerationRegistry.cs
using System;
using System.Collections.Generic;

namespace Dinwlooc.Common.Core
{
    /// <summary>
    /// 注册表：事件类型 → Hook 绑定器。
    /// 用于 EventBus 决定是否尝试 Hook 加速，以及绑定是否成功。
    /// </summary>
    internal static class HookAccelerationRegistry
    {
        private static readonly Dictionary<Type, Func<bool>> _binders = new();

        static HookAccelerationRegistry()
        {
            // 为所有需要被动生成的事件注册占位绑定器（未实现），
            // 这样当配置启用时，这些事件会触发降级日志。
            // 后续可逐步为特定事件实现真正的绑定逻辑。
            foreach (Type eventType in EventGeneratorRegistry.EventToGeneratorFactory.Keys)
            {
                // 占位绑定器：始终返回 false，表示绑定失败（未实现）
                _binders[eventType] = () =>
                {
                    CommonPlugin.Logger.LogDebug($"[HookAccelerationRegistry] Hook binder not implemented for {eventType.Name}.");
                    return false;
                };
            }
        }

        /// <summary>
        /// 注册特定事件类型的 Hook 绑定器。
        /// 允许外部模块覆盖默认占位，实现真正的 Hook 绑定。
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="binder">绑定函数，返回 true 表示绑定成功</param>
        public static void RegisterBinder(Type eventType, Func<bool> binder)
        {
            if (binder == null)
                throw new ArgumentNullException(nameof(binder));

            _binders[eventType] = binder;
        }

        /// <summary>
        /// 尝试绑定指定事件类型的 Hook。
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="success">若存在绑定器，则返回其执行结果；否则为 false</param>
        /// <returns>若该事件类型有注册绑定器，返回 true；否则返回 false</returns>
        public static bool TryBind(Type eventType, out bool success)
        {
            if (_binders.TryGetValue(eventType, out Func<bool> binder))
            {
                try
                {
                    success = binder.Invoke();
                }
                catch (Exception ex)
                {
                    CommonPlugin.Logger.LogError($"[HookAccelerationRegistry] Error in binder for {eventType.Name}: {ex}");
                    success = false;
                }
                return true;
            }

            success = false;
            return false;
        }
    }
}