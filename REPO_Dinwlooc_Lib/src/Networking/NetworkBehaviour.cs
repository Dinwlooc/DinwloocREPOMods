using UnityEngine;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;

namespace Dinwlooc.Common.Networking
{
    /// <summary>
    /// 网络行为基类，订阅 NetworkReadyEvent 和 LeftRoomEvent，
    /// 提供 OnNetworkReady / OnLeftRoom 虚方法。
    /// 不直接接触 Photon API，所有网络就绪信号由 SyncManager 驱动。
    /// </summary>
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        private bool _networkReadyReceived = false;

        protected virtual void Awake()
        {
            EventBus.Subscribe<NetworkReadyEvent>(OnNetworkReadyEvent);
            EventBus.Subscribe<LeftRoomEvent>(OnLeftRoomEvent);
        }

        protected virtual void OnDestroy()
        {
            EventBus.Unsubscribe<NetworkReadyEvent>(OnNetworkReadyEvent);
            EventBus.Unsubscribe<LeftRoomEvent>(OnLeftRoomEvent);
        }

        private void OnNetworkReadyEvent(NetworkReadyEvent eventData)
        {
            if (_networkReadyReceived) return;
            _networkReadyReceived = true;
            OnNetworkReady();
        }

        private void OnLeftRoomEvent(LeftRoomEvent eventData)
        {
            _networkReadyReceived = false;
            OnLeftRoom();
        }

        /// <summary>
        /// 网络就绪时调用（由 SyncManager 触发）。
        /// </summary>
        protected virtual void OnNetworkReady() { }

        /// <summary>
        /// 离开房间时调用。
        /// </summary>
        protected virtual void OnLeftRoom() { }
    }
}