using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

namespace SuperEnergy
{
    public class RepoGameBridge
    {
        private static RepoGameBridge? _instance;
        public static RepoGameBridge Instance => _instance ??= new RepoGameBridge();
        private RepoGameBridge() { }

        public bool IsMasterClientOrSingleplayer() => SemiFunc.IsMasterClientOrSingleplayer();

        // ---- 物品充电 ----
        public ItemBattery? GetHeldItemBattery(PlayerAvatar player)
        {
            if (player == null || player.physGrabber == null) return null;
            var grabbed = player.physGrabber.grabbedObject;
            if (grabbed == null) return null;
            return grabbed.GetComponent<ItemBattery>();
        }

        public void ChargeItemBattery(ItemBattery battery, int amountPercent)
        {
            if (battery == null) return;
            if (!IsMasterClientOrSingleplayer()) return;
            if (amountPercent <= 0) return;

            // 完全使用游戏原生充电逻辑，包括断电唤醒和UI同步
            battery.ChargeBattery(SuperEnergy.Instance.gameObject, amountPercent);
        }

        public float GetTruckCharge()
        {
            if (ChargingStation.instance == null) return 0f;
            return ChargingStation.instance.chargeTotal / 100f;
        }

        // ---- 玩家自愈 ----
        public PlayerAvatar? GetLocalPlayer() => PlayerController.instance?.playerAvatarScript;

        public void HealPlayer(PlayerAvatar player, int amount, bool effect = true)
        {
            if (player == null || player.playerHealth == null) return;
            if (!IsMasterClientOrSingleplayer()) return;
            if (amount <= 0) return;
            if (player.photonView.IsMine)
                player.playerHealth.Heal(amount, effect);
            else
                player.playerHealth.HealOther(amount, effect);
        }

        public List<ItemAttributes> FindNearbyHealthPacks(Vector3 position, float radius)
        {
            var list = new List<ItemAttributes>();
            if (ItemManager.instance == null) return list;
            foreach (var item in ItemManager.instance.spawnedItems)
            {
                if (item == null) continue;
                if (item.itemType != SemiFunc.itemType.healthPack) continue;
                if (Vector3.Distance(item.transform.position, position) > radius) continue;
                list.Add(item);
            }
            return list;
        }

        public void ConsumeHealthPack(ItemAttributes healthPack)
        {
            if (healthPack == null) return;
            if (!IsMasterClientOrSingleplayer()) return;
            Object.Destroy(healthPack.gameObject);
            if (ItemManager.instance != null)
                ItemManager.instance.spawnedItems.Remove(healthPack);
        }

        public void SetItemBatteryCharge(ItemBattery battery, int amountPercent)
        {
            if (battery == null) return;
            if (!IsMasterClientOrSingleplayer()) return;
            if (amountPercent <= 0) return;

            int current = Mathf.RoundToInt(battery.batteryLife);
            if (current <= 0)
            {
                // 完全没电，必须用 ChargeBattery 唤醒（脉冲充电，能正确激活 UI 和网络同步）
                battery.ChargeBattery(SuperEnergy.Instance.gameObject, amountPercent);
                return;
            }

            int newLife = Mathf.Min(100, current + amountPercent);
            if (newLife <= current) return;
            battery.SetBatteryLife(newLife);
        }

        public void ConsumeTruckCharge(float amount)
        {
            if (ChargingStation.instance == null) return;
            if (!IsMasterClientOrSingleplayer()) return;

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