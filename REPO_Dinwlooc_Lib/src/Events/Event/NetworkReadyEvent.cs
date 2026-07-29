// 文件：Dinwlooc.Common/Events/NetworkReadyEvent.cs
namespace Dinwlooc.Common.Events
{
    /// <summary>
    /// 网络就绪事件，在成功加入 Photon 房间后发布（仅本地）。
    /// 由 SyncManager 在 OnJoinedRoom 中触发。
    /// </summary>
    public readonly struct NetworkReadyEvent
    {
        // 纯标记事件，无数据成员
    }
}