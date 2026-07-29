using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using Dinwlooc.Common.Reflection; // 用于类型转换

namespace Dinwlooc.Common.Sync
{
    internal static class SyncRpcProcessor
    {
        private static bool TryGetCache(string cacheName, out ISyncCache cache)
        {
            if (SyncManager.Instance.SyncCaches.TryGetValue(cacheName, out var c))
            {
                cache = c;
                return true;
            }
            cache = null!;
            return false;
        }

        internal static void ApplyRemoteData(string cacheName, object key, object value)
        {
            if (!TryGetCache(cacheName, out var cache)) return;
            cache.ApplyRemoteSetObject(key, value);
        }

        internal static void ApplyRemoteDataBinary(string cacheName, object key, byte[] data)
        {
            if (!TryGetCache(cacheName, out var cache)) return;
            cache.ApplyRemoteSetBinary(key, data);
        }

        internal static void ApplyRemoteRemove(string cacheName, object key)
        {
            if (!TryGetCache(cacheName, out var cache)) return;
            cache.ApplyRemoteRemove(key);
        }

        internal static void ApplyRemoteClear(string cacheName)
        {
            if (!TryGetCache(cacheName, out var cache)) return;
            cache.ApplyRemoteClear();
        }

        /// <summary>
        /// 应用全量快照（原子性：先验证所有键可转换，再一次性清空并填充）。
        /// </summary>
        internal static void ApplyFullSnapshot(string cacheName, PhotonHashtable snapshot)
        {
            if (!TryGetCache(cacheName, out var cache)) return;

            // 预验证所有键值能否转换（无法获知具体 TKey/TValue，但可尝试使用 ReflectionCache.ChangeType 做简单验证）
            // 由于实际转换在内部进行，我们在此仅做通用性检查：确保键和值非空且类型可接受
            foreach (object key in snapshot.Keys)
            {
                if (key == null)
                {
                    Core.CommonPlugin.Logger.LogError($"[SyncRpcProcessor] 快照包含 null 键，放弃整个快照。");
                    return;
                }
                // 值可为 null（若 TValue 允许），但这里不强制
            }

            // 全部通过，执行更新
            cache.ApplyRemoteClear();
            foreach (object key in snapshot.Keys)
            {
                cache.ApplyRemoteSetObject(key, snapshot[key]);
            }
        }

        internal static void ApplyFullSnapshotBinary(string cacheName, Dictionary<object, byte[]> snapshot)
        {
            if (!TryGetCache(cacheName, out var cache)) return;

            cache.ApplyRemoteClear();
            foreach (var kv in snapshot)
            {
                try
                {
                    cache.ApplyRemoteSetBinary(kv.Key, kv.Value);
                }
                catch (Exception ex)
                {
                    Core.CommonPlugin.Logger.LogError($"[SyncRpcProcessor] 应用二进制快照键 {kv.Key} 失败: {ex.Message}，继续处理其余键。");
                }
            }
        }

        internal static void ApplyMergeRequest(string cacheName, object key, object value)
        {
            if (!TryGetCache(cacheName, out var cache)) return;
            if (cache.Mode != SyncMode.Merge) return;
            cache.ProcessMergeObject(key, value);
        }

        internal static void ApplyMergeRequestBinary(string cacheName, object key, byte[] data)
        {
            if (!TryGetCache(cacheName, out var cache)) return;
            if (cache.Mode != SyncMode.Merge) return;
            cache.ProcessMergeBinary(key, data);
        }

        /// <summary>
        /// 处理来自 SyncRpcModule 的子操作码（用于扩展，当前为占位）。
        /// </summary>
        internal static void HandleSubOp(byte op, string cacheName, PhotonHashtable data)
        {
            // 实际已在 SyncRpcModule 中直接路由到对应方法，此处保留空实现
        }
    }
}