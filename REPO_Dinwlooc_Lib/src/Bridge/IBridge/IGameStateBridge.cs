namespace Dinwlooc.Common.IBridge;

public interface IGameStateBridge
{
    bool IsMasterClientOrSingleplayer();
    bool IsMainMenu();
    bool IsInTransit();
    bool IsLevelLoaded();
}