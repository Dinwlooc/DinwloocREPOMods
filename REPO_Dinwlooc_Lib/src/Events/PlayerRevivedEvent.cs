using UnityEngine;

namespace Dinwlooc.Common.Events;

public readonly struct PlayerRevivedEvent
{
    public readonly PlayerAvatar Player;

    public PlayerRevivedEvent(PlayerAvatar player)
    {
        Player = player;
    }
}