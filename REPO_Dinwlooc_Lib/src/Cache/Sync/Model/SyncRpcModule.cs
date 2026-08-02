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

        // 获取传输层（单例，懒初始化）
        private static IRpcTransport Transport => TransportManager.GetOrCreateTransport();

        // 静态构造：注册事件接收
        static SyncRpcModule()
        {
            Transport.OnEventReceived += OnEventReceived;
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

        // ---------- 公共 API（与原来完全一致） ----------

        // 房主广播
        public static void BroadcastData<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            var msg = RpcMessage.Build(RpcMessage.SubOpCode.ApplyData, cacheName, key, value);
            Transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
        }

        public static void BroadcastDataBinary<TKey>(string cacheName, TKey key, byte[] data)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            var msg = RpcMessage.Build(RpcMessage.SubOpCode.ApplyDataBinary, cacheName, key, data);
            Transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
        }

        public static void BroadcastRemove<TKey>(string cacheName, TKey key)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            var msg = RpcMessage.Build(RpcMessage.SubOpCode.ApplyRemove, cacheName, key, null);
            Transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
        }

        public static void BroadcastClear(string cacheName)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            var msg = RpcMessage.Build(RpcMessage.SubOpCode.ApplyClear, cacheName, null, null);
            Transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
        }

        public static void BroadcastFullSnapshot(string cacheName, PhotonHashtable snapshot)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            var msg = RpcMessage.Build(RpcMessage.SubOpCode.ApplyFullSnapshot, cacheName, null, snapshot);
            Transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
        }

        public static void BroadcastFullSnapshotBinary(string cacheName, Dictionary<object, byte[]> snapshot)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            var msg = RpcMessage.Build(RpcMessage.SubOpCode.ApplyFullSnapshotBinary, cacheName, null, snapshot);
            Transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
        }

        // 客户端发送给房主（快照/合并请求）
        public static void SendSnapshot<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
            var msg = RpcMessage.Build(RpcMessage.SubOpCode.ReceiveSnapshot, cacheName, key, value);
            Transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient });
        }

        public static void SendSnapshotBinary<TKey>(string cacheName, TKey key, byte[] data)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
            var msg = RpcMessage.Build(RpcMessage.SubOpCode.ReceiveSnapshotBinary, cacheName, key, data);
            Transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient });
        }

        public static void SendMergeRequest<TKey, TValue>(string cacheName, TKey key, TValue value)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
            var msg = RpcMessage.Build(RpcMessage.SubOpCode.ReceiveMergeRequest, cacheName, key, value);
            Transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient });
        }

        public static void SendMergeRequestBinary<TKey>(string cacheName, TKey key, byte[] data)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
            var msg = RpcMessage.Build(RpcMessage.SubOpCode.ReceiveMergeRequestBinary, cacheName, key, data);
            Transport.Send(msg, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient });
        }

        // 单播全量快照给指定玩家
        public static void SendFullSnapshotToPlayer(string cacheName, PhotonHashtable snapshot, int targetActorNumber)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return;
            var msg = RpcMessage.Build(RpcMessage.SubOpCode.ReceiveFullSnapshot, cacheName, null, snapshot);
            Transport.Send(msg, new RaiseEventOptions { TargetActors = new int[] { targetActorNumber } });
        }

        public static void SendFullSnapshotBinaryToPlayer(string cacheName, Dictionary<object, byte[]> snapshot, int targetActorNumber)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return;
            var msg = RpcMessage.Build(RpcMessage.SubOpCode.ReceiveFullSnapshotBinary, cacheName, null, snapshot);
            Transport.Send(msg, new RaiseEventOptions { TargetActors = new int[] { targetActorNumber } });
        }

        // 自定义请求/响应
        public static void SendCustomRequest<T>(T data, ISerializationStrategy<T> strategy)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
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
            var msg = RpcMessage.Build(op, null, null, serialized);
            Transport.Send(msg, options);
        }

        public static void SendCustomRequest(object data)
        {
            SendCustomRequest(data, _defaultStrategy);
        }

        public static void SendCustomResponse<T>(int targetActor, T data, ISerializationStrategy<T> strategy)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
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
            var msg = RpcMessage.Build(op, null, null, serialized);
            Transport.Send(msg, options);
        }

        public static void SendCustomResponse(int targetActor, object data)
        {
            SendCustomResponse(targetActor, data, _defaultStrategy);
        }

        // 重置（由 SyncManager 在离开房间时调用）
        public static void Reset()
        {
            TransportManager.Reset();
        }
    }
}