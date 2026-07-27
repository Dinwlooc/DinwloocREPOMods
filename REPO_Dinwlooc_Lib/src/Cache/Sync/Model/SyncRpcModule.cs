using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace Dinwlooc.Common.Sync
{
    public static class SyncRpcModule
    {
        private const byte FALLBACK_EVENT_CODE = 200;

        private static bool _isInitialized = false;
        private static readonly object _initLock = new object();
        private static bool _useREPOLib = false;
        private static object? _networkedEvent = null;
        private static Action<object, RaiseEventOptions, SendOptions>? _raiseEventDelegate = null;
        private static IOnEventCallback? _eventListener = null;

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

        /// <summary>
        /// 重置初始化状态，用于网络重连后重新注册监听器。
        /// </summary>
        public static void Reset()
        {
            lock (_initLock)
            {
                if (_eventListener != null)
                {
                    PhotonNetwork.RemoveCallbackTarget(_eventListener);
                    _eventListener = null;
                }
                _networkedEvent = null;
                _raiseEventDelegate = null;
                _useREPOLib = false;
                _isInitialized = false;
                Core.CommonPlugin.Logger.LogInfo("SyncRpcModule 已重置，下次发送时将重新初始化。");
            }
        }

        private static void EnsureInitialized()
        {
            if (_isInitialized) return;
            lock (_initLock)
            {
                if (_isInitialized) return;

                if (PhotonNetwork.IsConnected)
                {
                    if (TryInitializeWithREPOLib())
                    {
                        _isInitialized = true;
                        Core.CommonPlugin.Logger.LogInfo("SyncRpcModule 已初始化（REPOLib NetworkedEvent 通道）。");
                        return;
                    }
                    InitializeFallback();
                    _isInitialized = true;
                    Core.CommonPlugin.Logger.LogInfo($"SyncRpcModule 已初始化（自研 Fallback，事件码 {FALLBACK_EVENT_CODE}）。");
                }
                else
                {
                    // 未连接时标记为已初始化，但实际不注册任何监听器，后续发送时若连接会再尝试
                    _isInitialized = true;
                    Core.CommonPlugin.Logger.LogInfo("SyncRpcModule 在 Photon 未连接时初始化（无监听器注册，将在发送时重试）。");
                }
            }
        }

        private static bool TryInitializeWithREPOLib()
        {
            try
            {
                Type? networkEventType = Type.GetType("REPOLib.Modules.NetworkEvent, REPOLib");
                if (networkEventType == null) return false;

                Action<EventData> onEvent = OnNetworkedEventReceived;
                _networkedEvent = Activator.CreateInstance(networkEventType, "Dinwlooc_Sync", onEvent);
                if (_networkedEvent == null) return false;

                MethodInfo? raiseMethod = networkEventType.GetMethod("RaiseEvent", new Type[] { typeof(object), typeof(RaiseEventOptions), typeof(SendOptions) });
                if (raiseMethod == null)
                {
                    _networkedEvent = null;
                    return false;
                }

                _raiseEventDelegate = (Action<object, RaiseEventOptions, SendOptions>)Delegate.CreateDelegate(
                    typeof(Action<object, RaiseEventOptions, SendOptions>), _networkedEvent, raiseMethod);
                _useREPOLib = true;
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

        private static void InitializeFallback()
        {
            if (_eventListener != null) return;
            _eventListener = new EventListener(FALLBACK_EVENT_CODE);
            PhotonNetwork.AddCallbackTarget(_eventListener);
            _useREPOLib = false;
        }

        private static void OnNetworkedEventReceived(EventData photonEvent)
        {
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

        private class EventListener : IOnEventCallback
        {
            private readonly byte _eventCode;
            public EventListener(byte eventCode) => _eventCode = eventCode;
            public void OnEvent(EventData photonEvent)
            {
                if (photonEvent.Code != _eventCode) return;
                OnNetworkedEventReceived(photonEvent);
            }
        }

        private static void SendEvent(SubOpCode op, string cacheName, object? key, object? value, RaiseEventOptions options)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
                return;

            // 如果已初始化但未注册（可能因网络重连导致监听器丢失），重新尝试初始化
            if (_isInitialized && _eventListener == null && !_useREPOLib)
            {
                Reset();
            }

            EnsureInitialized();

            // 如果仍未注册，可能因网络问题，直接返回
            if (_eventListener == null && !_useREPOLib)
            {
                Core.CommonPlugin.Logger.LogWarning("SyncRpcModule 未注册监听器，无法发送事件。");
                return;
            }

            Hashtable data = new Hashtable
            {
                ["op"] = (byte)op,
                ["c"] = cacheName
            };
            if (key != null) data["k"] = key;
            if (value != null) data["v"] = value;

            if (_useREPOLib && _raiseEventDelegate != null)
            {
                _raiseEventDelegate(data, options, SendOptions.SendReliable);
            }
            else
            {
                PhotonNetwork.RaiseEvent(FALLBACK_EVENT_CODE, data, options, SendOptions.SendReliable);
            }
        }

        // ---- 公开 API ----
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
            foreach (var kv in snapshot) hashtable[kv.Key] = kv.Value;
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