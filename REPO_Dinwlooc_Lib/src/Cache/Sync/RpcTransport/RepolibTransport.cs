using System;
using ExitGames.Client.Photon;
using Photon.Realtime;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace Dinwlooc.Common.Sync
{
    public class RepolibTransport : IRpcTransport
    {
        private object? _networkedEvent;
        private Action<object, RaiseEventOptions, SendOptions>? _raiseDelegate;
        public bool IsInitialized { get; private set; }
        public event Action<EventData>? OnEventReceived;

        public void Initialize()
        {
            if (IsInitialized) return;

            var type = Type.GetType("REPOLib.Modules.NetworkedEvent, REPOLib");
            if (type == null) return;

            var handler = new Action<EventData>(e => OnEventReceived?.Invoke(e));
            _networkedEvent = Activator.CreateInstance(type, "Dinwlooc_Sync", handler);
            if (_networkedEvent == null) return;

            var raiseMethod = type.GetMethod("RaiseEvent", new[] { typeof(object), typeof(RaiseEventOptions), typeof(SendOptions) });
            if (raiseMethod == null) return;

            _raiseDelegate = (Action<object, RaiseEventOptions, SendOptions>)Delegate.CreateDelegate(
                typeof(Action<object, RaiseEventOptions, SendOptions>), _networkedEvent, raiseMethod);

            IsInitialized = true;
        }

        public void Send(PhotonHashtable message, RaiseEventOptions options)
        {
            if (!IsInitialized) throw new InvalidOperationException("Transport not initialized.");
            _raiseDelegate?.Invoke(message, options, SendOptions.SendReliable);
        }

        public void Reset()
        {
            // 从 REPOLib 注销（反射移除）
            // ...（省略实现）
            IsInitialized = false;
            _networkedEvent = null;
            _raiseDelegate = null;
        }
    }
}