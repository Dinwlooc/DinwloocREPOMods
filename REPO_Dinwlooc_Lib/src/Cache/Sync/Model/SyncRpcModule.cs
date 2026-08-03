using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace Dinwlooc.Common.Sync
{
    public static class SyncRpcModule
    {
        // 默认序列化策略（用于自定义请求/响应）
        private static readonly HashtableStrategy<object> _defaultStrategy = new HashtableStrategy<object>();

        // 传输层实例（懒加载）
        private static IRpcTransport? _transport;
        private static readonly object _transportLock = new object();
        private static bool _eventRegistered = false;

        // ---------- 私有方法：确保传输层就绪 ----------
        private static IRpcTransport EnsureTransport()
        {
            lock (_transportLock)
            {
                // 如果传输层不存在，创建并尝试初始化
                if (_transport == null)
                {
                    _transport = TransportManager.GetOrCreateTransport();
                    // 注册事件接收（仅一次）
                    if (!_eventRegistered && _transport != null)
                    {
                        _transport.OnEventReceived += OnEventReceived;
                        _eventRegistered = true;
                    }
                }

                // 如果传输层存在但未初始化，尝试重新初始化（网络可能已就绪）
                if (_transport != null && !_transport.IsInitialized)
                {
                    _transport.Initialize();
                }

                return _transport!; // 不会为 null，但若为 null 则发送方法会处理
            }
        }

        // ---------- 事件接收处理 ----------
        private static void OnEventReceived(EventData photonEvent)
        {
            if (photonEvent.CustomData is not PhotonHashtable data) return;
            if (!RpcMessage.TryParse(data, out var op, out var cacheName, out var key, out var value))
                return;

            // 将操作转发给 SyncManager 的统一处理方法
            SyncManager.Instance.HandleRpcOperation(op, cacheName, key, value, photonEvent.Sender);
        }

        // ---------- 公共 API（与原来完全一致，但内部使用 EnsureTransport ）----------

        // 房主广播
        public static void BroadcastData<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning($"[SyncRpc] 传输层未就绪，广播数据丢弃。缓存：{cacheName}");
                return;
            }
            PhotonHashtable msg = RpcMessage.Build(RpcMessage.SubOpCode.ApplyData, cacheName, key, value);
            transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
        }

        public static void BroadcastDataBinary<TKey>(string cacheName, TKey key, byte[] data)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning($"[SyncRpc] 传输层未就绪，广播二进制数据丢弃。缓存：{cacheName}");
                return;
            }
            PhotonHashtable msg = RpcMessage.Build(RpcMessage.SubOpCode.ApplyDataBinary, cacheName, key, data);
            transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
        }

        public static void BroadcastRemove<TKey>(string cacheName, TKey key)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning($"[SyncRpc] 传输层未就绪，广播移除丢弃。缓存：{cacheName}");
                return;
            }
            PhotonHashtable msg = RpcMessage.Build(RpcMessage.SubOpCode.ApplyRemove, cacheName, key, null);
            transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
        }

        public static void BroadcastClear(string cacheName)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning($"[SyncRpc] 传输层未就绪，广播清空丢弃。缓存：{cacheName}");
                return;
            }
            PhotonHashtable msg = RpcMessage.Build(RpcMessage.SubOpCode.ApplyClear, cacheName, null, null);
            transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
        }

        public static void BroadcastFullSnapshot(string cacheName, PhotonHashtable snapshot)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning($"[SyncRpc] 传输层未就绪，广播全量快照丢弃。缓存：{cacheName}");
                return;
            }
            PhotonHashtable msg = RpcMessage.Build(RpcMessage.SubOpCode.ApplyFullSnapshot, cacheName, null, snapshot);
            transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
        }

        public static void BroadcastFullSnapshotBinary(string cacheName, Dictionary<object, byte[]> snapshot)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning($"[SyncRpc] 传输层未就绪，广播二进制快照丢弃。缓存：{cacheName}");
                return;
            }
            PhotonHashtable msg = RpcMessage.Build(RpcMessage.SubOpCode.ApplyFullSnapshotBinary, cacheName, null, snapshot);
            transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
        }

        // 客户端发送给房主（快照/合并请求）
        public static void SendSnapshot<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning($"[SyncRpc] 传输层未就绪，发送快照丢弃。缓存：{cacheName}");
                return;
            }
            PhotonHashtable msg = RpcMessage.Build(RpcMessage.SubOpCode.ReceiveSnapshot, cacheName, key, value);
            transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient });
        }

        public static void SendSnapshotBinary<TKey>(string cacheName, TKey key, byte[] data)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning($"[SyncRpc] 传输层未就绪，发送二进制快照丢弃。缓存：{cacheName}");
                return;
            }
            PhotonHashtable msg = RpcMessage.Build(RpcMessage.SubOpCode.ReceiveSnapshotBinary, cacheName, key, data);
            transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient });
        }

        public static void SendMergeRequest<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning($"[SyncRpc] 传输层未就绪，发送合并请求丢弃。缓存：{cacheName}");
                return;
            }
            PhotonHashtable msg = RpcMessage.Build(RpcMessage.SubOpCode.ReceiveMergeRequest, cacheName, key, value);
            transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient });
        }

        public static void SendMergeRequestBinary<TKey>(string cacheName, TKey key, byte[] data)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning($"[SyncRpc] 传输层未就绪，发送二进制合并请求丢弃。缓存：{cacheName}");
                return;
            }
            PhotonHashtable msg = RpcMessage.Build(RpcMessage.SubOpCode.ReceiveMergeRequestBinary, cacheName, key, data);
            transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient });
        }

        // 单播全量快照给指定玩家
        public static void SendFullSnapshotToPlayer(string cacheName, PhotonHashtable snapshot, int targetActorNumber)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning($"[SyncRpc] 传输层未就绪，单播快照丢弃。缓存：{cacheName}");
                return;
            }
            PhotonHashtable msg = RpcMessage.Build(RpcMessage.SubOpCode.ReceiveFullSnapshot, cacheName, null, snapshot);
            transport.Send(msg, new RaiseEventOptions { TargetActors = new int[] { targetActorNumber } });
        }

        public static void SendFullSnapshotBinaryToPlayer(string cacheName, Dictionary<object, byte[]> snapshot, int targetActorNumber)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning($"[SyncRpc] 传输层未就绪，单播二进制快照丢弃。缓存：{cacheName}");
                return;
            }
            PhotonHashtable msg = RpcMessage.Build(RpcMessage.SubOpCode.ReceiveFullSnapshotBinary, cacheName, null, snapshot);
            transport.Send(msg, new RaiseEventOptions { TargetActors = new int[] { targetActorNumber } });
        }

        // 自定义请求/响应
        public static void SendCustomRequest<T>(T data, ISerializationStrategy<T> strategy)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning("[SyncRpc] 传输层未就绪，自定义请求丢弃。");
                return;
            }
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            object serialized;
            RpcMessage.SubOpCode op;
            if (strategy is BinaryStrategy<T>)
            {
                serialized = strategy.SerializeToBinary(data);
                op = RpcMessage.SubOpCode.CustomRequestBinary;
            }
            else
            {
                serialized = strategy.SerializeToObject(data);
                op = RpcMessage.SubOpCode.CustomRequest;
            }
            PhotonHashtable msg = RpcMessage.Build(op, null, null, serialized);
            transport.Send(msg, options);
        }

        public static void SendCustomRequest(object data)
        {
            SendCustomRequest(data, _defaultStrategy);
        }

        public static void SendCustomResponse<T>(int targetActor, T data, ISerializationStrategy<T> strategy)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            IRpcTransport transport = EnsureTransport();
            if (!transport.IsInitialized)
            {
                Core.CommonPlugin.Logger.LogWarning("[SyncRpc] 传输层未就绪，自定义响应丢弃。");
                return;
            }
            RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { targetActor } };
            object serialized;
            RpcMessage.SubOpCode op;
            if (strategy is BinaryStrategy<T>)
            {
                serialized = strategy.SerializeToBinary(data);
                op = RpcMessage.SubOpCode.CustomResponseBinary;
            }
            else
            {
                serialized = strategy.SerializeToObject(data);
                op = RpcMessage.SubOpCode.CustomResponse;
            }
            PhotonHashtable msg = RpcMessage.Build(op, null, null, serialized);
            transport.Send(msg, options);
        }

        public static void SendCustomResponse(int targetActor, object data)
        {
            SendCustomResponse(targetActor, data, _defaultStrategy);
        }

        // 重置（由 SyncManager 在离开房间时调用）
        public static void Reset()
        {
            lock (_transportLock)
            {
                if (_transport != null)
                {
                    // 移除事件订阅（可选，但避免内存泄漏）
                    _transport.OnEventReceived -= OnEventReceived;
                    _transport.Reset();
                    _transport = null;
                }
                _eventRegistered = false;
            }
            TransportManager.Reset();
        }
    }
}