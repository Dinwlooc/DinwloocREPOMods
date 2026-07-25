using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 同步区域管理器，采用懒加载模式。
    /// 仅在首次调用 <see cref="GetOrCreateSyncCache{TKey, TValue}"/> 时创建单例实例和 PhotonView。
    /// </summary>
    public class SyncRegionManager : MonoBehaviour
    {
        private static SyncRegionManager? _instance;
        public static SyncRegionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(nameof(SyncRegionManager));
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<SyncRegionManager>();
                }
                return _instance;
            }
        }

        private readonly ConcurrentDictionary<string, object> _syncCaches = new();
        private PhotonView _photonView = null!;

        // 方法缓存：按缓存类型存储方法信息
        private class MethodCache
        {
            public MethodInfo? ApplyRemoteSet { get; set; }
            public MethodInfo? ApplyRemoteRemove { get; set; }
            public MethodInfo? ApplyRemoteClear { get; set; }
            public MethodInfo? TryGet { get; set; }
            public MethodInfo? GetAllData { get; set; }
            public MethodInfo? Set { get; set; }
        }
        private static readonly ConcurrentDictionary<Type, MethodCache> _methodCache = new();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _photonView = gameObject.GetComponent<PhotonView>();
            if (_photonView == null)
                _photonView = gameObject.AddComponent<PhotonView>();
            _photonView.ViewID = 2000;
            _photonView.ObservedComponents = new List<Component>();
            _photonView.Synchronization = ViewSynchronization.Off;
            _photonView.OwnershipTransfer = OwnershipOption.Takeover;

            EventBus.Subscribe<PlayerLevelEnteredEvent>(OnPlayerLevelEntered);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerLevelEnteredEvent>(OnPlayerLevelEntered);
        }

        public ISyncCache<TKey, TValue> GetOrCreateSyncCache<TKey, TValue>(
            string cacheName,
            SyncMode mode,
            Func<TValue, TValue, TValue>? mergeFunc = null) where TKey : notnull
        {
            if (_syncCaches.TryGetValue(cacheName, out var existing))
            {
                if (existing is SyncCache<TKey, TValue> typed)
                    return typed;
                throw new InvalidOperationException($"Cache '{cacheName}' exists with different type.");
            }

            var newCache = new SyncCache<TKey, TValue>(cacheName, mode, _photonView, mergeFunc);
            _syncCaches[cacheName] = newCache;
            CommonPlugin.Logger.LogInfo($"Sync cache '{cacheName}' created with mode {mode}.");
            return newCache;
        }

        private MethodCache GetOrCreateMethodCache(Type cacheType)
        {
            return _methodCache.GetOrAdd(cacheType, type =>
            {
                return new MethodCache
                {
                    ApplyRemoteSet = type.GetMethod("ApplyRemoteSet", BindingFlags.NonPublic | BindingFlags.Instance),
                    ApplyRemoteRemove = type.GetMethod("ApplyRemoteRemove", BindingFlags.NonPublic | BindingFlags.Instance),
                    ApplyRemoteClear = type.GetMethod("ApplyRemoteClear", BindingFlags.NonPublic | BindingFlags.Instance),
                    TryGet = type.GetMethod("TryGet", new[] { typeof(object), typeof(object).MakeByRefType() }),
                    GetAllData = type.GetMethod("GetAllData", BindingFlags.NonPublic | BindingFlags.Instance),
                    Set = type.GetMethod("Set", new[] { typeof(object), typeof(object), typeof(TimeSpan?) })
                };
            });
        }

        private void OnPlayerLevelEntered(PlayerLevelEnteredEvent evt)
        {
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
                return;

            foreach (var kv in _syncCaches)
            {
                string cacheName = kv.Key;
                var cacheObj = kv.Value;
                var cacheType = cacheObj.GetType();
                var methods = GetOrCreateMethodCache(cacheType);
                var getAllData = methods.GetAllData;
                if (getAllData != null)
                {
                    var data = getAllData.Invoke(cacheObj, null) as Dictionary<object, object>;
                    if (data != null && data.Count > 0)
                        SendFullSnapshotToPlayer(cacheName, data, evt.Player.photonView.ViewID);
                }
            }
        }

        private void SendFullSnapshotToPlayer(string cacheName, Dictionary<object, object> data, int targetPlayerViewId)
        {
            var hashtable = new Hashtable();
            foreach (var kv in data)
                hashtable[kv.Key] = kv.Value;
            _photonView.RPC("RPC_ReceiveFullSnapshot", RpcTarget.Others, cacheName, hashtable, targetPlayerViewId);
        }

        internal void BroadcastData<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
                return;
            _photonView.RPC("RPC_ApplyData", RpcTarget.Others, cacheName, key, value);
        }

        internal void BroadcastRemove<TKey>(string cacheName, TKey key)
        {
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
                return;
            _photonView.RPC("RPC_ApplyRemove", RpcTarget.Others, cacheName, key);
        }

        internal void BroadcastClear(string cacheName)
        {
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
                return;
            _photonView.RPC("RPC_ApplyClear", RpcTarget.Others, cacheName);
        }

        internal void BroadcastFullSnapshot<TKey, TValue>(string cacheName, Dictionary<TKey, TValue> snapshot)
        {
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
                return;
            var hashtable = new Hashtable();
            foreach (var kv in snapshot)
                hashtable[kv.Key] = kv.Value;
            _photonView.RPC("RPC_ApplyFullSnapshot", RpcTarget.Others, cacheName, hashtable);
        }

        internal void SendSnapshot<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (PhotonNetwork.IsMasterClient)
                return;
            _photonView.RPC("RPC_ReceiveSnapshot", RpcTarget.MasterClient, cacheName, key, value);
        }

        internal void SendMergeRequest<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (PhotonNetwork.IsMasterClient)
                return;
            _photonView.RPC("RPC_ReceiveMergeRequest", RpcTarget.MasterClient, cacheName, key, value);
        }

        [PunRPC]
        private void RPC_ApplyData(string cacheName, object key, object value, PhotonMessageInfo info)
        {
            if (info.Sender == PhotonNetwork.LocalPlayer)
                return;
            if (_syncCaches.TryGetValue(cacheName, out var cacheObj))
            {
                var methods = GetOrCreateMethodCache(cacheObj.GetType());
                var method = methods.ApplyRemoteSet;
                if (method != null)
                {
                    var genericArgs = cacheObj.GetType().GenericTypeArguments;
                    try
                    {
                        var typedKey = Convert.ChangeType(key, genericArgs[0]);
                        var typedValue = Convert.ChangeType(value, genericArgs[1]);
                        method.Invoke(cacheObj, new[] { typedKey, typedValue });
                    }
                    catch (Exception ex)
                    {
                        CommonPlugin.Logger.LogError($"Error applying remote set for cache {cacheName}: {ex}");
                    }
                }
            }
        }

        [PunRPC]
        private void RPC_ApplyRemove(string cacheName, object key, PhotonMessageInfo info)
        {
            if (info.Sender == PhotonNetwork.LocalPlayer)
                return;
            if (_syncCaches.TryGetValue(cacheName, out var cacheObj))
            {
                var methods = GetOrCreateMethodCache(cacheObj.GetType());
                var method = methods.ApplyRemoteRemove;
                if (method != null)
                {
                    var genericArgs = cacheObj.GetType().GenericTypeArguments;
                    try
                    {
                        var typedKey = Convert.ChangeType(key, genericArgs[0]);
                        method.Invoke(cacheObj, new[] { typedKey });
                    }
                    catch (Exception ex)
                    {
                        CommonPlugin.Logger.LogError($"Error applying remote remove for cache {cacheName}: {ex}");
                    }
                }
            }
        }

        [PunRPC]
        private void RPC_ApplyClear(string cacheName, PhotonMessageInfo info)
        {
            if (info.Sender == PhotonNetwork.LocalPlayer)
                return;
            if (_syncCaches.TryGetValue(cacheName, out var cacheObj))
            {
                var methods = GetOrCreateMethodCache(cacheObj.GetType());
                var method = methods.ApplyRemoteClear;
                method?.Invoke(cacheObj, null);
            }
        }

        [PunRPC]
        private void RPC_ApplyFullSnapshot(string cacheName, Hashtable snapshot, PhotonMessageInfo info)
        {
            if (info.Sender == PhotonNetwork.LocalPlayer)
                return;
            if (_syncCaches.TryGetValue(cacheName, out var cacheObj))
            {
                var methods = GetOrCreateMethodCache(cacheObj.GetType());
                var clearMethod = methods.ApplyRemoteClear;
                clearMethod?.Invoke(cacheObj, null);

                var setMethod = methods.ApplyRemoteSet;
                if (setMethod != null)
                {
                    var genericArgs = cacheObj.GetType().GenericTypeArguments;
                    foreach (var key in snapshot.Keys)
                    {
                        try
                        {
                            var typedKey = Convert.ChangeType(key, genericArgs[0]);
                            var typedValue = Convert.ChangeType(snapshot[key], genericArgs[1]);
                            setMethod.Invoke(cacheObj, new[] { typedKey, typedValue });
                        }
                        catch (Exception ex)
                        {
                            CommonPlugin.Logger.LogError($"Error applying full snapshot for cache {cacheName}: {ex}");
                        }
                    }
                }
            }
        }

        [PunRPC]
        private void RPC_ReceiveSnapshot(string cacheName, object key, object value, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;
            if (_syncCaches.TryGetValue(cacheName, out var cacheObj))
            {
                var methods = GetOrCreateMethodCache(cacheObj.GetType());
                var setMethod = methods.ApplyRemoteSet;
                if (setMethod != null)
                {
                    var genericArgs = cacheObj.GetType().GenericTypeArguments;
                    try
                    {
                        var typedKey = Convert.ChangeType(key, genericArgs[0]);
                        var typedValue = Convert.ChangeType(value, genericArgs[1]);
                        setMethod.Invoke(cacheObj, new[] { typedKey, typedValue });
                    }
                    catch (Exception ex)
                    {
                        CommonPlugin.Logger.LogError($"Error applying client snapshot for cache {cacheName}: {ex}");
                    }
                }
            }
        }

        [PunRPC]
        private void RPC_ReceiveMergeRequest(string cacheName, object key, object value, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;
            if (_syncCaches.TryGetValue(cacheName, out var cacheObj))
            {
                var cacheType = cacheObj.GetType();
                var methods = GetOrCreateMethodCache(cacheType);
                var modeProp = cacheType.GetProperty("Mode");
                if (modeProp != null && (SyncMode)modeProp.GetValue(cacheObj) == SyncMode.Merge)
                {
                    var mergeFuncField = cacheType.GetField("_mergeFunc", BindingFlags.NonPublic | BindingFlags.Instance);
                    var mergeFunc = mergeFuncField?.GetValue(cacheObj) as Delegate;
                    if (mergeFunc != null)
                    {
                        var genericArgs = cacheType.GenericTypeArguments;
                        try
                        {
                            var typedKey = Convert.ChangeType(key, genericArgs[0]);
                            var typedValue = Convert.ChangeType(value, genericArgs[1]);
                            var tryGetMethod = methods.TryGet;
                            if (tryGetMethod != null)
                            {
                                var parameters = new object[] { typedKey, null! };
                                if ((bool)tryGetMethod.Invoke(cacheObj, parameters))
                                {
                                    var currentValue = parameters[1];
                                    var mergedValue = mergeFunc.DynamicInvoke(currentValue, typedValue);
                                    var setMethod = methods.ApplyRemoteSet;
                                    setMethod?.Invoke(cacheObj, new[] { typedKey, mergedValue });
                                    BroadcastData(cacheName, typedKey, mergedValue!);
                                }
                                else
                                {
                                    var setMethod = methods.ApplyRemoteSet;
                                    setMethod?.Invoke(cacheObj, new[] { typedKey, typedValue });
                                    BroadcastData(cacheName, typedKey, typedValue);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            CommonPlugin.Logger.LogError($"Error merging for cache {cacheName}: {ex}");
                        }
                    }
                }
            }
        }

        [PunRPC]
        private void RPC_ReceiveFullSnapshot(string cacheName, Hashtable snapshot, int targetPlayerViewId, PhotonMessageInfo info)
        {
            if (info.Sender != PhotonNetwork.MasterClient)
                return;
            if (_photonView.ViewID != targetPlayerViewId && PhotonNetwork.LocalPlayer != info.Sender)
                return;

            if (_syncCaches.TryGetValue(cacheName, out var cacheObj))
            {
                var methods = GetOrCreateMethodCache(cacheObj.GetType());
                var clearMethod = methods.ApplyRemoteClear;
                clearMethod?.Invoke(cacheObj, null);
                var setMethod = methods.ApplyRemoteSet;
                if (setMethod != null)
                {
                    var genericArgs = cacheObj.GetType().GenericTypeArguments;
                    foreach (var key in snapshot.Keys)
                    {
                        try
                        {
                            var typedKey = Convert.ChangeType(key, genericArgs[0]);
                            var typedValue = Convert.ChangeType(snapshot[key], genericArgs[1]);
                            setMethod.Invoke(cacheObj, new[] { typedKey, typedValue });
                        }
                        catch (Exception ex)
                        {
                            CommonPlugin.Logger.LogError($"Error applying full snapshot for cache {cacheName}: {ex}");
                        }
                    }
                }
            }
        }
    }
}