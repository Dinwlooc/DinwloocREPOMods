using UnityEngine;

namespace Dinwlooc.Common.IBridge;

public interface IHealthPackBridge
{
    /// <summary>查找最近的医疗包（仅遍历可用医疗包）</summary>
    ItemHealthPack? FindNearestHealthPack(Vector3 position, float radius);

    /// <summary>直接销毁整个医疗包（保留兼容，内部转为消耗所有剩余量）</summary>
    void ConsumeHealthPack(ItemAttributes healthPack);

    /// <summary>检查医疗包是否可用（未使用、未销毁、剩余治疗量 > 0）</summary>
    bool IsHealthPackUsable(ItemHealthPack healthPack);

    /// <summary>
    /// 从医疗包中消耗指定的治疗量（最大不超过剩余量），返回实际消耗的治疗量。
    /// 当剩余治疗量归零时，自动触发原版的 UsedRPC 逻辑（禁用交互、播放特效、网络同步）。
    /// 仅在主机/单机模式下有效。
    /// </summary>
    int UseHealthPack(ItemHealthPack healthPack, int maxAmount);
}