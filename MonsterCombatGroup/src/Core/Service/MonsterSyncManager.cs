using System;
using System.IO;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Caching;
using Dinwlooc.Common.IBridge;
using Dinwlooc.Common.Sync;
using Photon.Pun;
using UnityEngine;

namespace MonsterCombatGroup
{
    /// <summary>
    /// 管理怪物属性（如最大血量）的跨客户端同步，使用 PhotonView.ViewID 作为键。
    /// 采用被动懒加载，仅在首次调用更新方法时初始化缓存并订阅事件。
    /// </summary>
    public static class MonsterSyncManager
    {
        private const string CACHE_NAME = "MonsterSyncCache";

        private static ISyncCache<int, MonsterSyncData>? _cache;
        private static bool _subscribed = false;

        /// <summary>
        /// 确保同步缓存已创建并订阅事件。所有客户端调用此方法都会获取同一个全局缓存实例。
        /// </summary>
        private static void EnsureCacheInitialized()
        {
            if (_cache != null)
                return;

            _cache = CacheManager.GetOrCreateSyncCache<int, MonsterSyncData>(
                CACHE_NAME,
                SyncMode.HostAuthority,
                serialize: (BinaryWriter writer, MonsterSyncData data) =>
                {
                    writer.Write(data.ViewID);
                    writer.Write(data.MaxHealth);
                },
                deserialize: (BinaryReader reader) =>
                {
                    return new MonsterSyncData
                    {
                        ViewID = reader.ReadInt32(),
                        MaxHealth = reader.ReadInt32()
                    };
                }
            );

            if (_cache != null && !_subscribed)
            {
                _cache.OnDataChanged += OnDataChanged;
                _cache.OnDataRemoved += OnDataRemoved;
                _cache.OnDataCleared += OnDataCleared;
                _subscribed = true;
            }
        }

        /// <summary>
        /// 公共初始化方法，供外部在合适的时机（如进入关卡）调用，确保所有客户端都能接收同步数据。
        /// </summary>
        public static void EnsureInitialized()
        {
            EnsureCacheInitialized();
        }

        /// <summary>
        /// 更新怪物的最大血量（仅房主可调用）。自动应用本地修改并同步到所有客户端。
        /// </summary>
        /// <param name="enemy">目标怪物</param>
        /// <param name="newMaxHealth">新的最大血量</param>
        public static void UpdateMonsterMaxHealth(EnemyParent enemy, int newMaxHealth)
        {
            if (enemy == null || enemy.Enemy == null || enemy.Enemy.Health == null)
                return;

            // 只有房主可以写入
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            // 获取 PhotonView 作为网络标识
            PhotonView view = enemy.GetComponent<PhotonView>();
            if (view == null)
            {
                Debug.LogWarning("[MonsterSyncManager] 怪物缺少 PhotonView，无法同步最大血量。");
                return;
            }
            int viewID = view.ViewID;
            if (viewID == 0)
                return;

            // 应用本地修改（最大血量）
            enemy.Enemy.Health.health = newMaxHealth;
            // 如果当前血量超出新上限，裁剪
            if (enemy.Enemy.Health.healthCurrent > newMaxHealth)
                enemy.Enemy.Health.healthCurrent = newMaxHealth;

            // 初始化缓存（若未初始化）
            EnsureCacheInitialized();

            // 更新缓存（触发广播）
            var data = new MonsterSyncData { ViewID = viewID, MaxHealth = newMaxHealth };
            _cache?.Set(viewID, data);
        }

        /// <summary>
        /// 缓存数据变更事件（所有客户端触发）。客户端应用新最大血量，并调用 SetHealth 将当前血量置为新最大值（满血）。
        /// </summary>
        private static void OnDataChanged(int key, MonsterSyncData data)
        {
            // 房主已在本地更新，避免重复
            if (SemiFunc.IsMasterClientOrSingleplayer())
                return;

            // 通过 PhotonView ID 查找对象
            PhotonView view = PhotonView.Find(key);
            if (view == null)
                return;

            EnemyParent enemy = view.GetComponent<EnemyParent>();
            if (enemy == null || enemy.Enemy == null || enemy.Enemy.Health == null)
                return;

            // 应用最大血量
            enemy.Enemy.Health.health = data.MaxHealth;

            // 客户端调用 SetHealth 将当前血量置为满血（与房主行为一致）
            IEnemyModifierBridge? modifier = BridgeLocator.Get<IEnemyModifierBridge>();
            if (modifier != null)
            {
                modifier.SetHealth(enemy, data.MaxHealth);
            }
            else
            {
                // 降级方案：直接赋值，避免空引用
                enemy.Enemy.Health.healthCurrent = data.MaxHealth;
                Debug.LogWarning("[MonsterSyncManager] IEnemyModifierBridge 不可用，使用降级方案设置当前血量。");
            }
        }

        private static void OnDataRemoved(int key)
        {
            // 无需处理，怪物死亡后不再同步
        }

        private static void OnDataCleared()
        {
            // 缓存清空时无需额外操作（所有怪物数据已被清除）
        }

        /// <summary>
        /// 清空同步缓存中的所有数据（仅房主调用，通常在场景切换时）。
        /// </summary>
        public static void ClearState()
        {
            if (_cache != null && SemiFunc.IsMasterClientOrSingleplayer())
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// 重置管理器，取消订阅并释放缓存引用（在离开房间或模组卸载时调用）。
        /// </summary>
        public static void Reset()
        {
            if (_cache != null)
            {
                if (_subscribed)
                {
                    _cache.OnDataChanged -= OnDataChanged;
                    _cache.OnDataRemoved -= OnDataRemoved;
                    _cache.OnDataCleared -= OnDataCleared;
                    _subscribed = false;
                }
                _cache = null;
            }
        }
    }

    /// <summary>
    /// 同步数据结构，包含网络标识和最大血量。
    /// </summary>
    public struct MonsterSyncData
    {
        public int ViewID;
        public int MaxHealth;
    }
}