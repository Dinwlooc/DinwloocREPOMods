using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 同步 RPC 通信模块，基于 RaiseEvent，无 PhotonView。
    /// 懒加载：首次发送事件时自动初始化监听器。
    /// </summary>
    public static class SyncRpcModule
    {
        private const byte EVENT_CODE_SYNC = 200;

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

        private static bool _isSubscribed = false;
        private static readonly object _subscribeLock = new object();

        private static void EnsureInitialized()
        {
            if (_isSubscribed) return;
            lock (_subscribeLock)
            {
                if (_isSubscribed) return;
                PhotonNetwork.AddCallbackTarget(new EventListener());
                _isSubscribed = true;
                Core.CommonPlugin.Logger.LogInfo("SyncRpcModule 已初始化（懒加载）");
            }
        }

        private class EventListener : IOnEventCallback
        {
            public void OnEvent(EventData photonEvent)
            {
                if (photonEvent.Code != EVENT_CODE_SYNC) return;
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
        }

        private static void SendEvent(SubOpCode op, string cacheName, object? key, object? value, RaiseEventOptions options)
        {
            EnsureInitialized();
            if (!PhotonNetwork.IsConnected) return;
            Hashtable data = new Hashtable();
            data["op"] = (byte)op;
            data["c"] = cacheName;
            if (key != null) data["k"] = key;
            if (value != null) data["v"] = value;
            PhotonNetwork.RaiseEvent(EVENT_CODE_SYNC, data, options, SendOptions.SendReliable);
        }

        // ----- 发送 API（完整）-----
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