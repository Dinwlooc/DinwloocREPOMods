// Dinwlooc.Common/Bridge/CoreBridge.cs
using System;
using Dinwlooc.Common.src.Bridge.IBridge;
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

        // ---------- IGameStateBridge ----------
        public bool IsMasterClientOrSingleplayer() => SemiFunc.IsMasterClientOrSingleplayer();
        public bool IsMainMenu() => SemiFunc.IsMainMenu();
        public bool IsInTransit() => !SemiFunc.RunIsLevel() && !SemiFunc.RunIsShop() && !IsMainMenu();
        public bool IsLevelLoaded() => LevelGenerator.Instance != null && LevelGenerator.Instance.Generated;

        // ---------- ISaveLoadBridge ----------
        public string? GetCurrentSaveFileName()
        {
            try
            {
                var stats = StatsManager.instance;
                if (stats == null) return null;
                var field = stats.GetType().GetField("saveFileCurrent",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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