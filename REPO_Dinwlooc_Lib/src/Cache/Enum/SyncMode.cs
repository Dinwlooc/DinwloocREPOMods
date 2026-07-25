using System;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 同步模式，决定数据的写入权限和同步策略
    /// </summary>
    public enum SyncMode
    {
        /// <summary>
        /// 房主权威模式：仅房主可写入，写入后自动广播给所有客户端。
        /// 客户端调用 Set 会被忽略。
        /// </summary>
        HostAuthority,

        /// <summary>
        /// 客户端快照模式：每个客户端可写入自己的数据（通常以玩家 SteamID 为键），
        /// 房主定期收集所有客户端的快照，合并后广播给所有人。
        /// </summary>
        ClientSnapshot,

        /// <summary>
        /// 合并模式：所有客户端均可写入，房主按自定义合并函数合并，然后广播。
        /// </summary>
        Merge
    }
}