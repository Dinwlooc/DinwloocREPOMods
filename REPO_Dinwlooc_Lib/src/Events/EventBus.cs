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
            if (!EventGeneratorRegistry.EventToGeneratorFactory.TryGetValue(eventType, out Func<IEventGenerator> factory))
                return;

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