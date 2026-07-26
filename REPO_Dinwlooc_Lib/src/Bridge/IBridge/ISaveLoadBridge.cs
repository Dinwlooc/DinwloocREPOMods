namespace Dinwlooc.Common.IBridge;

public interface ISaveLoadBridge
{
    string? GetCurrentSaveFileName();
    void LoadCurrentSave();
    void SaveCurrentProgress();
    void RestartScene();
    void ChangeToShop();
}