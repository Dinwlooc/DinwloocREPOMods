namespace Dinwlooc.Common.IBridge;

public interface ITruckBridge
{
    float GetTruckCharge();
    void ConsumeTruckCharge(float amount);
}