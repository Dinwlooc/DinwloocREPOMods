// 文件：Dinwlooc.Common/Events/SceneType.cs
namespace Dinwlooc.Common.Events
{
    /// <summary>
    /// 场景类型，完全对齐原版 RunManager 中的场景分类。
    /// </summary>
    public enum SceneType
    {
        MainMenu,   // 主菜单
        LobbyMenu,  // 大厅菜单（等待界面）
        Lobby,      // 关卡内大厅/卡车
        Shop,       // 商店
        Level,      // 关卡
        Tutorial,   // 教程
        Recording,  // 录像/回放
        Unknown     // 未知或过渡场景
    }
}