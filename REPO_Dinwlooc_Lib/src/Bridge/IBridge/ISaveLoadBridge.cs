namespace Dinwlooc.Common.src.Bridge.IBridge;

public interface ISaveLoadBridge
{
    string? GetCurrentSaveFileName();
    void LoadCurrentSave();
    void SaveCurrentProgress();
    void RestartScene();
    void ChangeToShop();
}