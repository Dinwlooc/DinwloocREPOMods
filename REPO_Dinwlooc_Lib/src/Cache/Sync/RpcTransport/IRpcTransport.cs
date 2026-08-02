using ExitGames.Client.Photon;
using Photon.Realtime;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using System;

namespace Dinwlooc.Common.Sync
{
    public interface IRpcTransport
    {
        bool IsInitialized { get; }
        void Initialize();
        void Send(PhotonHashtable message, RaiseEventOptions options);
        void Reset();
        event Action<EventData> OnEventReceived;  // 接收原始事件，由外层解析
    }
}