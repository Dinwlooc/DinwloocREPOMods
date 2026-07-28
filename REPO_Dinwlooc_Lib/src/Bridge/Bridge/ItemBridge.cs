// Dinwlooc.Common/Bridge/ItemBridge.cs
using System;
using System.Reflection;
using Dinwlooc.Common.IBridge;
using Dinwlooc.Common.Reflection;
using Photon.Pun;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    public class ItemBridge : IItemBridge, IHealthPackBridge
    {
        private static ItemBridge? _instance;
        public static ItemBridge Instance => _instance ??= new ItemBridge();
        private ItemBridge() { }

        // ---------- IItemBridge ----------
        public ItemBattery? GetHeldItemBattery(PlayerAvatar player)
        {
            if (player?.physGrabber == null) return null;
            Rigidbody? grabbed = player.physGrabber.grabbedObject;
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

        /// <summary>
        /// 获取玩家手持物品的 EnemyValuable 组件。
        /// grabbedObject 是 Rigidbody，直接获取其上的 EnemyValuable 组件。
        /// </summary>
        public EnemyValuable? GetHeldValuable(PlayerAvatar player)
        {
            if (player?.physGrabber == null) return null;
            Rigidbody? grabbed = player.physGrabber.grabbedObject;
            if (grabbed == null) return null;
            return grabbed.GetComponent<EnemyValuable>();
        }

        // ---------- IHealthPackBridge ----------
        public ItemHealthPack? FindNearestHealthPack(Vector3 position, float radius)
        {
            if (ItemManager.instance == null) return null;
            ItemHealthPack? nearest = null;
            float nearestDist = radius;
            foreach (ItemAttributes item in ItemManager.instance.spawnedItems)
            {
                if (item == null || item.itemType != SemiFunc.itemType.healthPack) continue;
                ItemHealthPack hp = item.GetComponent<ItemHealthPack>();
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
            FieldInfo usedField = ReflectionCache.ItemHealthPack_used;
            if (usedField != null)
            {
                try { if ((bool)usedField.GetValue(healthPack)) return false; }
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
                FieldInfo usedField = ReflectionCache.ItemHealthPack_used;
                if (usedField != null)
                {
                    try { usedField.SetValue(healthPack, true); }
                    catch { /* 忽略 */ }
                }

                // 触发原版 UsedRPC
                MethodInfo usedRPCMethod = ReflectionCache.ItemHealthPack_UsedRPC;
                if (SemiFunc.IsMultiplayer() && healthPack.photonView != null)
                {
                    healthPack.photonView.RPC("UsedRPC", RpcTarget.All);
                }
                else if (usedRPCMethod != null)
                {
                    try { usedRPCMethod.Invoke(healthPack, new object[] { default(PhotonMessageInfo) }); }
                    catch { /* 降级处理 */ }
                }

                // 禁用 ItemToggle（保险）
                FieldInfo itemToggleField = ReflectionCache.ItemHealthPack_itemToggle;
                if (itemToggleField != null)
                {
                    ItemToggle itemToggle = itemToggleField.GetValue(healthPack) as ItemToggle;
                    itemToggle?.ToggleDisable(true);
                }
            }

            return consume;
        }

        public void ConsumeHealthPack(ItemAttributes healthPack)
        {
            ItemHealthPack hp = healthPack?.GetComponent<ItemHealthPack>();
            if (hp == null) return;
            UseHealthPack(hp, hp.healAmount);
        }
    }
}