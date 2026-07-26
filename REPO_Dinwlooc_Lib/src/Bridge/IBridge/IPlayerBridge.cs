using System.Collections.Generic;
using UnityEngine;

namespace Dinwlooc.Common.IBridge;

public interface IPlayerBridge
{
    PlayerAvatar? GetLocalPlayer();
    List<PlayerAvatar> GetAllPlayers();
    void HealPlayer(PlayerAvatar player, int amount, bool effect = true);
    int GetPlayerHP(string steamID);
    void SetPlayerHP(string steamID, int newHP);
    T? GetComponentOnPlayer<T>(PlayerAvatar player) where T : Component;
}