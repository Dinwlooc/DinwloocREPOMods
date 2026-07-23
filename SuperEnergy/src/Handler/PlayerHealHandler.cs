using UnityEngine;
using System.Collections.Generic;

namespace SuperEnergy
{
    public class PlayerHealHandler : IEnergyHandler
    {
        private readonly RepoGameBridge _bridge;
        private float _accumulator = 0f;
        private readonly List<ItemHealthPack> _healthPackPool = new();
        private float _poolRefreshAccumulator = 0f;
        private const float PoolRefreshInterval = 5f;

        public PlayerHealHandler(RepoGameBridge bridge) => _bridge = bridge;

        public void Process(bool isHost, float deltaTime)
        {
            if (!SuperEnergy.EnablePlayerHeal?.Value ?? false) return;
            if (!isHost) return;

            int interval = SuperEnergy.HealInterval?.Value ?? 2;
            int amount = SuperEnergy.HealAmount?.Value ?? 5;
            if (interval <= 0 || amount <= 0) return;

            _accumulator += deltaTime;
            if (_accumulator < interval) return;
            _accumulator = 0f;

            _poolRefreshAccumulator += deltaTime;
            if (_poolRefreshAccumulator >= PoolRefreshInterval)
            {
                _poolRefreshAccumulator = 0f;
                RefreshHealthPackPool();
            }

            HealSource source = SuperEnergy.HealSourceSetting?.Value ?? HealSource.Free;
            var players = GameDirector.instance.PlayerList;
            if (players == null) return;

            foreach (var player in players)
            {
                if (player == null || player.isDisabled || player.playerHealth == null) continue;
                if (player.playerHealth.health >= player.playerHealth.maxHealth) continue;

                if (source == HealSource.HealthPack)
                {
                    var pack = FindAvailableHealthPack();
                    if (pack == null) continue;
                    int heal = Mathf.Min(pack.healAmount, amount);
                    if (heal <= 0) continue;
                    pack.healAmount -= heal;
                    _bridge.HealPlayer(player, heal, true);
                    if (pack.healAmount <= 0)
                    {
                        var attr = pack.GetComponent<ItemAttributes>();
                        if (attr != null)
                            _bridge.ConsumeHealthPack(attr);
                        _healthPackPool.Remove(pack);
                    }
                }
                else
                {
                    _bridge.HealPlayer(player, amount, true);
                }
            }
        }

        private void RefreshHealthPackPool()
        {
            _healthPackPool.Clear();
            if (ItemManager.instance == null) return;
            foreach (var item in ItemManager.instance.spawnedItems)
            {
                if (item == null) continue;
                if (item.itemType != SemiFunc.itemType.healthPack) continue;
                var hp = item.GetComponent<ItemHealthPack>();
                if (hp != null && hp.healAmount > 0)
                    _healthPackPool.Add(hp);
            }
        }

        private ItemHealthPack? FindAvailableHealthPack()
        {
            _healthPackPool.RemoveAll(p => p == null || p.healAmount <= 0 || p.gameObject == null);
            return _healthPackPool.Count > 0 ? _healthPackPool[0] : null;
        }
    }
}