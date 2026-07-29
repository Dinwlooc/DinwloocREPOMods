// 文件：Dinwlooc.Common/Events/LeftRoomEvent.cs
namespace Dinwlooc.Common.Events
{
    /// <summary>
    /// 离开房间事件，在离开 Photon 房间或断开连接时发布（仅本地）。
    /// 由 SyncManager 在 OnLeftRoom / OnDisconnected 中触发。
    /// </summary>
    public readonly struct LeftRoomEvent
    {
        // 纯标记事件，无数据成员
    }
}