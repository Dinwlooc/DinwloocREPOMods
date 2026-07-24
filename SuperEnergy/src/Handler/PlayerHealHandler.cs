using System.Collections.Generic;
using System.Reflection;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.src.Bridge.IBridge;
using Photon.Pun;
using UnityEngine;

namespace SuperEnergy
{
    public class PlayerHealHandler : IEnergyHandler
    {
        private readonly IPlayerBridge _playerBridge;
        private readonly List<ItemHealthPack> _healthPackPool = new();
        private float _lastHealTime = -1f;              // 上次治疗时间戳
        private float _lastPoolRefreshTime = -1f;        // 上次刷新池时间戳
        private const float PoolRefreshInterval = 5f;

        // 反射缓存
        private static readonly FieldInfo? _usedField =
            typeof(ItemHealthPack).GetField("used", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo? _itemToggleField =
            typeof(ItemHealthPack).GetField("itemToggle", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo? _usedRPCMethod =
            typeof(ItemHealthPack).GetMethod("UsedRPC", BindingFlags.NonPublic | BindingFlags.Instance);

        public PlayerHealHandler()
        {
            _playerBridge = BridgeLocator.Player;
        }

        public void Process(bool isHost, float deltaTime)
        {
            var config = SuperEnergyConfig.Instance;
            if (!config.EnablePlayerHeal.Value) return;
            if (!isHost) return;

            int interval = config.HealInterval.Value;
            int amount = config.HealAmount.Value;
            if (interval <= 0 || amount <= 0) return;

            // ---------- 刷新健康包池（基于时间戳差值） ----------
            if (_lastPoolRefreshTime < 0f)
                _lastPoolRefreshTime = Time.time;
            if (Time.time - _lastPoolRefreshTime >= PoolRefreshInterval)
            {
                _lastPoolRefreshTime += PoolRefreshInterval; // 保留余数
                RefreshHealthPackPool();
            }

            // ---------- 玩家治疗（基于时间戳差值） ----------
            if (_lastHealTime < 0f)
                _lastHealTime = Time.time;
            if (Time.time - _lastHealTime < interval)
                return;
            _lastHealTime += interval; // 保留余数

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
                    int heal = Mathf.Min(pack.healAmount, amount);
                    if (heal <= 0) continue;

                    pack.healAmount -= heal;
                    _playerBridge.HealPlayer(player, heal, true);

                    if (pack.healAmount <= 0)
                    {
                        MarkAsUsed(pack);
                        _healthPackPool.Remove(pack);
                    }
                }
                else
                {
                    _playerBridge.HealPlayer(player, amount, true);
                }
            }
        }

        /// <summary>
        /// 模拟原版 UsedRPC 行为：设置 used、销毁 ItemEquippable、播放特效、禁用交互
        /// </summary>
        private void MarkAsUsed(ItemHealthPack hp)
        {
            if (hp == null) return;

            var photonView = hp.GetComponent<PhotonView>();
            if (photonView != null && SemiFunc.IsMultiplayer())
            {
                photonView.RPC("UsedRPC", RpcTarget.All);
            }
            else if (_usedRPCMethod != null)
            {
                try { _usedRPCMethod.Invoke(hp, new object[] { default(PhotonMessageInfo) }); }
                catch { /* 降级处理 */ }
            }

            if (_itemToggleField != null)
            {
                var itemToggle = _itemToggleField.GetValue(hp) as ItemToggle;
                itemToggle?.ToggleDisable(true);
            }
        }

        private bool IsHealthPackAvailable(ItemHealthPack hp)
        {
            if (hp == null || hp.gameObject == null) return false;

            if (_usedField != null)
            {
                try { if ((bool)_usedField.GetValue(hp)) return false; }
                catch { /* 降级 */ }
            }

            if (hp.GetComponent<ItemEquippable>() == null) return false;
            return hp.healAmount > 0;
        }

        private void RefreshHealthPackPool()
        {
            _healthPackPool.Clear();
            if (ItemManager.instance == null) return;
            foreach (var item in ItemManager.instance.spawnedItems)
            {
                if (item == null || item.itemType != SemiFunc.itemType.healthPack) continue;
                var hp = item.GetComponent<ItemHealthPack>();
                if (hp != null && IsHealthPackAvailable(hp))
                    _healthPackPool.Add(hp);
            }
        }

        private ItemHealthPack? FindAvailableHealthPack()
        {
            _healthPackPool.RemoveAll(p => p == null || p.gameObject == null || !IsHealthPackAvailable(p));
            return _healthPackPool.Count > 0 ? _healthPackPool[0] : null;
        }
    }
}