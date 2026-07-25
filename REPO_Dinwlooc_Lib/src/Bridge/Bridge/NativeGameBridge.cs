using System;
using System.Collections.Generic;
using System.Reflection;
using Dinwlooc.Common.src.Bridge.IBridge;
using Photon.Pun;
using UnityEngine;

namespace Dinwlooc.Common.Bridge;

public class NativeGameBridge :
    IPlayerBridge,
    IItemBridge,
    IHealthPackBridge,
    ITruckBridge,
    ISaveLoadBridge,
    IGameStateBridge,
    INetworkBridge,
    IUpgradeBridge,
    IEnemyBridge
{
    private static NativeGameBridge? _instance;
    public static NativeGameBridge Instance => _instance ??= new NativeGameBridge();

    protected NativeGameBridge() { }

    // ========== 缓存字段 ==========
    protected Dictionary<string, Item>? _upgradeItemCache;
    protected readonly object _cacheLock = new();
    private static FieldInfo? _rigidField;

    // ---- 医疗包反射缓存 ----
    private static MethodInfo? _usedRPCMethod;
    private static FieldInfo? _usedField;
    private static FieldInfo? _itemToggleField;

    static NativeGameBridge()
    {
        var hpType = typeof(ItemHealthPack);
        _usedRPCMethod = hpType.GetMethod("UsedRPC", BindingFlags.NonPublic | BindingFlags.Instance);
        _usedField = hpType.GetField("used", BindingFlags.NonPublic | BindingFlags.Instance);
        _itemToggleField = hpType.GetField("itemToggle", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    // ========== IGameStateBridge ==========
    public virtual bool IsMasterClientOrSingleplayer() => SemiFunc.IsMasterClientOrSingleplayer();
    public virtual bool IsMainMenu() => SemiFunc.IsMainMenu();
    public virtual bool IsInTransit() => !SemiFunc.RunIsLevel() && !SemiFunc.RunIsShop() && !IsMainMenu();
    public virtual bool IsLevelLoaded() => LevelGenerator.Instance != null && LevelGenerator.Instance.Generated;

    // ========== IPlayerBridge ==========
    public virtual PlayerAvatar? GetLocalPlayer() => PlayerController.instance?.playerAvatarScript;
    public virtual List<PlayerAvatar> GetAllPlayers()
    {
        var list = new List<PlayerAvatar>();
        if (GameDirector.instance == null) return list;
        foreach (var p in GameDirector.instance.PlayerList)
        {
            if (p != null && !p.isDisabled)
                list.Add(p);
        }
        return list;
    }
    public virtual void HealPlayer(PlayerAvatar player, int amount, bool effect = true)
    {
        if (player == null || player.playerHealth == null || amount <= 0) return;
        if (!IsMasterClientOrSingleplayer()) return;
        if (player.photonView.IsMine)
            player.playerHealth.Heal(amount, effect);
        else
            player.playerHealth.HealOther(amount, effect);
    }
    public virtual int GetPlayerHP(string steamID)
    {
        if (StatsManager.instance == null) return 100;
        return StatsManager.instance.playerHealth.TryGetValue(steamID, out int hp) ? hp : 100;
    }
    public virtual void SetPlayerHP(string steamID, int newHP)
    {
        if (StatsManager.instance != null)
            StatsManager.instance.playerHealth[steamID] = newHP;
    }
    public virtual T? GetComponentOnPlayer<T>(PlayerAvatar player) where T : Component
    {
        if (player == null) return null;
        return player.GetComponent<T>();
    }

    // ========== IItemBridge ==========
    public virtual ItemBattery? GetHeldItemBattery(PlayerAvatar player)
    {
        if (player?.physGrabber == null) return null;
        var grabbed = player.physGrabber.grabbedObject;
        if (grabbed == null) return null;
        return grabbed.GetComponent<ItemBattery>();
    }
    public virtual void ChargeItemBattery(ItemBattery battery, int amountPercent)
    {
        if (battery == null || amountPercent <= 0 || !IsMasterClientOrSingleplayer()) return;
        battery.ChargeBattery(Core.CommonService.Instance.gameObject, amountPercent);
    }
    public virtual void SetItemBatteryCharge(ItemBattery battery, int amountPercent)
    {
        if (battery == null || amountPercent <= 0 || !IsMasterClientOrSingleplayer()) return;
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

    // ========== IHealthPackBridge ==========
    public virtual ItemHealthPack? FindNearestHealthPack(Vector3 position, float radius)
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

    public virtual bool IsHealthPackUsable(ItemHealthPack healthPack)
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

    public virtual int UseHealthPack(ItemHealthPack healthPack, int maxAmount)
    {
        if (!IsMasterClientOrSingleplayer()) return 0;
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
                healthPack.photonView.RPC("UsedRPC", RpcTarget.All);
            }
            else if (_usedRPCMethod != null)
            {
                try { _usedRPCMethod.Invoke(healthPack, new object[] { default(PhotonMessageInfo) }); }
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

    // 注意：此方法已不再强制销毁对象，仅消耗所有剩余治疗量，原版 UsedRPC 会处理禁用交互等。
    public virtual void ConsumeHealthPack(ItemAttributes healthPack)
    {
        var hp = healthPack?.GetComponent<ItemHealthPack>();
        if (hp == null) return;
        UseHealthPack(hp, hp.healAmount); // 消耗全部剩余量，若归零则触发原版逻辑
    }

    // ========== ITruckBridge ==========
    public virtual float GetTruckCharge()
    {
        if (ChargingStation.instance == null) return 0f;
        return ChargingStation.instance.chargeTotal / 100f;
    }
    public virtual void ConsumeTruckCharge(float amount)
    {
        if (ChargingStation.instance == null || !IsMasterClientOrSingleplayer()) return;
        var station = ChargingStation.instance;
        int total = station.chargeTotal;
        int consume = Mathf.RoundToInt(amount * 100f);
        total = Mathf.Max(0, total - consume);
        station.chargeTotal = total;
        station.chargeFloat = total / 100f;
        if (StatsManager.instance != null)
            StatsManager.instance.runStats["chargingStationChargeTotal"] = total;
    }

    // ========== ISaveLoadBridge ==========
    public virtual string? GetCurrentSaveFileName()
    {
        try
        {
            var stats = StatsManager.instance;
            if (stats == null) return null;
            var field = stats.GetType().GetField("saveFileCurrent", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(stats) as string;
        }
        catch { return null; }
    }
    public virtual void LoadCurrentSave()
    {
        string? fileName = GetCurrentSaveFileName();
        if (!string.IsNullOrEmpty(fileName))
            SemiFunc.SaveFileLoad(fileName, null);
    }
    public virtual void SaveCurrentProgress() => SemiFunc.SaveFileSave();
    public virtual void RestartScene()
    {
        if (RunManager.instance != null && GameDirector.instance != null)
        {
            RunManager.instance.RestartScene();
            return;
        }
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (SemiFunc.IsMultiplayer())
            PhotonNetwork.LoadLevel(sceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
    public virtual void ChangeToShop()
    {
        if (RunManager.instance == null) return;
        if (!IsMasterClientOrSingleplayer()) return;
        LoadCurrentSave();
        SyncDictionariesToClients();
        RunManager.instance.ChangeLevel(false, false, RunManager.ChangeLevelType.Shop);
    }

    // ========== INetworkBridge ==========
    public virtual void SyncDictionariesToClients()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            if (PunManager.instance != null)
                PunManager.instance.SyncAllDictionaries();
        }
    }

    // ========== IUpgradeBridge ==========
    public virtual void RefreshUpgradeItemCache()
    {
        lock (_cacheLock)
        {
            _upgradeItemCache = new Dictionary<string, Item>();
            if (StatsManager.instance == null) return;

            foreach (var entry in StatsManager.instance.itemDictionary)
            {
                var item = entry.Value;
                if (item.itemType != SemiFunc.itemType.item_upgrade) continue;
                var prefab = item.prefab?.Prefab;
                if (prefab == null) continue;

                foreach (var kvp in UpgradeDefinitions.KeyToComponentType)
                {
                    if (_upgradeItemCache.ContainsKey(kvp.Key)) continue;
                    var type = typeof(PlayerAvatar).Assembly.GetType(kvp.Value);
                    if (type != null && prefab.GetComponent(type) != null)
                    {
                        _upgradeItemCache[kvp.Key] = item;
                        break;
                    }
                }
            }
        }
    }
    public virtual Dictionary<string, int> FetchUpgrades(string steamID)
    {
        var raw = StatsManager.instance?.FetchPlayerUpgrades(steamID);
        return raw != null ? new Dictionary<string, int>(raw) : new Dictionary<string, int>();
    }
    public virtual Item? FindItemByUpgradeKey(string upgradeKey)
    {
        if (_upgradeItemCache == null) RefreshUpgradeItemCache();
        return _upgradeItemCache?.TryGetValue(upgradeKey, out var item) == true ? item : null;
    }
    public virtual void ClearUpgradeStat(string steamID, string upgradeKey)
    {
        if (PunManager.instance != null)
            PunManager.instance.UpdateStat(upgradeKey, steamID, 0);
    }
    public virtual void AddPurchasedItem(string itemName, int count)
    {
        if (count <= 0 || StatsManager.instance == null) return;
        int current = StatsManager.instance.itemsPurchased.TryGetValue(itemName, out int val) ? val : 0;
        StatsManager.instance.itemsPurchased[itemName] = current + count;
    }

    // ========== IEnemyBridge ==========
    public virtual IReadOnlyList<EnemyParent> GetAllEnemies()
    {
        var director = EnemyDirector.instance;
        if (director == null) return Array.Empty<EnemyParent>();
        var list = director.enemiesSpawned;
        if (list == null) return Array.Empty<EnemyParent>();
        return list;
    }

    public virtual bool IsEnemyValid(EnemyParent enemy)
    {
        if (enemy == null) return false;
        if (!enemy.Spawned) return false;
        if (enemy.Enemy == null) return false;
        if (enemy.Enemy.Health == null) return false;
        if (enemy.Enemy.Health.health <= 0) return false;
        if (!enemy.Enemy.gameObject.activeInHierarchy) return false;
        return true;
    }

    public virtual Vector3 GetEnemyPosition(EnemyParent enemy)
    {
        if (enemy?.Enemy == null) return Vector3.zero;
        var enemyComp = enemy.Enemy;
        if (enemyComp.CenterTransform != null)
            return enemyComp.CenterTransform.position;
        if (enemyComp.transform != null)
            return enemyComp.transform.position;
        return Vector3.zero;
    }

    public virtual int GetEnemyInstanceId(EnemyParent enemy)
    {
        return enemy?.Enemy?.GetInstanceID() ?? 0;
    }

    public virtual void ApplyHighlight(EnemyParent enemy, bool active, Color color)
    {
        if (enemy == null || enemy.EnableObject == null) return;

        var enableObj = enemy.EnableObject;
        Transform modelTransform = enableObj.transform.Find("[VISUALS]");
        if (modelTransform == null)
            modelTransform = enableObj.transform.Find("Visual");
        if (modelTransform == null)
            modelTransform = enableObj.transform.Find("Model");

        GameObject modelTarget = modelTransform != null ? modelTransform.gameObject : enableObj;
        var renderers = modelTarget.GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
        {
            if (rend == null) continue;
            if (rend.GetComponent<ParticleSystem>() != null) continue;
            var mat = rend.material;
            if (!mat.HasProperty("_EmissionColor")) continue;

            if (active)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2f);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    public virtual float GetIndicatorHeightOffset(EnemyParent enemy)
    {
        if (enemy?.Enemy == null) return 0.5f;
        Enemy enemyComp = enemy.Enemy;

        if (_rigidField == null)
            _rigidField = typeof(Enemy).GetField("Rigidbody", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        try
        {
            EnemyRigidbody rigid = _rigidField?.GetValue(enemyComp) as EnemyRigidbody;
            if (rigid != null)
            {
                Collider[] colliders = rigid.GetComponentsInChildren<Collider>();
                Vector3 center = GetEnemyPosition(enemy);
                Bounds bounds = new Bounds(center, Vector3.zero);
                bool hasBounds = false;
                foreach (var col in colliders)
                {
                    if (col == null || col.isTrigger) continue;
                    if (!hasBounds)
                    {
                        bounds = col.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(col.bounds);
                    }
                }
                if (hasBounds)
                {
                    float height = bounds.max.y - center.y + 0.15f;
                    return Mathf.Clamp(height, 0.3f, 5f);
                }
            }
        }
        catch { }

        try
        {
            Transform model = enemyComp.CenterTransform;
            if (model != null)
            {
                Renderer renderer = model.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    float height = renderer.bounds.size.y * 0.8f;
                    return Mathf.Clamp(height, 0.3f, 3f);
                }
                float scaleY = model.lossyScale.y;
                if (scaleY > 0.5f)
                    return Mathf.Clamp(scaleY * 0.8f, 0.3f, 3f);
            }
        }
        catch { }

        return 0.5f;
    }

    // ========== 升级定义 ==========
    protected static class UpgradeDefinitions
    {
        public static readonly Dictionary<string, string> KeyToComponentType = new()
        {
            { "playerUpgradeHealth", "ItemUpgradePlayerHealth" },
            { "playerUpgradeStamina", "ItemUpgradePlayerEnergy" },
            { "playerUpgradeExtraJump", "ItemUpgradePlayerExtraJump" },
            { "playerUpgradeSpeed", "ItemUpgradePlayerSprintSpeed" },
            { "playerUpgradeStrength", "ItemUpgradePlayerGrabStrength" },
            { "playerUpgradeRange", "ItemUpgradePlayerGrabRange" },
            { "playerUpgradeThrow", "ItemUpgradePlayerThrowStrength" },
            { "playerUpgradeLaunch", "ItemUpgradePlayerTumbleLaunch" },
            { "playerUpgradeTumbleClimb", "ItemUpgradePlayerTumbleClimb" },
            { "playerUpgradeCrouchRest", "ItemUpgradePlayerCrouchRest" },
            { "playerUpgradeDeathHeadBattery", "ItemUpgradeDeathHeadBattery" },
            { "playerUpgradeTumbleWings", "ItemUpgradePlayerTumbleWings" },
            { "playerUpgradeMapPlayerCount", "ItemUpgradeMapPlayerCount" },
        };
    }
}