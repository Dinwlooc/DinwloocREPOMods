using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Reflection;
using ExitGames.Client.Photon;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 同步 RPC 业务处理器，负责处理所有远程数据应用逻辑。
    /// 使用全局反射缓存提高性能。
    /// </summary>
    internal static class SyncRpcProcessor
    {
        /// <summary>
        /// 应用单条远程数据（Hashtable 模式）
        /// </summary>
        internal static void ApplyRemoteData(string cacheName, object key, object value)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();

            MethodInfo? method = ReflectionCache.GetMethod(cacheType, "ApplyRemoteSetObject");
            if (method == null) method = ReflectionCache.GetMethod(cacheType, "ApplyRemoteSet");
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

                if (method.Name == "ApplyRemoteSetObject")
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

        /// <summary>
        /// 应用单条远程数据（二进制模式）
        /// </summary>
        internal static void ApplyRemoteDataBinary(string cacheName, object key, byte[] data)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();
            MethodInfo? method = ReflectionCache.GetMethod(cacheType, "ApplyRemoteSetBinary");
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

        /// <summary>
        /// 应用远程删除
        /// </summary>
        internal static void ApplyRemoteRemove(string cacheName, object key)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();
            MethodInfo? method = ReflectionCache.GetMethod(cacheType, "ApplyRemoteRemove");
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

        /// <summary>
        /// 应用远程清空
        /// </summary>
        internal static void ApplyRemoteClear(string cacheName)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();
            MethodInfo? method = ReflectionCache.GetMethod(cacheType, "ApplyRemoteClear");
            method?.Invoke(cacheObj, null);
        }

        /// <summary>
        /// 应用全量快照（Hashtable 模式）
        /// </summary>
        internal static void ApplyFullSnapshot(string cacheName, Hashtable snapshot)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();

            MethodInfo? clearMethod = ReflectionCache.GetMethod(cacheType, "ApplyRemoteClear");
            clearMethod?.Invoke(cacheObj, null);

            MethodInfo? setMethod = ReflectionCache.GetMethod(cacheType, "ApplyRemoteSetObject");
            if (setMethod == null) setMethod = ReflectionCache.GetMethod(cacheType, "ApplyRemoteSet");
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

                    if (setMethod.Name == "ApplyRemoteSetObject")
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

        /// <summary>
        /// 应用全量快照（二进制模式）
        /// </summary>
        internal static void ApplyFullSnapshotBinary(string cacheName, Dictionary<object, byte[]> snapshot)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();

            MethodInfo? clearMethod = ReflectionCache.GetMethod(cacheType, "ApplyRemoteClear");
            clearMethod?.Invoke(cacheObj, null);

            MethodInfo? setMethod = ReflectionCache.GetMethod(cacheType, "ApplyRemoteSetBinary");
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

        /// <summary>
        /// 应用合并请求（Hashtable 模式）
        /// </summary>
        internal static void ApplyMergeRequest(string cacheName, object key, object value)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();

            PropertyInfo? modeProp = ReflectionCache.GetProperty(cacheType, "Mode");
            if (modeProp == null) return;
            if ((SyncMode)modeProp.GetValue(cacheObj) != SyncMode.Merge) return;

            FieldInfo? mergeFuncField = ReflectionCache.GetField(cacheType, "_mergeFunc");
            Delegate? mergeFunc = mergeFuncField?.GetValue(cacheObj) as Delegate;
            if (mergeFunc == null) return;

            MethodInfo? tryGetMethod = ReflectionCache.GetMethod(cacheType, "TryGet", BindingFlags.Public | BindingFlags.Instance);
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

                    MethodInfo? setMethod = ReflectionCache.GetMethod(cacheType, "ApplyRemoteSetObject");
                    if (setMethod == null) setMethod = ReflectionCache.GetMethod(cacheType, "ApplyRemoteSet");
                    if (setMethod == null) return;

                    if (setMethod.Name == "ApplyRemoteSetObject")
                    {
                        setMethod.Invoke(cacheObj, new object[] { keyConverted, mergedValue });
                        SyncRpcModule.BroadcastData<object, object>(cacheName, keyConverted, mergedValue);
                    }
                    else
                    {
                        setMethod.Invoke(cacheObj, new object[] { keyConverted, mergedValue });
                        SyncRpcModule.BroadcastData<object, object>(cacheName, keyConverted, mergedValue);
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
                    MethodInfo? setMethod = ReflectionCache.GetMethod(cacheType, "ApplyRemoteSetObject");
                    if (setMethod == null) setMethod = ReflectionCache.GetMethod(cacheType, "ApplyRemoteSet");
                    if (setMethod == null) return;

                    if (setMethod.Name == "ApplyRemoteSetObject")
                    {
                        setMethod.Invoke(cacheObj, new object[] { keyConverted, valueConverted });
                        SyncRpcModule.BroadcastData<object, object>(cacheName, keyConverted, valueConverted);
                    }
                    else
                    {
                        setMethod.Invoke(cacheObj, new object[] { keyConverted, valueConverted });
                        SyncRpcModule.BroadcastData<object, object>(cacheName, keyConverted, valueConverted);
                    }
                }
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"合并请求处理失败 [{cacheName}]: {ex}");
            }
        }

        /// <summary>
        /// 应用合并请求（二进制模式）
        /// </summary>
        internal static void ApplyMergeRequestBinary(string cacheName, object key, byte[] data)
        {
            if (!SyncRegionManager.Instance.SyncCaches.TryGetValue(cacheName, out object cacheObj)) return;
            Type cacheType = cacheObj.GetType();

            PropertyInfo? modeProp = ReflectionCache.GetProperty(cacheType, "Mode");
            if (modeProp == null) return;
            if ((SyncMode)modeProp.GetValue(cacheObj) != SyncMode.Merge) return;

            FieldInfo? deserializeField = ReflectionCache.GetField(cacheType, "_deserialize");
            Delegate? deserialize = deserializeField?.GetValue(cacheObj) as Delegate;
            if (deserialize == null) return;

            FieldInfo? mergeFuncField = ReflectionCache.GetField(cacheType, "_mergeFunc");
            Delegate? mergeFunc = mergeFuncField?.GetValue(cacheObj) as Delegate;
            if (mergeFunc == null) return;

            MethodInfo? tryGetMethod = ReflectionCache.GetMethod(cacheType, "TryGet", BindingFlags.Public | BindingFlags.Instance);
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

                        MethodInfo? setMethod = ReflectionCache.GetMethod(cacheType, "ApplyRemoteSetBinary");
                        if (setMethod != null)
                        {
                            using (MemoryStream msOut = new MemoryStream())
                            using (BinaryWriter writer = new BinaryWriter(msOut))
                            {
                                FieldInfo? serializeField = ReflectionCache.GetField(cacheType, "_serialize");
                                Delegate? serialize = serializeField?.GetValue(cacheObj) as Delegate;
                                if (serialize != null)
                                {
                                    serialize.DynamicInvoke(writer, mergedValue);
                                    byte[] outData = msOut.ToArray();
                                    setMethod.Invoke(cacheObj, new object[] { keyConverted, outData });
                                    SyncRpcModule.BroadcastDataBinary<object>(cacheName, keyConverted, outData);
                                }
                            }
                        }
                    }
                    else
                    {
                        MethodInfo? setMethod = ReflectionCache.GetMethod(cacheType, "ApplyRemoteSetBinary");
                        if (setMethod != null)
                        {
                            setMethod.Invoke(cacheObj, new object[] { keyConverted, data });
                            SyncRpcModule.BroadcastDataBinary<object>(cacheName, keyConverted, data);
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