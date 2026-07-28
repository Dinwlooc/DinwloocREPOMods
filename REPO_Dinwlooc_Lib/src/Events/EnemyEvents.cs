// Dinwlooc.Common/Events/EnemyEvents.cs
using UnityEngine;

namespace Dinwlooc.Common.Events
{
    /// <summary>怪物生成时触发（对应 OnSpawn）</summary>
    public readonly struct EnemySpawnedEvent
    {
        public readonly EnemyParent EnemyParent;
        public readonly int InstanceId;

        public EnemySpawnedEvent(EnemyParent enemyParent)
        {
            EnemyParent = enemyParent;
            InstanceId = enemyParent ? enemyParent.GetInstanceID() : 0;
        }
    }

    /// <summary>怪物死亡时触发（对应 OnDeath）</summary>
    public readonly struct EnemyDiedEvent
    {
        public readonly EnemyParent EnemyParent;
        public readonly int InstanceId;

        public EnemyDiedEvent(EnemyParent enemyParent)
        {
            EnemyParent = enemyParent;
            InstanceId = enemyParent ? enemyParent.GetInstanceID() : 0;
        }
    }

    /// <summary>怪物发现玩家时触发（对应 OnVision）</summary>
    public readonly struct EnemyVisionEvent
    {
        public readonly EnemyParent EnemyParent;
        public readonly int InstanceId;

        public EnemyVisionEvent(EnemyParent enemyParent)
        {
            EnemyParent = enemyParent;
            InstanceId = enemyParent ? enemyParent.GetInstanceID() : 0;
        }
    }

    /// <summary>怪物调查时触发（对应 OnInvestigate）</summary>
    public readonly struct EnemyInvestigateEvent
    {
        public readonly EnemyParent EnemyParent;
        public readonly int InstanceId;

        public EnemyInvestigateEvent(EnemyParent enemyParent)
        {
            EnemyParent = enemyParent;
            InstanceId = enemyParent ? enemyParent.GetInstanceID() : 0;
        }
    }

    /// <summary>怪物受伤时触发（对应 OnHurt）</summary>
    public readonly struct EnemyHurtEvent
    {
        public readonly EnemyParent EnemyParent;
        public readonly int InstanceId;

        public EnemyHurtEvent(EnemyParent enemyParent)
        {
            EnemyParent = enemyParent;
            InstanceId = enemyParent ? enemyParent.GetInstanceID() : 0;
        }
    }

    /// <summary>怪物被玩家抓取时触发（对应 OnGrabbed）</summary>
    public readonly struct EnemyGrabbedEvent
    {
        public readonly EnemyParent EnemyParent;
        public readonly int InstanceId;

        public EnemyGrabbedEvent(EnemyParent enemyParent)
        {
            EnemyParent = enemyParent;
            InstanceId = enemyParent ? enemyParent.GetInstanceID() : 0;
        }
    }

    /// <summary>怪物消失/移除时触发（对应 OnDespawn）</summary>
    public readonly struct EnemyDespawnEvent
    {
        public readonly EnemyParent EnemyParent;
        public readonly int InstanceId;

        public EnemyDespawnEvent(EnemyParent enemyParent)
        {
            EnemyParent = enemyParent;
            InstanceId = enemyParent ? enemyParent.GetInstanceID() : 0;
        }
    }
}