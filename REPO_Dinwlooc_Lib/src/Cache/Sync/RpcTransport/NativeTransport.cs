using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;


namespace Dinwlooc.Common.Sync
{
    public class NativeTransport : IRpcTransport, IOnEventCallback
    {
        public bool IsInitialized { get; private set; }
        public event Action<EventData>? OnEventReceived;

        private const int MIN_CODE = 100;
        private const int MAX_CODE = 199;
        private int _currentCode = MIN_CODE;
        private bool _conflictDetected;

        public void Initialize()
        {
            if (IsInitialized) return;
            if (!PhotonNetwork.IsConnectedAndReady) return;

            _currentCode = MIN_CODE;
            _conflictDetected = false;
            PhotonNetwork.AddCallbackTarget(this);
            IsInitialized = true;
        }

        public void Send(PhotonHashtable message, RaiseEventOptions options)
        {
            if (!IsInitialized) throw new InvalidOperationException("Native transport not initialized.");

            bool sent = false;
            int attempts = 0;
            while (!sent && attempts < (MAX_CODE - MIN_CODE + 1))
            {
                if (_conflictDetected)
                {
                    SwitchEventCode();
                    _conflictDetected = false;
                }

                try
                {
                    PhotonNetwork.RaiseEvent((byte)_currentCode, message, options, SendOptions.SendReliable);
                    sent = true;
                }
                catch
                {
                    SwitchEventCode();
                    attempts++;
                }
            }
            if (!sent)
                throw new Exception("Failed to send event after all code attempts.");
        }

        private void SwitchEventCode()
        {
            _currentCode = (_currentCode - MIN_CODE + 1) % (MAX_CODE - MIN_CODE + 1) + MIN_CODE;
            // 重新注册监听（实际可仅更新内部 code，由于是同一个实例，无需重复 AddCallback）
            // 但冲突检测发生后，原 code 已不可用，需标记切换
        }

        public void Reset()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            IsInitialized = false;
        }

        void IOnEventCallback.OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != _currentCode)
            {
                // 若收到其他事件码，可能冲突
                _conflictDetected = true;
                return;
            }
            OnEventReceived?.Invoke(photonEvent);
        }
    }
}