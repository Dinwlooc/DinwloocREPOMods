using UnityEngine;

namespace SuperEnergy
{
    public class ItemChargingHandler : IEnergyHandler
    {
        private readonly RepoGameBridge _bridge;
        private float _accumulator = 0f; // 累积未充电的百分比

        public ItemChargingHandler(RepoGameBridge bridge) => _bridge = bridge;

        public void Process(bool isHost, float deltaTime)
        {
            if (!SuperEnergy.EnableItemCharging?.Value ?? false) return;
            if (!isHost) return;

            int interval = SuperEnergy.ChargeInterval?.Value ?? 2;
            int amount = SuperEnergy.ChargeAmount?.Value ?? 5;
            if (interval <= 0 || amount <= 0) return;

            // 每秒充电速率（物品电量百分比）
            float ratePerSecond = amount / (float)interval;
            // 本帧应充入的物品电量
            float chargeThisFrame = ratePerSecond * deltaTime;
            _accumulator += chargeThisFrame;

            // 累积至少 1% 才执行充电
            if (_accumulator < 1f) return;

            int chargeToItem = Mathf.FloorToInt(_accumulator);
            _accumulator -= chargeToItem;

            ChargingSource source = SuperEnergy.ChargingSourceSetting?.Value ?? ChargingSource.Free;
            var players = GameDirector.instance.PlayerList;
            if (players == null) return;

            foreach (var player in players)
            {
                if (player == null || player.isDisabled) continue;
                var battery = _bridge.GetHeldItemBattery(player);
                if (battery == null) continue;

                // 满电跳过
                if (battery.batteryLife >= 99.9f) continue;

                // 计算充满一格所需的最小百分比
                float barPercent = 100f / battery.batteryBars;
                float current = battery.batteryLife;
                // 当前电量所在的格数（0-based）
                int currentBar = Mathf.FloorToInt(current / barPercent);
                // 充满下一格所需电量
                float needToNextBar = (currentBar + 1) * barPercent - current;
                if (needToNextBar < 0.01f) needToNextBar = barPercent; // 如果已经在格上，充一格

                // 本次计划充入的电量（取配置和剩余容量中的较小值）
                int maxCanCharge = Mathf.RoundToInt(100f - current);
                int chargePlan = Mathf.Min(chargeToItem, maxCanCharge);
                if (chargePlan <= 0) continue;

                // 确保至少充到下一格
                int chargeActual = Mathf.Max(chargePlan, Mathf.RoundToInt(needToNextBar));
                // 但也不能超过剩余容量
                chargeActual = Mathf.Min(chargeActual, maxCanCharge);

                if (source == ChargingSource.Truck)
                {
                    // 卡车消耗 = 充入物品电量的 10%（1:10 比例），向上取整保证至少消耗1%
                    int consumeTruck = Mathf.CeilToInt(chargeActual / 10f);
                    if (consumeTruck < 1) consumeTruck = 1;

                    float truckCharge = _bridge.GetTruckCharge();
                    if (truckCharge <= 0) continue;
                    int maxAvailable = Mathf.RoundToInt(truckCharge * 100f);
                    int consumeActual = Mathf.Min(consumeTruck, maxAvailable);
                    if (consumeActual <= 0) continue;

                    // 如果卡车余量不足，重新计算实际可充入物品电量（按比例）
                    int adjustedCharge = Mathf.Min(chargeActual, consumeActual * 10);
                    // 确保调整后仍能增加至少一格，否则跳过
                    if (adjustedCharge < needToNextBar) continue;

                    _bridge.ConsumeTruckCharge(consumeActual / 100f);
                    _bridge.SetItemBatteryCharge(battery, adjustedCharge);
                }
                else // Free
                {
                    _bridge.SetItemBatteryCharge(battery, chargeActual);
                }
            }
        }
    }
}