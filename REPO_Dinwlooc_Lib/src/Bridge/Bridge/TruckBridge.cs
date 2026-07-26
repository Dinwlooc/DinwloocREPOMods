// Dinwlooc.Common/Bridge/TruckBridge.cs
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    public class TruckBridge : ITruckBridge
    {
        private static TruckBridge? _instance;
        public static TruckBridge Instance => _instance ??= new TruckBridge();
        private TruckBridge() { }

        public float GetTruckCharge()
        {
            if (ChargingStation.instance == null) return 0f;
            return ChargingStation.instance.chargeTotal / 100f;
        }

        public void ConsumeTruckCharge(float amount)
        {
            if (ChargingStation.instance == null) return;
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer()) return;
            var station = ChargingStation.instance;
            int total = station.chargeTotal;
            int consume = Mathf.RoundToInt(amount * 100f);
            total = Mathf.Max(0, total - consume);
            station.chargeTotal = total;
            station.chargeFloat = total / 100f;
            if (StatsManager.instance != null)
                StatsManager.instance.runStats["chargingStationChargeTotal"] = total;
        }
    }
}