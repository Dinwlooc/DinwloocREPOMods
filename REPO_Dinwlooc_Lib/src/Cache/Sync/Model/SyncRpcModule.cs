using System;
using System.Collections.Generic;
using System.Reflection;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace Dinwlooc.Common.Sync
{
    public static class SyncRpcModule
    {
        private const string LOG_TAG = "[SyncRpcModule]";

        private static bool _isInitialized = false;
        private static readonly object _initLock = new object();
        private static bool _useREPOLib = false;
        private static object? _networkedEvent = null;
        private static Action<object, RaiseEventOptions, SendOptions>? _raiseEventDelegate = null;
        private static IOnEventCallback? _eventListener = null;

        // 自适应事件码相关
        private static int _currentFallbackCode = 200;        // 当前使用的原生事件码
        private static bool _codeConflictDetected = false;
        private static readonly object _eventCodeLock = new object();

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
        /// 显式初始化网络监听器（必须在加入房间后、任何发送前调用）。
        /// 若网络未就绪，不会标记为已初始化，发送时会再次尝试。
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_isInitialized && (_useREPOLib || _eventListener != null))
                return;

            lock (_initLock)
            {
                if (_isInitialized && (_useREPOLib || _eventListener != null))
                    return;

                if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
                {
                    if (TryInitializeWithREPOLib())
                    {
                        _isInitialized = true;
                        Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 初始化成功（REPOLib 通道）。");
                        return;
                    }
                    InitializeFallback();
                    _isInitialized = true;
                    Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 初始化成功（原生 Fallback，事件码 {_currentFallbackCode}）。");
                }
                else
                {
                    // 网络未就绪，不标记为已初始化，下次发送时重试
                    _isInitialized = false;
                    Core.CommonPlugin.Logger.LogWarning($"{LOG_TAG} 网络未就绪，延迟初始化。");
                }
            }
        }

        /// <summary>
        /// 重置监听器（断开连接或离开房间时调用）。
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
                // 重置事件码相关状态
                lock (_eventCodeLock)
                {
                    _currentFallbackCode = 200;
                    _codeConflictDetected = false;
                }
                Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 已重置。");
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
                Core.CommonPlugin.Logger.LogError($"{LOG_TAG} 初始化 REPOLib 失败: {ex.Message}");
                _networkedEvent = null;
                _raiseEventDelegate = null;
                return false;
            }
        }

        private static void InitializeFallback()
        {
            if (_eventListener != null) return;
            _eventListener = new EventListener((byte)_currentFallbackCode);
            PhotonNetwork.AddCallbackTarget(_eventListener);
            _useREPOLib = false;
        }

        private static void OnNetworkedEventReceived(EventData photonEvent)
        {
            // 如果数据不是 Hashtable 或缺少必要字段，说明该事件码被其他插件占用
            if (photonEvent.CustomData is not PhotonHashtable data || !data.ContainsKey("op"))
            {
                // 仅当事件码等于我们当前使用的码时才触发冲突处理
                if (photonEvent.Code == _currentFallbackCode)
                {
                    Core.CommonPlugin.Logger.LogWarning($"{LOG_TAG} 检测到事件码 {_currentFallbackCode} 被其他插件占用，触发切换。");
                    lock (_eventCodeLock)
                    {
                        _codeConflictDetected = true;
                    }
                }
                return;
            }

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
                    SyncRpcProcessor.ApplyFullSnapshot(cacheName, (PhotonHashtable)data["v"]);
                    break;
                case (byte)SubOpCode.ApplyFullSnapshotBinary:
                    SyncRpcProcessor.ApplyFullSnapshotBinary(cacheName, (Dictionary<object, byte[]>)data["v"]);
                    break;
                case (byte)SubOpCode.ReceiveSnapshot:
                case (byte)SubOpCode.ReceiveSnapshotBinary:
                case (byte)SubOpCode.ReceiveMergeRequest:
                case (byte)SubOpCode.ReceiveMergeRequestBinary:
                case (byte)SubOpCode.ReceiveFullSnapshot:
                case (byte)SubOpCode.ReceiveFullSnapshotBinary:
                    SyncRpcProcessor.HandleSubOp(op, cacheName, data);
                    break;
                default:
                    Core.CommonPlugin.Logger.LogWarning($"{LOG_TAG} 未知操作码: {op}");
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

        /// <summary>
        /// 核心发送方法——所有网络事件必经之路。
        /// 若网络未就绪，事件被丢弃且仅记录警告，绝不影响游戏启动。
        /// </summary>
        private static void SendEvent(SubOpCode op, string cacheName, object? key, object? value, RaiseEventOptions options)
        {
            // 第一道防线：检查 Photon 连接状态
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                Core.CommonPlugin.Logger.LogWarning($"{LOG_TAG} 网络未连接或未在房间内，事件被丢弃（缓存：{cacheName}，操作：{op}）。");
                return;
            }

            // 确保监听器已初始化
            EnsureInitialized();

            PhotonHashtable data = new PhotonHashtable
            {
                ["op"] = (byte)op,
                ["c"] = cacheName
            };
            if (key != null) data["k"] = key;
            if (value != null) data["v"] = value;

            // === 第一层：尝试 REPOLib ===
            if (_useREPOLib && _raiseEventDelegate != null)
            {
                try
                {
                    _raiseEventDelegate(data, options, SendOptions.SendReliable);
                    Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} REPOLib 发送成功 (缓存:{cacheName}, 操作:{op})");
                    return;
                }
                catch (Exception ex)
                {
                    Core.CommonPlugin.Logger.LogError($"{LOG_TAG} REPOLib 发送失败，降级到原生通道。错误: {ex.Message}");
                    _useREPOLib = false;
                    _isInitialized = false;
                    InitializeFallback();
                }
            }

            // === 第二层：原生 Photon 发送（自适应事件码） ===
            bool sent = false;
            int attempts = 0;
            const int maxAttempts = 56; // 200~255 共56个码

            while (!sent && attempts < maxAttempts)
            {
                // 若检测到冲突，先轮转（并计入尝试次数）
                if (_codeConflictDetected)
                {
                    lock (_eventCodeLock)
                    {
                        _currentFallbackCode = (_currentFallbackCode - 200 + 1) % 56 + 200;
                        _codeConflictDetected = false;
                        // 重新注册监听器
                        if (_eventListener != null)
                        {
                            PhotonNetwork.RemoveCallbackTarget(_eventListener);
                            _eventListener = null;
                        }
                        _eventListener = new EventListener((byte)_currentFallbackCode);
                        PhotonNetwork.AddCallbackTarget(_eventListener);
                        Core.CommonPlugin.Logger.LogInfo($"{LOG_TAG} 事件码冲突，切换到 {_currentFallbackCode}");
                    }
                    attempts++; // 冲突切换也算一次尝试
                    continue;   // 重新循环，使用新码发送
                }

                try
                {
                    PhotonNetwork.RaiseEvent((byte)_currentFallbackCode, data, options, SendOptions.SendReliable);
                    sent = true;
                }
                catch (Exception ex)
                {
                    // 发送异常（如通道满），轮转码并增加尝试次数
                    Core.CommonPlugin.Logger.LogWarning($"{LOG_TAG} 原生发送失败 (码{_currentFallbackCode})，尝试切换。错误: {ex.Message}");
                    lock (_eventCodeLock)
                    {
                        _currentFallbackCode = (_currentFallbackCode - 200 + 1) % 56 + 200;
                        if (_eventListener != null)
                        {
                            PhotonNetwork.RemoveCallbackTarget(_eventListener);
                            _eventListener = null;
                        }
                        _eventListener = new EventListener((byte)_currentFallbackCode);
                        PhotonNetwork.AddCallbackTarget(_eventListener);
                    }
                    attempts++; // 发送异常也算一次尝试
                }
            }

            if (!sent)
            {
                Core.CommonPlugin.Logger.LogError($"{LOG_TAG} 所有事件码 (200~255) 均尝试失败，事件彻底丢弃。");
            }
        }

        // ---- 公共 API ----
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

        public static void BroadcastFullSnapshot(string cacheName, PhotonHashtable snapshot)
        {
            if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendEvent(SubOpCode.ApplyFullSnapshot, cacheName, null, snapshot, options);
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

        public static void SendFullSnapshotToPlayer(string cacheName, PhotonHashtable snapshot, int targetActorNumber)
        {
            if (!PhotonNetwork.IsConnected) return;
            RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { targetActorNumber } };
            SendEvent(SubOpCode.ReceiveFullSnapshot, cacheName, null, snapshot, options);
        }

        public static void SendFullSnapshotBinaryToPlayer(string cacheName, Dictionary<object, byte[]> snapshot, int targetActorNumber)
        {
            if (!PhotonNetwork.IsConnected) return;
            RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { targetActorNumber } };
            SendEvent(SubOpCode.ReceiveFullSnapshotBinary, cacheName, null, snapshot, options);
        }
    }
}