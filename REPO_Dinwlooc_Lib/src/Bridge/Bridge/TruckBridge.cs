using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    public class TruckBridge : BridgeSingleton<TruckBridge>, ITruckBridge
    {
        private const float ChargeToPercent = 100f;

        private TruckBridge() { }

        public float GetTruckCharge()
        {
            if (ChargingStation.instance == null) return 0f;
            return ChargingStation.instance.chargeTotal / ChargeToPercent;
        }

        public void ConsumeTruckCharge(float amount)
        {
            if (ChargingStation.instance == null) return;
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer()) return;
            ChargingStation station = ChargingStation.instance;
            int total = station.chargeTotal;
            int consume = Mathf.RoundToInt(amount * ChargeToPercent);
            total = Mathf.Max(0, total - consume);
            station.chargeTotal = total;
            station.chargeFloat = total / ChargeToPercent;
            if (StatsManager.instance != null)
                StatsManager.instance.runStats["chargingStationChargeTotal"] = total;
        }
    }
}