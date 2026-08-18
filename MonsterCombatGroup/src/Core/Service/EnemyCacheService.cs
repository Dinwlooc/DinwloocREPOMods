using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace MonsterCombatGroup
{
    /// <summary>
    /// 全局敌人实例缓存服务，统一管理所有敌人的引用，减少重复遍历开销。
    /// 所有方法均为静态，采用被动懒加载，仅在首次调用时初始化。
    /// </summary>
    public static class EnemyCacheService
    {
        private const float REFRESH_INTERVAL = 0.5f;

        private static IEnemyBridge _enemyBridge;
        private static Dictionary<int, EnemyParent> _cache;
        private static float _nextRefreshTime;
        private static bool _initialized = false;

        /// <summary>
        /// 确保缓存已初始化（懒加载）。
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized)
                return;

            _enemyBridge = BridgeLocator.Enemy;
            _cache = new Dictionary<int, EnemyParent>();
            _nextRefreshTime = 0f;
            _initialized = true;
        }

        /// <summary>
        /// 刷新缓存（若距离上次刷新超过 REFRESH_INTERVAL）。
        /// 仅在房主或单机时调用有效，但调用方应自行控制权限。
        /// </summary>
        public static void RefreshIfNeeded()
        {
            EnsureInitialized();

            if (Time.time < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.time + REFRESH_INTERVAL;
            _cache.Clear();

            IReadOnlyList<EnemyParent> allEnemies = _enemyBridge.GetAllEnemies();
            if (allEnemies == null)
                return;

            foreach (EnemyParent enemy in allEnemies)
            {
                if (enemy != null)
                {
                    int instanceId = enemy.GetInstanceID();
                    _cache[instanceId] = enemy;
                }
            }
        }

        /// <summary>
        /// 根据实例 ID 获取敌人，若不存在则返回 null。
        /// </summary>
        public static EnemyParent GetEnemyById(int instanceId)
        {
            EnsureInitialized();
            _cache.TryGetValue(instanceId, out EnemyParent enemy);
            return enemy;
        }

        /// <summary>
        /// 获取所有缓存的敌人列表（只读副本）。
        /// </summary>
        public static IReadOnlyList<EnemyParent> GetAllEnemies()
        {
            EnsureInitialized();
            List<EnemyParent> result = new List<EnemyParent>(_cache.Values);
            return result.AsReadOnly();
        }

        /// <summary>
        /// 重置缓存（场景切换、离开房间时调用）。
        /// </summary>
        public static void Reset()
        {
            if (!_initialized)
                return;

            _cache.Clear();
            _nextRefreshTime = 0f;
        }
    }
}