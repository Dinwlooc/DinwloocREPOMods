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
        private float _lastChargeTime = -1f;   // 上次充电时间戳

        public ItemChargingHandler()
        {
            _itemBridge = BridgeLocator.Item;
            _truckBridge = BridgeLocator.Truck;
            _playerBridge = BridgeLocator.Player;
        }

        public void Process(bool isHost, float deltaTime)
        {
            var config = SuperEnergyConfig.Instance;
            if (!config.EnableItemCharging.Value) return;
            if (!isHost) return;

            int interval = config.ChargeInterval.Value;
            int amount = config.ChargeAmount.Value;
            if (interval <= 0 || amount <= 0) return;

            // 每秒钟充电百分比
            float ratePerSecond = amount / (float)interval;

            // 初始化时间戳
            if (_lastChargeTime < 0f)
                _lastChargeTime = Time.time;

            // 计算自上次充电以来的总充电量（浮点数）
            float elapsed = Time.time - _lastChargeTime;
            float totalCharge = ratePerSecond * elapsed;

            // 取整部分（可应用的完整百分比）
            int appliedCharge = Mathf.FloorToInt(totalCharge);
            if (appliedCharge <= 0)
                return;

            // 更新时间戳（保留余数，确保下次继续累积）
            _lastChargeTime += appliedCharge / ratePerSecond;

            ChargingSource source = config.ChargingSourceSetting.Value;
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
                // 计划充电量（取appliedCharge与上限较小值）
                int chargePlan = Mathf.Min(appliedCharge, maxCanCharge);
                if (chargePlan <= 0) continue;

                // 强制至少充到下一格（但不超过上限）
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