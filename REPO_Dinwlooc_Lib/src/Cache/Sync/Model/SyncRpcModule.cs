using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 同步 RPC 通信模块，优先使用 REPOLib 的 NetworkedEvent 通道，
    /// 若不可用则回退到自研的 RaiseEvent 实现（固定事件码 200）。
    /// </summary>
    public static class SyncRpcModule
    {
        // 自研方案的事件码（仅在 REPOLib 不可用时使用）
        private const byte FALLBACK_EVENT_CODE = 200;

        // 是否已初始化
        private static bool _isInitialized = false;
        private static readonly object _initLock = new object();

        // 是否使用 REPOLib 通道
        private static bool _useREPOLib = false;

        // 当使用 REPOLib 时的 NetworkedEvent 实例
        private static object? _networkedEvent = null;

        // 当使用 REPOLib 时，缓存其 RaiseEvent 方法委托（提升性能）
        private static Action<object, RaiseEventOptions, SendOptions>? _raiseEventDelegate = null;

        // 当使用自研方案时的 EventListener 实例
        private static IOnEventCallback? _eventListener = null;

        // 子操作码
        private enum SubOpCode : byte
        {
            ApplyData,
            ApplyDataBinary,
            ApplyRemove,
            ApplyClear,
            ApplyFullSnapshot,
            ApplyFullSnapshotBinary,
            ReceiveSnapshot,
            ReceiveSnapshotBinary,
            ReceiveMergeRequest,
            ReceiveMergeRequestBinary,
            ReceiveFullSnapshot,
            ReceiveFullSnapshotBinary
        }

        // ---- 初始化 ----
        private static void EnsureInitialized()
        {
            if (_isInitialized) return;
            lock (_initLock)
            {
                if (_isInitialized) return;

                // 尝试初始化 REPOLib 通道
                if (!TryInitializeWithREPOLib())
                {
                    // 回退到自研方案
                    InitializeFallback();
                }

                _isInitialized = true;
                Core.CommonPlugin.Logger.LogInfo($"SyncRpcModule 已初始化，使用 {(_useREPOLib ? "REPOLib NetworkedEvent" : "自研 Fallback")} 通道");
            }
        }

        // ---- 尝试使用 REPOLib NetworkedEvent ----
        private static bool TryInitializeWithREPOLib()
        {
            try
            {
                Type? networkEventType = Type.GetType("REPOLib.Modules.NetworkEvent, REPOLib");
                if (networkEventType == null)
                {
                    Core.CommonPlugin.Logger.LogInfo("REPOLib.NetworkEvent 未找到，回退到自研方案");
                    return false;
                }

                // 创建 NetworkedEvent 实例，传入事件名称和回调委托
                Action<EventData> onEvent = OnNetworkedEventReceived;
                _networkedEvent = Activator.CreateInstance(networkEventType, "Dinwlooc_Sync", onEvent);
                if (_networkedEvent == null)
                {
                    Core.CommonPlugin.Logger.LogWarning("创建 NetworkedEvent 失败，回退到自研方案");
                    return false;
                }

                // 缓存 RaiseEvent 方法委托（因为 NetworkedEvent.RaiseEvent 是公开方法）
                MethodInfo? raiseMethod = networkEventType.GetMethod("RaiseEvent", new Type[] { typeof(object), typeof(RaiseEventOptions), typeof(SendOptions) });
                if (raiseMethod == null)
                {
                    Core.CommonPlugin.Logger.LogWarning("NetworkedEvent.RaiseEvent 方法未找到，回退到自研方案");
                    _networkedEvent = null;
                    return false;
                }

                _raiseEventDelegate = (Action<object, RaiseEventOptions, SendOptions>)Delegate.CreateDelegate(
                    typeof(Action<object, RaiseEventOptions, SendOptions>), _networkedEvent, raiseMethod);
                _useREPOLib = true;
                Core.CommonPlugin.Logger.LogInfo("SyncRpcModule 已成功接入 REPOLib NetworkedEvent 通道");
                return true;
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogError($"初始化 REPOLib 通道失败: {ex.Message}");
                _networkedEvent = null;
                _raiseEventDelegate = null;
                return false;
            }
        }

        // ---- REPOLib 事件接收回调 ----
        private static void OnNetworkedEventReceived(EventData photonEvent)
        {
            // 与自研方案的事件处理逻辑完全相同
            if (photonEvent.CustomData is not Hashtable data) return;
            if (!data.ContainsKey("op")) return;

            byte op = (byte)data["op"];
            string cacheName = (string)data["c"];

            switch (op)
            {
                case (byte)SubOpCode.ApplyData:
                    SyncRpcProcessor.ApplyRemoteData(cacheName, data["k"], data["v"]);
                    break;
                case (byte)SubOpCode.ApplyDataBinary:
                    SyncRpcProcessor.ApplyRemoteDataBinary(cacheName, data["k"], (byte[])data["v"]);
                    break;
                case (byte)SubOpCode.ApplyRemove:
                    SyncRpcProcessor.ApplyRemoteRemove(cacheName, data["k"]);
                    break;
                case (byte)SubOpCode.ApplyClear:
                    SyncRpcProcessor.ApplyRemoteClear(cacheName);
                    break;
                case (byte)SubOpCode.ApplyFullSnapshot:
                    SyncRpcProcessor.ApplyFullSnapshot(cacheName, (Hashtable)data["v"]);
                    break;
                case (byte)SubOpCode.ApplyFullSnapshotBinary:
                    SyncRpcProcessor.ApplyFullSnapshotBinary(cacheName, (Dictionary<object, byte[]>)data["v"]);
                    break;
                case (byte)SubOpCode.ReceiveSnapshot:
                    SyncRpcProcessor.ApplyRemoteData(cacheName, data["k"], data["v"]);
                    break;
                case (byte)SubOpCode.ReceiveSnapshotBinary:
                    SyncRpcProcessor.ApplyRemoteDataBinary(cacheName, data["k"], (byte[])data["v"]);
                    break;
                case (byte)SubOpCode.ReceiveMergeRequest:
                    SyncRpcProcessor.ApplyMergeRequest(cacheName, data["k"], data["v"]);
                    break;
                case (byte)SubOpCode.ReceiveMergeRequestBinary:
                    SyncRpcProcessor.ApplyMergeRequestBinary(cacheName, data["k"], (byte[])data["v"]);
                    break;
                case (byte)SubOpCode.ReceiveFullSnapshot:
                    SyncRpcProcessor.ApplyFullSnapshot(cacheName, (Hashtable)data["v"]);
                    break;
                case (byte)SubOpCode.ReceiveFullSnapshotBinary:
                    SyncRpcProcessor.ApplyFullSnapshotBinary(cacheName, (Dictionary<object, byte[]>)data["v"]);
                    break;
            }
        }

        // ---- 自研 Fallback 实现 ----
        private static void InitializeFallback()
        {
            _eventListener = new EventListener(FALLBACK_EVENT_CODE);
            PhotonNetwork.AddCallbackTarget(_eventListener);
            _useREPOLib = false;
        }

        private class EventListener : IOnEventCallback
        {
            private readonly byte _eventCode;

            public EventListener(byte eventCode)
            {
                _eventCode = eventCode;
            }

            public void OnEvent(EventData photonEvent)
            {
                if (photonEvent.Code != _eventCode) return;
                OnNetworkedEventReceived(photonEvent);
            }
        }

        // ---- 发送方法封装 ----
        private static void SendEvent(SubOpCode op, string cacheName, object? key, object? value, RaiseEventOptions options)
        {
            EnsureInitialized();
            if (!PhotonNetwork.IsConnected) return;

            Hashtable data = new Hashtable
            {
                ["op"] = (byte)op,
                ["c"] = cacheName
            };
            if (key != null) data["k"] = key;
            if (value != null) data["v"] = value;

            if (_useREPOLib && _raiseEventDelegate != null)
            {
                // 使用 REPOLib 的 NetworkedEvent 发送
                _raiseEventDelegate(data, options, SendOptions.SendReliable);
            }
            else
            {
                // 使用自研方案发送
                PhotonNetwork.RaiseEvent(FALLBACK_EVENT_CODE, data, options, SendOptions.SendReliable);
            }
        }

        // ---- 公开 API（完全不变） ----
        public static void BroadcastData<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendEvent(SubOpCode.ApplyData, cacheName, key, value, options);
        }

        public static void BroadcastDataBinary<TKey>(string cacheName, TKey key, byte[] data)
        {
            if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendEvent(SubOpCode.ApplyDataBinary, cacheName, key, data, options);
        }

        public static void BroadcastRemove<TKey>(string cacheName, TKey key)
        {
            if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendEvent(SubOpCode.ApplyRemove, cacheName, key, null, options);
        }

        public static void BroadcastClear(string cacheName)
        {
            if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendEvent(SubOpCode.ApplyClear, cacheName, null, null, options);
        }

        public static void BroadcastFullSnapshot<TKey, TValue>(string cacheName, ConcurrentDictionary<TKey, TValue> snapshot)
        {
            if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
            Hashtable hashtable = new Hashtable();
            foreach (var kv in snapshot)
                hashtable[kv.Key] = kv.Value;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendEvent(SubOpCode.ApplyFullSnapshot, cacheName, null, hashtable, options);
        }

        public static void BroadcastFullSnapshotBinary<TKey>(string cacheName, Dictionary<object, byte[]> snapshot)
        {
            if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendEvent(SubOpCode.ApplyFullSnapshotBinary, cacheName, null, snapshot, options);
        }

        public static void SendSnapshot<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            SendEvent(SubOpCode.ReceiveSnapshot, cacheName, key, value, options);
        }

        public static void SendSnapshotBinary<TKey>(string cacheName, TKey key, byte[] data)
        {
            if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            SendEvent(SubOpCode.ReceiveSnapshotBinary, cacheName, key, data, options);
        }

        public static void SendMergeRequest<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            SendEvent(SubOpCode.ReceiveMergeRequest, cacheName, key, value, options);
        }

        public static void SendMergeRequestBinary<TKey>(string cacheName, TKey key, byte[] data)
        {
            if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            SendEvent(SubOpCode.ReceiveMergeRequestBinary, cacheName, key, data, options);
        }

        public static void SendFullSnapshotToPlayer(string cacheName, Hashtable snapshot, int targetPlayerViewId)
        {
            if (!PhotonNetwork.IsConnected) return;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            SendEvent(SubOpCode.ReceiveFullSnapshot, cacheName, null, snapshot, options);
        }

        public static void SendFullSnapshotBinaryToPlayer(string cacheName, Dictionary<object, byte[]> snapshot, int targetPlayerViewId)
        {
            if (!PhotonNetwork.IsConnected) return;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            SendEvent(SubOpCode.ReceiveFullSnapshotBinary, cacheName, null, snapshot, options);
        }
    }
}