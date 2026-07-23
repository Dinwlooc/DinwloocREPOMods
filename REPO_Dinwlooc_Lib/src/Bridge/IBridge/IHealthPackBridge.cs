using UnityEngine;

namespace Dinwlooc.Common.src.Bridge.IBridge;

public interface IHealthPackBridge
{
    ItemHealthPack? FindNearestHealthPack(Vector3 position, float radius);
    void ConsumeHealthPack(ItemAttributes healthPack);
}