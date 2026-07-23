// 在 Dinwlooc.Common.Events 命名空间
namespace Dinwlooc.Common.Events
{
    public readonly struct PlayerDiedEvent
    {
        public readonly PlayerAvatar Player;
        public readonly int InstanceId;

        public PlayerDiedEvent(PlayerAvatar player)
        {
            Player = player;
            InstanceId = player ? player.GetInstanceID() : 0;
        }
    }
}