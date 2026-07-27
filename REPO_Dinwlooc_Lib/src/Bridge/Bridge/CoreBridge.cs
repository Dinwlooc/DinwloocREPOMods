// Dinwlooc.Common/Bridge/CoreBridge.cs
using System;
using System.Reflection;
using Dinwlooc.Common.IBridge;
using Dinwlooc.Common.Reflection;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dinwlooc.Common.Bridge
{
    public class CoreBridge :
        IGameStateBridge,
        ISaveLoadBridge,
        INetworkBridge
    {
        private static CoreBridge? _instance;
        public static CoreBridge Instance => _instance ??= new CoreBridge();
        private CoreBridge() { }

        // ---------- IGameStateBridge（带异常保护） ----------
        public bool IsMasterClientOrSingleplayer()
        {
            try
            {
                return SemiFunc.IsMasterClientOrSingleplayer();
            }
            catch (Exception ex)
            {
                Core.CommonPlugin.Logger.LogWarning($"IsMasterClientOrSingleplayer failed (early init?), defaulting to true: {ex.Message}");
                return true; // 安全默认值，允许本地操作
            }
        }

        public bool IsMainMenu()
        {
            try
            {
                return SemiFunc.IsMainMenu();
            }
            catch
            {
                return false;
            }
        }

        public bool IsInTransit()
        {
            try
            {
                return !SemiFunc.RunIsLevel() && !SemiFunc.RunIsShop() && !IsMainMenu();
            }
            catch
            {
                return true; // 不确定时认为处于过渡状态
            }
        }

        public bool IsLevelLoaded()
        {
            try
            {
                return LevelGenerator.Instance != null && LevelGenerator.Instance.Generated;
            }
            catch
            {
                return false;
            }
        }

        // ---------- ISaveLoadBridge ----------
        public string? GetCurrentSaveFileName()
        {
            try
            {
                StatsManager stats = StatsManager.instance;
                if (stats == null) return null;
                FieldInfo field = ReflectionCache.StatsManager_saveFileCurrent;
                return field?.GetValue(stats) as string;
            }
            catch { return null; }
        }

        public void LoadCurrentSave()
        {
            string? fileName = GetCurrentSaveFileName();
            if (!string.IsNullOrEmpty(fileName))
                SemiFunc.SaveFileLoad(fileName, null);
        }

        public void SaveCurrentProgress() => SemiFunc.SaveFileSave();

        public void RestartScene()
        {
            if (RunManager.instance != null && GameDirector.instance != null)
            {
                RunManager.instance.RestartScene();
                return;
            }
            string sceneName = SceneManager.GetActiveScene().name;
            if (SemiFunc.IsMultiplayer())
                PhotonNetwork.LoadLevel(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }

        public void ChangeToShop()
        {
            if (RunManager.instance == null) return;
            if (!IsMasterClientOrSingleplayer()) return;
            LoadCurrentSave();
            SyncDictionariesToClients();
            RunManager.instance.ChangeLevel(false, false, RunManager.ChangeLevelType.Shop);
        }

        // ---------- INetworkBridge ----------
        public void SyncDictionariesToClients()
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            {
                if (PunManager.instance != null)
                    PunManager.instance.SyncAllDictionaries();
            }
        }
    }
}