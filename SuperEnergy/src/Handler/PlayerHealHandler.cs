using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.src.Bridge.IBridge;
using UnityEngine;

namespace SuperEnergy
{
    public class PlayerHealHandler : IEnergyHandler
    {
        private readonly IPlayerBridge _playerBridge;
        private readonly IHealthPackBridge _healthPackBridge;
        private readonly List<ItemHealthPack> _healthPackPool = new();
        private float _lastHealTime = -1f;
        private float _lastPoolRefreshTime = -1f;
        private const float PoolRefreshInterval = 5f;  

        public PlayerHealHandler()
        {
            _playerBridge = BridgeLocator.Player;
            _healthPackBridge = BridgeLocator.HealthPack;
        }

        public void Process(bool isHost, float deltaTime)
        {
            var config = SuperEnergyConfig.Instance;
            if (!config.EnablePlayerHeal.Value || !isHost) return;

            int interval = config.HealInterval.Value;
            int amount = config.HealAmount.Value;
            if (interval <= 0 || amount <= 0) return;

            // 刷新医疗包池（定期）
            if (_lastPoolRefreshTime < 0f || Time.time - _lastPoolRefreshTime >= PoolRefreshInterval)
            {
                _lastPoolRefreshTime = Time.time;
                RefreshHealthPackPool();
            }

            // 治疗计时
            if (_lastHealTime < 0f)
                _lastHealTime = Time.time;
            if (Time.time - _lastHealTime < interval)
                return;
            _lastHealTime += interval;

            HealSource source = config.HealSourceSetting.Value;
            var players = _playerBridge.GetAllPlayers();
            if (players == null || players.Count == 0) return;

            foreach (var player in players)
            {
                if (player == null || player.isDisabled || player.playerHealth == null) continue;
                if (player.playerHealth.health >= player.playerHealth.maxHealth) continue;

                if (source == HealSource.HealthPack)
                {
                    var pack = FindAvailableHealthPack();
                    if (pack == null) continue;

                    int consumed = _healthPackBridge.UseHealthPack(pack, amount);
                    if (consumed > 0)
                    {
                        _playerBridge.HealPlayer(player, consumed, true);
                        // 若医疗包被完全消耗，桥接内部已触发 UsedRPC，无需额外操作
                    }
                }
                else // HealSource.Free
                {
                    _playerBridge.HealPlayer(player, amount, true);
                }
            }
        }

        private void RefreshHealthPackPool()
        {
            _healthPackPool.Clear();
            if (ItemManager.instance == null) return;
            foreach (var item in ItemManager.instance.spawnedItems)
            {
                if (item == null || item.itemType != SemiFunc.itemType.healthPack) continue;
                var hp = item.GetComponent<ItemHealthPack>();
                if (hp != null && _healthPackBridge.IsHealthPackUsable(hp))
                    _healthPackPool.Add(hp);
            }
        }

        private ItemHealthPack? FindAvailableHealthPack()
        {
            _healthPackPool.RemoveAll(p => p == null || !_healthPackBridge.IsHealthPackUsable(p));
            return _healthPackPool.Count > 0 ? _healthPackPool[0] : null;
        }
    }
}