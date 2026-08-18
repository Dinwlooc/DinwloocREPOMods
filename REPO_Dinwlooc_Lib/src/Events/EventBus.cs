// Dinwlooc.Common/Core/EventBus.cs
using System;
using System.Collections.Generic;

namespace Dinwlooc.Common.Core
{
    public static class EventBus
    {
        // ---------- 订阅处理 ----------
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private static readonly object _handlerLock = new();

        // ---------- 生成器实例管理（直接持有单例） ----------
        private static readonly Dictionary<Type, IEventGenerator> _generatorCache = new();
        private static readonly object _generatorLock = new();

        // ---------- 订阅 / 取消订阅 ----------
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type eventType = typeof(T);
            lock (_handlerLock)
            {
                if (!_handlers.TryGetValue(eventType, out List<Delegate> list))
                {
                    list = new List<Delegate>();
                    _handlers[eventType] = list;
                }
                if (!list.Contains(handler))
                    list.Add(handler);
            }

            AutoEnableGeneratorForEvent(eventType);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Type eventType = typeof(T);
            bool hasSubscribers;
            lock (_handlerLock)
            {
                if (_handlers.TryGetValue(eventType, out List<Delegate> list))
                {
                    list.Remove(handler);
                    hasSubscribers = list.Count > 0;
                    if (!hasSubscribers)
                        _handlers.Remove(eventType);
                }
                else
                {
                    hasSubscribers = false;
                }
            }

            if (!hasSubscribers)
                AutoDisableGeneratorForEvent(eventType);
        }

        public static void Publish<T>(T eventData) where T : struct
        {
            Type eventType = typeof(T);
            Delegate[] snapshot;
            lock (_handlerLock)
            {
                if (!_handlers.TryGetValue(eventType, out List<Delegate> list))
                    return;
                snapshot = list.ToArray();
            }

            foreach (Delegate d in snapshot)
            {
                if (d is Action<T> handler)
                {
                    try
                    {
                        handler.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        CommonPlugin.Logger.LogError($"[EventBus] Error in handler for {eventType.Name}: {ex}");
                    }
                }
            }
        }

        // ---------- 内部自动启用 / 禁用 ----------
        private static void AutoEnableGeneratorForEvent(Type eventType)
        {
            // ----- 1. 检查该事件是否在轮询生成器注册表中 -----
            // 若不在，说明该事件由内部逻辑主动发送，不需要任何生成器，直接返回。
            if (!EventGeneratorRegistry.EventToGeneratorFactory.TryGetValue(eventType, out Func<IEventGenerator> factory))
                return;

            // ----- 2. 判断是否需要 Hook 加速（配置启用且可用） -----
            bool useHook = CommonPlugin.IsHookGenAvailable && CommonPlugin.UseHookAcceleration.Value;

            if (useHook)
            {
                // 尝试从 Hook 注册表获取绑定器
                bool hasBinder = HookAccelerationRegistry.TryBind(eventType, out bool boundSuccess);

                if (hasBinder)
                {
                    if (boundSuccess)
                    {
                        // Hook 绑定成功，无需轮询生成器
                        return;
                    }

                    // 绑定失败（未实现或执行异常），输出降级日志并继续轮询
                    CommonPlugin.Logger.LogInfo(
                        $"[EventBus] Hook acceleration failed for event '{eventType.Name}', falling back to polling generator. " +
                        "Reason: Hook binder returned false (not implemented or error)."
                    );
                    // 继续执行下面的轮询创建逻辑
                }
                // else: 没有注册绑定器，直接走轮询（不输出日志）
            }

            // ----- 3. 回退到轮询生成器（创建或激活） -----
            IEventGenerator generator;
            lock (_generatorLock)
            {
                if (_generatorCache.TryGetValue(eventType, out generator))
                {
                    generator.Enable(60);
                    return;
                }

                generator = factory.Invoke();
                _generatorCache[eventType] = generator;
            }

            generator.Enable(60);
        }

        private static void AutoDisableGeneratorForEvent(Type eventType)
        {
            lock (_generatorLock)
            {
                if (_generatorCache.TryGetValue(eventType, out IEventGenerator generator))
                {
                    generator.Disable();
                    _generatorCache.Remove(eventType);
                }
            }
        }
    }
}