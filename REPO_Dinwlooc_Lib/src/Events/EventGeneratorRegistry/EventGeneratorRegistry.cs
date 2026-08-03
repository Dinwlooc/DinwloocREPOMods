// Dinwlooc.Common/Core/EventGeneratorRegistry.cs
using System;
using System.Collections.Generic;
using Dinwlooc.Common.Events;

namespace Dinwlooc.Common.Core
{
    /// <summary>
    /// 硬编码映射：事件类型 → 生成器工厂（返回 IEventGenerator 实例）。
    /// 用于 EventBus 自动启用生成器，避免反射。
    /// </summary>
    public static class EventGeneratorRegistry
    {
        public static readonly IReadOnlyDictionary<Type, Func<IEventGenerator>> EventToGeneratorFactory;

        static EventGeneratorRegistry()
        {
            Dictionary<Type, Func<IEventGenerator>> dict = new Dictionary<Type, Func<IEventGenerator>>
            {
                // 场景事件
                { typeof(SceneChangedEvent), () => SceneEventGenerator.Instance },

                // 玩家事件
                { typeof(PlayerDiedEvent), () => PlayerDeathEventGenerator.Instance },
                { typeof(PlayerRevivedEvent), () => PlayerReviveEventGenerator.Instance },
                { typeof(PlayerJoinedEvent), () => PlayerJoinedEventGenerator.Instance },

                // 怪物视野事件
                { typeof(MonsterVisibilityChangedEvent), () => VisionEventGenerator.Instance },
            };

            EventToGeneratorFactory = dict;
        }
    }
}