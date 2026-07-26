using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dinwlooc.Common.Core;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

namespace Dinwlooc.Common.Sync
{
    public static class SyncRpcHelper
    {
        private const int RPC_VIEW_ID = 3000;
        private const string RPC_GAMEOBJECT_NAME = "SyncRpcHelper";

        private static PhotonView? _photonView;
        private static readonly object _lock = new object();

        private class MethodCache
        {
            public MethodInfo? ApplyRemoteSet { get; set; }
            public MethodInfo? ApplyRemoteSetBinary { get; set; }
            public MethodInfo? ApplyRemoteSetObject { get; set; }
            public MethodInfo? ApplyRemoteRemove { get; set; }
            public MethodInfo? ApplyRemoteClear { get; set; }
            public MethodInfo? TryGet { get; set; }
        }

        private static readonly ConcurrentDictionary<Type, MethodCache> _methodCache = new ConcurrentDictionary<Type, MethodCache>();

        private static PhotonView GetPhotonView()
        {
            if (_photonView != null)
            {
                return _photonView;
            }

            lock (_lock)
            {
                if (_photonView != null)
                {
                    return _photonView;
                }

                GameObject go = new GameObject(RPC_GAMEOBJECT_NAME);
                UnityEngine.Object.DontDestroyOnLoad(go);
                _photonView = go.AddComponent<PhotonView>();
                _photonView.ViewID = RPC_VIEW_ID;
                _photonView.ObservedComponents = new List<Component>();
                _photonView.Synchronization = ViewSynchronization.Off;
                _photonView.OwnershipTransfer = OwnershipOption.Takeover;
                return _photonView;
            }
        }

        private static MethodCache GetOrCreateMethodCache(Type cacheType)
        {
            return _methodCache.GetOrAdd(cacheType, (Type type) =>
            {
                MethodCache mc = new MethodCache();
                mc.ApplyRemoteSet = type.GetMethod("ApplyRemoteSet", BindingFlags.NonPublic | BindingFlags.Instance);
                mc.ApplyRemoteSetBinary = type.GetMethod("ApplyRemoteSetBinary", BindingFlags.NonPublic | BindingFlags.Instance);
                mc.ApplyRemoteSetObject = type.GetMethod("ApplyRemoteSetObject", BindingFlags.NonPublic | BindingFlags.Instance);
                mc.ApplyRemoteRemove = type.GetMethod("ApplyRemoteRemove", BindingFlags.NonPublic | BindingFlags.Instance);
                mc.ApplyRemoteClear = type.GetMethod("ApplyRemoteClear", BindingFlags.NonPublic | BindingFlags.Instance);
                mc.TryGet = type.GetMethod("TryGet", new Type[] { typeof(object), typeof(object).MakeByRefType() });
                return mc;
            });
        }

        public static void BroadcastData<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
            {
                GetPhotonView().RPC("RPC_ApplyData", RpcTarget.Others, cacheName, key, value);
            }
        }

        public static void BroadcastDataBinary<TKey>(string cacheName, TKey key, byte[] data)
        {
            if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
            {
                GetPhotonView().RPC("RPC_ApplyDataBinary", RpcTarget.Others, cacheName, key, data);
            }
        }

        public static void BroadcastRemove<TKey>(string cacheName, TKey key)
        {
            if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
            {
                GetPhotonView().RPC("RPC_ApplyRemove", RpcTarget.Others, cacheName, key);
            }
        }

        public static void BroadcastClear(string cacheName)
        {
            if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
            {
                GetPhotonView().RPC("RPC_ApplyClear", RpcTarget.Others, cacheName);
            }
        }

        public static void BroadcastFullSnapshot<TKey, TValue>(string cacheName, ConcurrentDictionary<TKey, TValue> snapshot)
        {
            if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
            {
                Hashtable hashtable = new Hashtable();
                foreach (KeyValuePair<TKey, TValue> kv in snapshot)
                {
                    hashtable[kv.Key] = kv.Value;
                }
                GetPhotonView().RPC("RPC_ApplyFullSnapshot", RpcTarget.Others, cacheName, hashtable);
            }
        }

        public static void BroadcastFullSnapshotBinary<TKey>(string cacheName, Dictionary<object, byte[]> snapshot)
        {
            if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
            {
                GetPhotonView().RPC("RPC_ApplyFullSnapshotBinary", RpcTarget.Others, cacheName, snapshot);
            }
        }

        public static void SendSnapshot<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                GetPhotonView().RPC("RPC_ReceiveSnapshot", RpcTarget.MasterClient, cacheName, key, value);
            }
        }

        public static void SendSnapshotBinary<TKey>(string cacheName, TKey key, byte[] data)
        {
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                GetPhotonView().RPC("RPC_ReceiveSnapshotBinary", RpcTarget.MasterClient, cacheName, key, data);
            }
        }

        public static void SendMergeRequest<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                GetPhotonView().RPC("RPC_ReceiveMergeRequest", RpcTarget.MasterClient, cacheName, key, value);
            }
        }

        public static void SendMergeRequestBinary<TKey>(string cacheName, TKey key, byte[] data)
        {
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                GetPhotonView().RPC("RPC_ReceiveMergeRequestBinary", RpcTarget.MasterClient, cacheName, key, data);
            }
        }

        public static void SendFullSnapshotToPlayer(string cacheName, Hashtable snapshot, int targetPlayerViewId)
        {
            GetPhotonView().RPC("RPC_ReceiveFullSnapshot", RpcTarget.Others, cacheName, snapshot, targetPlayerViewId);
        }

        public static void SendFullSnapshotBinaryToPlayer(string cacheName, Dictionary<object, byte[]> snapshot, int targetPlayerViewId)
        {
            GetPhotonView().RPC("RPC_ReceiveFullSnapshotBinary", RpcTarget.Others, cacheName, snapshot, targetPlayerViewId);
        }

        [PunRPC]
        private static void RPC_ApplyData(string cacheName, object key, object value, PhotonMessageInfo info)
        {
            if (info.Sender == PhotonNetwork.LocalPlayer) return;
            ApplyRemoteData(cacheName, key, value);
        }

        [PunRPC]
        private static void RPC_ApplyDataBinary(string cacheName, object key, byte[] data, PhotonMessageInfo info)
        {
            if (info.Sender == PhotonNetwork.LocalPlayer) return;
            ApplyRemoteDataBinary(cacheName, key, data);
        }

        [PunRPC]
        private static void RPC_ApplyRemove(string cacheName, object key, PhotonMessageInfo info)
        {
            if (info.Sender == PhotonNetwork.LocalPlayer) return;
            ApplyRemoteRemove(cacheName, key);
        }

        [PunRPC]
        private static void RPC_ApplyClear(string cacheName, PhotonMessageInfo info)
        {
            if (info.Sender == PhotonNetwork.LocalPlayer) return;
            ApplyRemoteClear(cacheName);
        }

        [PunRPC]
        private static void RPC_ApplyFullSnapshot(string cacheName, Hashtable snapshot, PhotonMessageInfo info)
        {
            if (info.Sender == PhotonNetwork.LocalPlayer) return;
            ApplyFullSnapshot(cacheName, snapshot);
        }

        [PunRPC]
        private static void RPC_ApplyFullSnapshotBinary(string cacheName, Dictionary<object, byte[]> snapshot, PhotonMessageInfo info)
        {
            if (info.Sender == PhotonNetwork.LocalPlayer) return;
            ApplyFullSnapshotBinary(cacheName, snapshot);
        }

        [PunRPC]
        private static void RPC_ReceiveSnapshot(string cacheName, object key, object value, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            ApplyRemoteData(cacheName, key, value);
        }

        [PunRPC]
        private static void RPC_ReceiveSnapshotBinary(string cacheName, object key, byte[] data, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            ApplyRemoteDataBinary(cacheName, key, data);
        }

        [PunRPC]
        private static void RPC_ReceiveMergeRequest(string cacheName, object key, object value, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            ApplyMergeRequest(cacheName, key, value);
        }

        [PunRPC]
        private static void RPC_ReceiveMergeRequestBinary(string cacheName, object key, byte[] data, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            ApplyMergeRequestBinary(cacheName, key, data);
        }

        [PunRPC]
        private static void RPC_ReceiveFullSnapshot(string cacheName, Hashtable snapshot, int targetPlayerViewId, PhotonMessageInfo info)
        {
            if (info.Sender != PhotonNetwork.MasterClient) return;
            ApplyFullSnapshot(cacheName, snapshot);
        }

        [PunRPC]
        private static void RPC_ReceiveFullSnapshotBinary(string cacheName, Dictionary<object, byte[]> snapshot, int targetPlayerViewId, PhotonMessageInfo info)
        {
            if (info.Sender != PhotonNetwork.MasterClient) return;
            ApplyFullSnapshotBinary(cacheName, snapshot);
        }

        private static void ApplyRemoteData(string cacheName, object key, object value)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();
            MethodCache methods = GetOrCreateMethodCache(cacheType);
            MethodInfo method = methods.ApplyRemoteSetObject ?? methods.ApplyRemoteSet;
            if (method == null) return;

            Type[] genericArgs = cacheType.GenericTypeArguments;
            try
            {
                object? keyConverted = Convert.ChangeType(key, genericArgs[0]);
                if (keyConverted == null)
                {
                    Core.CommonPlugin.Logger.LogError($"类型转换失败：key={key}");
                    return;
                }

                if (method == methods.ApplyRemoteSetObject)
                {
                    method.Invoke(cacheObj, new object[] { keyConverted, value });
                }
                else
                {
                    object? valueConverted = Convert.ChangeType(value, genericArgs[1]);
                    if (valueConverted == null)
                    {
                        Core.CommonPlugin.Logger.LogError($"类型转换失败：value={value}");
                        return;
                    }
                    method.Invoke(cacheObj, new object[] { keyConverted, valueConverted });
                }
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"应用远程数据失败 [{cacheName}]: {ex}");
            }
        }

        private static void ApplyRemoteDataBinary(string cacheName, object key, byte[] data)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();
            MethodCache methods = GetOrCreateMethodCache(cacheType);
            MethodInfo method = methods.ApplyRemoteSetBinary;
            if (method == null) return;

            Type[] genericArgs = cacheType.GenericTypeArguments;
            try
            {
                object? keyConverted = Convert.ChangeType(key, genericArgs[0]);
                if (keyConverted == null)
                {
                    Core.CommonPlugin.Logger.LogError($"类型转换失败：key={key}");
                    return;
                }
                method.Invoke(cacheObj, new object[] { keyConverted, data });
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"应用二进制数据失败 [{cacheName}]: {ex}");
            }
        }

        private static void ApplyRemoteRemove(string cacheName, object key)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();
            MethodCache methods = GetOrCreateMethodCache(cacheType);
            MethodInfo method = methods.ApplyRemoteRemove;
            if (method == null) return;

            Type[] genericArgs = cacheType.GenericTypeArguments;
            try
            {
                object? keyConverted = Convert.ChangeType(key, genericArgs[0]);
                if (keyConverted == null)
                {
                    Core.CommonPlugin.Logger.LogError($"类型转换失败：key={key}");
                    return;
                }
                method.Invoke(cacheObj, new object[] { keyConverted });
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"远程删除失败 [{cacheName}]: {ex}");
            }
        }

        private static void ApplyRemoteClear(string cacheName)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();
            MethodCache methods = GetOrCreateMethodCache(cacheType);
            MethodInfo method = methods.ApplyRemoteClear;
            method?.Invoke(cacheObj, null);
        }

        private static void ApplyFullSnapshot(string cacheName, Hashtable snapshot)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();
            MethodCache methods = GetOrCreateMethodCache(cacheType);
            MethodInfo clearMethod = methods.ApplyRemoteClear;
            clearMethod?.Invoke(cacheObj, null);

            MethodInfo setMethod = methods.ApplyRemoteSetObject ?? methods.ApplyRemoteSet;
            if (setMethod == null) return;

            Type[] genericArgs = cacheType.GenericTypeArguments;
            foreach (object key in snapshot.Keys)
            {
                try
                {
                    object? keyConverted = Convert.ChangeType(key, genericArgs[0]);
                    if (keyConverted == null)
                    {
                        Core.CommonPlugin.Logger.LogError($"类型转换失败：key={key}");
                        continue;
                    }

                    if (setMethod == methods.ApplyRemoteSetObject)
                    {
                        setMethod.Invoke(cacheObj, new object[] { keyConverted, snapshot[key] });
                    }
                    else
                    {
                        object? valueConverted = Convert.ChangeType(snapshot[key], genericArgs[1]);
                        if (valueConverted == null)
                        {
                            Core.CommonPlugin.Logger.LogError($"类型转换失败：value={snapshot[key]}");
                            continue;
                        }
                        setMethod.Invoke(cacheObj, new object[] { keyConverted, valueConverted });
                    }
                }
                catch (Exception ex)
                {
                    Core.CommonPlugin.Logger.LogError($"应用全量快照失败 [{cacheName}]: {ex}");
                }
            }
        }

        private static void ApplyFullSnapshotBinary(string cacheName, Dictionary<object, byte[]> snapshot)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();
            MethodCache methods = GetOrCreateMethodCache(cacheType);
            MethodInfo clearMethod = methods.ApplyRemoteClear;
            clearMethod?.Invoke(cacheObj, null);

            MethodInfo setMethod = methods.ApplyRemoteSetBinary;
            if (setMethod == null) return;

            Type[] genericArgs = cacheType.GenericTypeArguments;
            foreach (KeyValuePair<object, byte[]> kv in snapshot)
            {
                try
                {
                    object? keyConverted = Convert.ChangeType(kv.Key, genericArgs[0]);
                    if (keyConverted == null)
                    {
                        Core.CommonPlugin.Logger.LogError($"类型转换失败：key={kv.Key}");
                        continue;
                    }
                    setMethod.Invoke(cacheObj, new object[] { keyConverted, kv.Value });
                }
                catch (Exception ex)
                {
                    Core.CommonPlugin.Logger.LogError($"应用二进制全量快照失败 [{cacheName}]: {ex}");
                }
            }
        }

        private static void ApplyMergeRequest(string cacheName, object key, object value)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();
            PropertyInfo modeProp = cacheType.GetProperty("Mode");
            if (modeProp == null || (SyncMode)modeProp.GetValue(cacheObj) != SyncMode.Merge) return;

            FieldInfo mergeFuncField = cacheType.GetField("_mergeFunc", BindingFlags.NonPublic | BindingFlags.Instance);
            Delegate? mergeFunc = mergeFuncField?.GetValue(cacheObj) as Delegate;
            if (mergeFunc == null) return;

            MethodCache methods = GetOrCreateMethodCache(cacheType);
            MethodInfo tryGetMethod = methods.TryGet;
            if (tryGetMethod == null) return;

            Type[] genericArgs = cacheType.GenericTypeArguments;
            try
            {
                object? keyConverted = Convert.ChangeType(key, genericArgs[0]);
                if (keyConverted == null)
                {
                    Core.CommonPlugin.Logger.LogError($"类型转换失败：key={key}");
                    return;
                }

                object[] parameters = new object[] { keyConverted, null! };
                if ((bool)tryGetMethod.Invoke(cacheObj, parameters))
                {
                    object currentValue = parameters[1];
                    object? valueConverted = Convert.ChangeType(value, genericArgs[1]);
                    if (valueConverted == null)
                    {
                        Core.CommonPlugin.Logger.LogError($"类型转换失败：value={value}");
                        return;
                    }
                    object mergedValue = mergeFunc.DynamicInvoke(currentValue, valueConverted)!;
                    MethodInfo setMethod = methods.ApplyRemoteSetObject ?? methods.ApplyRemoteSet;
                    if (setMethod == null) return;
                    if (setMethod == methods.ApplyRemoteSetObject)
                    {
                        setMethod.Invoke(cacheObj, new object[] { keyConverted, mergedValue });
                        BroadcastData<object, object>(cacheName, keyConverted, mergedValue);
                    }
                    else
                    {
                        setMethod.Invoke(cacheObj, new object[] { keyConverted, mergedValue });
                        BroadcastData<object, object>(cacheName, keyConverted, mergedValue);
                    }
                }
                else
                {
                    object? valueConverted = Convert.ChangeType(value, genericArgs[1]);
                    if (valueConverted == null)
                    {
                        Core.CommonPlugin.Logger.LogError($"类型转换失败：value={value}");
                        return;
                    }
                    MethodInfo setMethod = methods.ApplyRemoteSetObject ?? methods.ApplyRemoteSet;
                    if (setMethod == null) return;
                    if (setMethod == methods.ApplyRemoteSetObject)
                    {
                        setMethod.Invoke(cacheObj, new object[] { keyConverted, valueConverted });
                        BroadcastData<object, object>(cacheName, keyConverted, valueConverted);
                    }
                    else
                    {
                        setMethod.Invoke(cacheObj, new object[] { keyConverted, valueConverted });
                        BroadcastData<object, object>(cacheName, keyConverted, valueConverted);
                    }
                }
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"合并请求处理失败 [{cacheName}]: {ex}");
            }
        }

        private static void ApplyMergeRequestBinary(string cacheName, object key, byte[] data)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();
            PropertyInfo modeProp = cacheType.GetProperty("Mode");
            if (modeProp == null || (SyncMode)modeProp.GetValue(cacheObj) != SyncMode.Merge) return;

            FieldInfo deserializeField = cacheType.GetField("_deserialize", BindingFlags.NonPublic | BindingFlags.Instance);
            Delegate? deserialize = deserializeField?.GetValue(cacheObj) as Delegate;
            if (deserialize == null) return;

            FieldInfo mergeFuncField = cacheType.GetField("_mergeFunc", BindingFlags.NonPublic | BindingFlags.Instance);
            Delegate? mergeFunc = mergeFuncField?.GetValue(cacheObj) as Delegate;
            if (mergeFunc == null) return;

            MethodCache methods = GetOrCreateMethodCache(cacheType);
            MethodInfo tryGetMethod = methods.TryGet;
            if (tryGetMethod == null) return;

            Type[] genericArgs = cacheType.GenericTypeArguments;
            try
            {
                object? keyConverted = Convert.ChangeType(key, genericArgs[0]);
                if (keyConverted == null)
                {
                    Core.CommonPlugin.Logger.LogError($"类型转换失败：key={key}");
                    return;
                }

                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    object typedValue = deserialize.DynamicInvoke(reader)!;
                    object[] parameters = new object[] { keyConverted, null! };
                    if ((bool)tryGetMethod.Invoke(cacheObj, parameters))
                    {
                        object currentValue = parameters[1];
                        object mergedValue = mergeFunc.DynamicInvoke(currentValue, typedValue)!;
                        MethodInfo setMethod = methods.ApplyRemoteSetBinary;
                        if (setMethod != null)
                        {
                            using (MemoryStream msOut = new MemoryStream())
                            using (BinaryWriter writer = new BinaryWriter(msOut))
                            {
                                FieldInfo serializeField = cacheType.GetField("_serialize", BindingFlags.NonPublic | BindingFlags.Instance);
                                Delegate? serialize = serializeField?.GetValue(cacheObj) as Delegate;
                                if (serialize != null)
                                {
                                    serialize.DynamicInvoke(writer, mergedValue);
                                    byte[] outData = msOut.ToArray();
                                    setMethod.Invoke(cacheObj, new object[] { keyConverted, outData });
                                    BroadcastDataBinary<object>(cacheName, keyConverted, outData);
                                }
                            }
                        }
                    }
                    else
                    {
                        MethodInfo setMethod = methods.ApplyRemoteSetBinary;
                        if (setMethod != null)
                        {
                            setMethod.Invoke(cacheObj, new object[] { keyConverted, data });
                            BroadcastDataBinary<object>(cacheName, keyConverted, data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"二进制合并请求处理失败 [{cacheName}]: {ex}");
            }
        }
    }
}