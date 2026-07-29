using UnityEngine;

namespace Dinwlooc.Common.Events
{
    /// <summary>
    /// 通用玩家加入事件，在玩家进入任何场景（包括关卡、商店、大厅等）时触发。
    /// 由 <see cref="PlayerJoinedEventGenerator"/> 在检测到新玩家 SteamID 时发布。
    /// </summary>
    public readonly struct PlayerJoinedEvent
    {
        public readonly PlayerAvatar Player;
        public readonly int InstanceId;

        public PlayerJoinedEvent(PlayerAvatar player)
        {
            Player = player;
            InstanceId = player ? player.GetInstanceID() : 0;
        }
    }
}