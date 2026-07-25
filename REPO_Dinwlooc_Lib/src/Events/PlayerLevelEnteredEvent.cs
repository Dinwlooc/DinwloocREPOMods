// 文件：Dinwlooc.Common/Events/PlayerJoinedEvent.cs
using UnityEngine;

namespace Dinwlooc.Common.Events
{
    public readonly struct PlayerLevelEnteredEvent
    {
        public readonly PlayerAvatar Player;
        public readonly int InstanceId;

        public PlayerLevelEnteredEvent(PlayerAvatar player)
        {
            Player = player;
            InstanceId = player ? player.GetInstanceID() : 0;
        }
    }
}