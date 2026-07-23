using System;
using System.Collections.Generic;

namespace Dinwlooc.Common.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }
            if (!list.Contains(handler))
                list.Add(handler);
            // 可选日志（仅调试，可注释）
            // CommonPlugin.Logger.LogInfo($"[EventBus] Subscribed to {type.Name}");
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                    _handlers.Remove(type);
            }
        }

        public static void Publish<T>(T eventData) where T : struct
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var list))
            {
                var snapshot = list.ToArray();
                foreach (Action<T> handler in snapshot)
                {
                    try
                    {
                        handler.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        CommonPlugin.Logger.LogError($"[EventBus] Error in handler for {type.Name}: {ex}");
                    }
                }
            }
            // 可选：未订阅时静默，不输出日志
        }
    }
}