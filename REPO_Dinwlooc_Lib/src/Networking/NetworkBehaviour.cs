using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;   // 添加此引用以使用 NetworkReadyEvent 和 LeftRoomEvent

namespace Dinwlooc.Common.Networking
{
    /// <summary>
    /// 网络行为基类，自动订阅 NetworkReadyEvent 和 LeftRoomEvent，
    /// 在网络就绪时注册 Photon 回调，并提供 OnNetworkReady / OnLeftRoom 虚方法。
    /// 不会在 Awake 中接触 Photon API，所有网络操作延迟到事件触发。
    /// </summary>
    public abstract class NetworkBehaviour : MonoBehaviour, IOnEventCallback
    {
        private bool _subscribedToPhoton;

        protected virtual void Awake()
        {
            EventBus.Subscribe<NetworkReadyEvent>(OnNetworkReadyEvent);
            EventBus.Subscribe<LeftRoomEvent>(OnLeftRoomEvent);
        }

        protected virtual void OnDestroy()
        {
            EventBus.Unsubscribe<NetworkReadyEvent>(OnNetworkReadyEvent);
            EventBus.Unsubscribe<LeftRoomEvent>(OnLeftRoomEvent);
            UnsubscribeFromPhoton();
        }

        private void OnNetworkReadyEvent(NetworkReadyEvent eventData)
        {
            SubscribeToPhoton();
            OnNetworkReady();
        }

        private void OnLeftRoomEvent(LeftRoomEvent eventData)
        {
            UnsubscribeFromPhoton();
            OnLeftRoom();
        }

        private void SubscribeToPhoton()
        {
            if (_subscribedToPhoton) return;
            if (!PhotonNetwork.IsConnected) return;

            PhotonNetwork.AddCallbackTarget(this);
            _subscribedToPhoton = true;
            CommonPlugin.Logger.LogInfo($"[{GetType().Name}] 已注册 Photon 回调。");
        }

        private void UnsubscribeFromPhoton()
        {
            if (!_subscribedToPhoton) return;
            PhotonNetwork.RemoveCallbackTarget(this);
            _subscribedToPhoton = false;
            CommonPlugin.Logger.LogInfo($"[{GetType().Name}] 已注销 Photon 回调。");
        }

        /// <summary>网络就绪且已加入房间时调用（仅一次，每次进房触发）。</summary>
        protected virtual void OnNetworkReady() { }

        /// <summary>离开房间时调用。</summary>
        protected virtual void OnLeftRoom() { }

        /// <summary>子类重写以处理自定义网络事件。</summary>
        public virtual void OnEvent(EventData photonEvent) { }
    }
}