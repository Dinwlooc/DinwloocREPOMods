using Photon.Pun;
using UnityEngine;
using System.Reflection;

namespace QuickReload
{
    public class RepoGameBridge
    {
        private static RepoGameBridge? _instance;
        public static RepoGameBridge Instance => _instance ??= new RepoGameBridge();
        private RepoGameBridge() { }

        public bool IsMasterClientOrSingleplayer() => SemiFunc.IsMasterClientOrSingleplayer();
        public bool IsMainMenu() => SemiFunc.IsMainMenu();

        public string? GetCurrentSaveFileName()
        {
            try
            {
                var stats = StatsManager.instance;
                if (stats != null)
                {
                    var field = stats.GetType().GetField("saveFileCurrent", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field != null)
                        return field.GetValue(stats) as string;
                }
            }
            catch { }
            return null;
        }

        public void LoadCurrentSave()
        {
            string? fileName = GetCurrentSaveFileName();
            if (!string.IsNullOrEmpty(fileName))
                SemiFunc.SaveFileLoad(fileName, null);
        }

        public void SaveCurrentProgress() => SemiFunc.SaveFileSave();

        public void SyncToClients()
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            {
                if (PunManager.instance != null)
                    PunManager.instance.SyncAllDictionaries();
            }
        }
    }
}