namespace Dinwlooc.Common.src.Bridge.IBridge;

public interface ITruckBridge
{
    float GetTruckCharge();
    void ConsumeTruckCharge(float amount);
}