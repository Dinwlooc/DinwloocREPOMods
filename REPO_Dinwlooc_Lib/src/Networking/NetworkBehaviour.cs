using UnityEngine;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;

namespace Dinwlooc.Common.Networking
{
    /// <summary>
    /// 网络行为基类，订阅 SyncReadyEvent 和 LeftRoomEvent，
    /// 提供 OnSyncReady / OnLeftRoom 虚方法。
    /// 不直接接触 Photon API，所有网络就绪信号由 SyncManager 驱动。\
    /// 没有EnterRoomEvent这样的事件，因为没有找到不干预游戏原版网络初始化的实现方案。
    /// </summary>
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        private bool _networkReadyReceived = false;

        protected virtual void Awake()
        {
            EventBus.Subscribe<SyncReadyEvent>(OnSyncReadyEvent);
            EventBus.Subscribe<LeftRoomEvent>(OnLeftRoomEvent);
        }

        protected virtual void OnDestroy()
        {
            EventBus.Unsubscribe<SyncReadyEvent>(OnSyncReadyEvent);
            EventBus.Unsubscribe<LeftRoomEvent>(OnLeftRoomEvent);
        }

        private void OnSyncReadyEvent(SyncReadyEvent eventData)
        {
            if (_networkReadyReceived) return;
            _networkReadyReceived = true;
            OnSyncReady();
        }

        private void OnLeftRoomEvent(LeftRoomEvent eventData)
        {
            _networkReadyReceived = false;
            OnLeftRoom();
        }

        /// <summary>
        /// 网络就绪时调用（由 SyncManager 触发）。
        /// </summary>
        protected virtual void OnSyncReady() { }

        /// <summary>
        /// 离开房间时调用。
        /// </summary>
        protected virtual void OnLeftRoom() { }
    }
}