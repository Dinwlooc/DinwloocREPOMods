namespace Dinwlooc.Common.src.Bridge.IBridge;

public interface IGameStateBridge
{
    bool IsMasterClientOrSingleplayer();
    bool IsMainMenu();
    bool IsInTransit();
    bool IsLevelLoaded();
}