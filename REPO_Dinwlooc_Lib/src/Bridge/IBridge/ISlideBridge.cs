using UnityEngine;

namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// 滑铲系统桥接接口，提供对本地玩家滑铲状态的查询与控制。
    /// 仅本地玩家有效，远程玩家无法通过此接口控制。
    /// 修改操作（如 SetSlideDecay）需要主机/单机权限。
    /// </summary>
    public interface ISlideBridge
    {
        /// <summary>本地玩家是否正在滑铲。</summary>
        bool IsSliding();

        /// <summary>获取本地玩家剩余滑铲时间（秒）。</summary>
        float GetSlideTimerRemaining();

        /// <summary>获取本地玩家最大滑铲持续时间（秒）。</summary>
        float GetSlideDuration();

        /// <summary>获取本地玩家滑铲减速系数（每帧衰减速度）。</summary>
        float GetSlideDecay();

        /// <summary>设置本地玩家滑铲减速系数（需主机/单机权限）。</summary>
        void SetSlideDecay(float decay);
    }
}