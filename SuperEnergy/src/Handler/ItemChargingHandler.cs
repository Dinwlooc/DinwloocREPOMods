using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace SuperEnergy
{
    public class ItemChargingHandler : IEnergyHandler
    {
        private readonly IItemBridge _itemBridge;
        private readonly ITruckBridge _truckBridge;
        private readonly IPlayerBridge _playerBridge;
        private float _lastChargeTime = -1f;

        public ItemChargingHandler()
        {
            _itemBridge = BridgeLocator.Item;
            _truckBridge = BridgeLocator.Truck;
            _playerBridge = BridgeLocator.Player;
        }

        public void Process(bool isHost, float deltaTime)
        {
            var config = SuperEnergyConfig.Instance;
            if (!config.ItemChargingEnabled.Value) return;
            if (!isHost) return;

            int interval = config.ItemChargingInterval.Value;
            int amount = config.ItemChargingAmount.Value;
            if (interval <= 0 || amount <= 0) return;

            float ratePerSecond = amount / (float)interval;

            if (_lastChargeTime < 0f)
                _lastChargeTime = Time.time;

            float elapsed = Time.time - _lastChargeTime;
            float totalCharge = ratePerSecond * elapsed;

            int appliedCharge = Mathf.FloorToInt(totalCharge);
            if (appliedCharge <= 0)
                return;

            _lastChargeTime += appliedCharge / ratePerSecond;

            ChargingSource source = config.ItemChargingSource.Value;
            var players = _playerBridge.GetAllPlayers();
            if (players == null || players.Count == 0) return;

            foreach (var player in players)
            {
                if (player == null || player.isDisabled) continue;
                var battery = _itemBridge.GetHeldItemBattery(player);
                if (battery == null) continue;

                if (battery.batteryLife >= 99.9f) continue;

                float barPercent = 100f / battery.batteryBars;
                float current = battery.batteryLife;
                int currentBar = Mathf.FloorToInt(current / barPercent);
                float needToNextBar = (currentBar + 1) * barPercent - current;
                if (needToNextBar < 0.01f) needToNextBar = barPercent;

                int maxCanCharge = Mathf.RoundToInt(100f - current);
                int chargePlan = Mathf.Min(appliedCharge, maxCanCharge);
                if (chargePlan <= 0) continue;

                int chargeActual = Mathf.Max(chargePlan, Mathf.RoundToInt(needToNextBar));
                chargeActual = Mathf.Min(chargeActual, maxCanCharge);
                if (chargeActual <= 0) continue;

                if (source == ChargingSource.Truck)
                {
                    int consumeTruck = Mathf.CeilToInt(chargeActual / 10f);
                    if (consumeTruck < 1) consumeTruck = 1;

                    float truckCharge = _truckBridge.GetTruckCharge();
                    if (truckCharge <= 0) continue;
                    int maxAvailable = Mathf.RoundToInt(truckCharge * 100f);
                    int consumeActual = Mathf.Min(consumeTruck, maxAvailable);
                    if (consumeActual <= 0) continue;

                    int adjustedCharge = Mathf.Min(chargeActual, consumeActual * 10);
                    if (adjustedCharge < needToNextBar) continue;

                    _truckBridge.ConsumeTruckCharge(consumeActual / 100f);
                    _itemBridge.SetItemBatteryCharge(battery, adjustedCharge);
                }
                else
                {
                    _itemBridge.SetItemBatteryCharge(battery, chargeActual);
                }
            }
        }
    }
}