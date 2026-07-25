// Dinwlooc.Common/Bridge/ItemBridge.cs
using System;
using System.Reflection;
using Dinwlooc.Common.src.Bridge.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    public class ItemBridge : IItemBridge, IHealthPackBridge
    {
        private static ItemBridge? _instance;
        public static ItemBridge Instance => _instance ??= new ItemBridge();
        private ItemBridge() { }

        // 医疗包反射缓存
        private static MethodInfo? _usedRPCMethod;
        private static FieldInfo? _usedField;
        private static FieldInfo? _itemToggleField;

        static ItemBridge()
        {
            var hpType = typeof(ItemHealthPack);
            _usedRPCMethod = hpType.GetMethod("UsedRPC", BindingFlags.NonPublic | BindingFlags.Instance);
            _usedField = hpType.GetField("used", BindingFlags.NonPublic | BindingFlags.Instance);
            _itemToggleField = hpType.GetField("itemToggle", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        // ---------- IItemBridge ----------
        public ItemBattery? GetHeldItemBattery(PlayerAvatar player)
        {
            if (player?.physGrabber == null) return null;
            var grabbed = player.physGrabber.grabbedObject;
            if (grabbed == null) return null;
            return grabbed.GetComponent<ItemBattery>();
        }

        public void ChargeItemBattery(ItemBattery battery, int amountPercent)
        {
            if (battery == null || amountPercent <= 0) return;
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer()) return;
            battery.ChargeBattery(Core.CommonService.Instance.gameObject, amountPercent);
        }

        public void SetItemBatteryCharge(ItemBattery battery, int amountPercent)
        {
            if (battery == null || amountPercent <= 0) return;
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer()) return;
            int current = Mathf.RoundToInt(battery.batteryLife);
            if (current <= 0)
            {
                ChargeItemBattery(battery, amountPercent);
                return;
            }
            int newLife = Mathf.Min(100, current + amountPercent);
            if (newLife <= current) return;
            battery.SetBatteryLife(newLife);
        }

        // ---------- IHealthPackBridge ----------
        public ItemHealthPack? FindNearestHealthPack(Vector3 position, float radius)
        {
            if (ItemManager.instance == null) return null;
            ItemHealthPack? nearest = null;
            float nearestDist = radius;
            foreach (var item in ItemManager.instance.spawnedItems)
            {
                if (item == null || item.itemType != SemiFunc.itemType.healthPack) continue;
                var hp = item.GetComponent<ItemHealthPack>();
                if (hp == null || !IsHealthPackUsable(hp)) continue;
                float dist = Vector3.Distance(item.transform.position, position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = hp;
                }
            }
            return nearest;
        }

        public bool IsHealthPackUsable(ItemHealthPack healthPack)
        {
            if (healthPack == null) return false;
            if (healthPack.healAmount <= 0) return false;
            if (_usedField != null)
            {
                try { if ((bool)_usedField.GetValue(healthPack)) return false; }
                catch { /* 忽略 */ }
            }
            return true;
        }

        public int UseHealthPack(ItemHealthPack healthPack, int maxAmount)
        {
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer()) return 0;
            if (!IsHealthPackUsable(healthPack)) return 0;

            int consume = Mathf.Min(maxAmount, healthPack.healAmount);
            if (consume <= 0) return 0;

            healthPack.healAmount -= consume;

            if (healthPack.healAmount <= 0)
            {
                healthPack.healAmount = 0;
                if (_usedField != null)
                {
                    try { _usedField.SetValue(healthPack, true); }
                    catch { /* 忽略 */ }
                }

                // 触发原版 UsedRPC
                if (SemiFunc.IsMultiplayer() && healthPack.photonView != null)
                {
                    healthPack.photonView.RPC("UsedRPC", Photon.Pun.RpcTarget.All);
                }
                else if (_usedRPCMethod != null)
                {
                    try { _usedRPCMethod.Invoke(healthPack, new object[] { default(Photon.Pun.PhotonMessageInfo) }); }
                    catch { /* 降级处理 */ }
                }

                // 禁用 ItemToggle（保险）
                if (_itemToggleField != null)
                {
                    var itemToggle = _itemToggleField.GetValue(healthPack) as ItemToggle;
                    itemToggle?.ToggleDisable(true);
                }
            }

            return consume;
        }

        public void ConsumeHealthPack(ItemAttributes healthPack)
        {
            var hp = healthPack?.GetComponent<ItemHealthPack>();
            if (hp == null) return;
            UseHealthPack(hp, hp.healAmount);
        }
    }
}