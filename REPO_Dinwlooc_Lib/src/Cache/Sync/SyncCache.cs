using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Dinwlooc.Common.Caching;
using Photon.Pun;
using UnityEngine;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 同步缓存实现，支持多种同步模式，基于 Photon RPC 进行网络通信。
    /// 必须由 SyncRegionManager 创建和管理。
    /// </summary>
    internal class SyncCache<TKey, TValue> : ISyncCache<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _cache = new();
        private readonly SyncMode _mode;
        private readonly string _cacheName;
        private readonly PhotonView _photonView;

        // 自定义合并函数（仅用于 Merge 模式）
        private readonly Func<TValue, TValue, TValue>? _mergeFunc;

        public event Action<TKey, TValue>? OnDataChanged;

        public SyncMode Mode => _mode;

        internal SyncCache(string cacheName, SyncMode mode, PhotonView photonView, Func<TValue, TValue, TValue>? mergeFunc = null)
        {
            _cacheName = cacheName;
            _mode = mode;
            _photonView = photonView;
            _mergeFunc = mergeFunc;
        }

        // ----- ICacheProvider 实现 -----
        public bool TryGet(TKey key, out TValue value) => _cache.TryGetValue(key, out value);

        public void Set(TKey key, TValue value, TimeSpan? expiration = null)
        {
            // 根据模式决定是否允许写入
            bool isHost = PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom;
            bool canWrite = isHost || _mode == SyncMode.ClientSnapshot || _mode == SyncMode.Merge;

            if (!canWrite)
            {
                // 非房主且在 HostAuthority 模式下，忽略写入
                return;
            }

            // 如果是客户端快照模式且键不是当前玩家 SteamID，可能需要限制，但由调用者决定
            // 这里只做通用处理
            _cache[key] = value;
            OnDataChanged?.Invoke(key, value);

            // 通知同步
            if (isHost && (_mode == SyncMode.HostAuthority || _mode == SyncMode.Merge))
            {
                // 房主立即广播
                SyncRegionManager.Instance.BroadcastData(_cacheName, key, value);
            }
            else if (_mode == SyncMode.ClientSnapshot && !isHost)
            {
                // 客户端发送快照给房主（由 SyncRegionManager 定期收集，或立即发送）
                SyncRegionManager.Instance.SendSnapshot(_cacheName, key, value);
            }
            else if (_mode == SyncMode.Merge && !isHost)
            {
                // 客户端发送修改给房主，房主合并后广播
                SyncRegionManager.Instance.SendMergeRequest(_cacheName, key, value);
            }
        }

        public bool Remove(TKey key)
        {
            bool removed = _cache.TryRemove(key, out _);
            if (removed && PhotonNetwork.IsMasterClient && (_mode == SyncMode.HostAuthority || _mode == SyncMode.Merge))
            {
                // 房主同步删除
                SyncRegionManager.Instance.BroadcastRemove(_cacheName, key);
            }
            return removed;
        }

        public void Clear()
        {
            _cache.Clear();
            if (PhotonNetwork.IsMasterClient && (_mode == SyncMode.HostAuthority || _mode == SyncMode.Merge))
            {
                SyncRegionManager.Instance.BroadcastClear(_cacheName);
            }
        }

        public void Refresh(TKey key)
        {
            // 可延长过期时间，但当前不实现
        }

        // ----- 内部同步方法 -----
        internal void ApplyRemoteSet(TKey key, TValue value)
        {
            _cache[key] = value;
            OnDataChanged?.Invoke(key, value);
        }

        internal void ApplyRemoteRemove(TKey key)
        {
            _cache.TryRemove(key, out _);
            // 没有事件，但可考虑触发
        }

        internal void ApplyRemoteClear()
        {
            _cache.Clear();
        }

        // ----- 强制同步 -----
        public void SyncNow()
        {
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
                return;

            // 发送全量数据
            var snapshot = new Dictionary<TKey, TValue>(_cache);
            SyncRegionManager.Instance.BroadcastFullSnapshot(_cacheName, snapshot);
        }

        // ----- 获取所有数据（用于快照）-----
        internal Dictionary<TKey, TValue> GetAllData() => new(_cache);
    }
}