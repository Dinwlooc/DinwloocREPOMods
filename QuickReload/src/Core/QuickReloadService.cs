using Photon.Pun;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace QuickReload
{
    public sealed class QuickReloadService
    {
        private const int MaxRandomAttempts = 50;

        private readonly IGameStateBridge _gameState;
        private readonly ISaveLoadBridge _saveLoad;
        private readonly INetworkBridge _network;

        public QuickReloadService(IGameStateBridge gameState, ISaveLoadBridge saveLoad, INetworkBridge network)
        {
            _gameState = gameState;
            _saveLoad = saveLoad;
            _network = network;
        }

        /// <summary>
        /// 重载当前场景（根据场景类型执行不同前置操作）。
        /// </summary>
        public void ReloadCurrentScene()
        {
            if (!_gameState.IsMasterClientOrSingleplayer())
                return;

            var rm = RunManager.instance;
            if (rm == null)
            {
                QuickReload.Logger.LogError("RunManager.instance is null, cannot reload.");
                return;
            }

            try
            {
                // 根据场景类型执行不同的准备操作
                if (SemiFunc.RunIsShop() || SemiFunc.RunIsLobby())
                {
                    ReloadShopOrLobby();
                }
                else if (SemiFunc.RunIsLevel())
                {
                    ReloadLevel(rm);
                }
                else
                {
                    QuickReload.Logger.LogWarning("Unsupported scene for reload.");
                    return;
                }

                // 通用清理和同步
                ClearItemDictionary();
                SyncAndCleanup();

                QuickReload.Logger.LogInfo("Restarting scene...");
                rm.RestartScene();
            }
            catch (System.Exception ex)
            {
                QuickReload.Logger.LogError($"Failed to reload scene: {ex.Message}");
            }
        }

        /// <summary>
        /// 跳转到商店（加载存档并切换场景）。
        /// </summary>
        public void GoToShop()
        {
            if (!_gameState.IsMasterClientOrSingleplayer())
                return;

            var rm = RunManager.instance;
            if (rm == null)
            {
                QuickReload.Logger.LogError("RunManager.instance is null, cannot go to shop.");
                return;
            }

            if (SemiFunc.RunIsShop())
            {
                QuickReload.Logger.LogInfo("Already in shop.");
                return;
            }

            try
            {
                QuickReload.Logger.LogInfo("Loading save before going to shop...");
                _saveLoad.LoadCurrentSave();

                ClearItemDictionary();
                SyncAndCleanup();

                QuickReload.Logger.LogInfo("Changing level to shop...");
                rm.ChangeLevel(false, false, RunManager.ChangeLevelType.Shop);
            }
            catch (System.Exception ex)
            {
                QuickReload.Logger.LogError($"Failed to go to shop: {ex.Message}");
            }
        }

        // ---------- 私有辅助方法 ----------

        private void ReloadShopOrLobby()
        {
            QuickReload.Logger.LogInfo("Saving current progress...");
            _saveLoad.SaveCurrentProgress();
        }

        private void ReloadLevel(RunManager rm)
        {
            QuickReload.Logger.LogInfo("Loading save to restore initial state...");
            _saveLoad.LoadCurrentSave();

            bool random = QuickReloadConfig.Instance.ReloadRandomScene.Value;
            if (random)
            {
                Level target = GetRandomLevelOfSameType(rm, rm.levelCurrent);
                if (target != null && target != rm.levelCurrent)
                {
                    rm.levelCurrent = target;
                    QuickReload.Logger.LogInfo($"Randomly switched to level: {target.name}");
                }
            }

            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                var pun = rm.GetComponent<RunManagerPUN>();
                if (pun != null && pun.photonView != null)
                {
                    pun.photonView.RPC(
                        "UpdateLevelRPC",
                        RpcTarget.OthersBuffered,
                        new object[] { rm.levelCurrent.name, rm.levelsCompleted, rm.gameOver }
                    );
                    QuickReload.Logger.LogInfo("Sent UpdateLevelRPC to clients.");
                }
            }
        }

        private void ClearItemDictionary()
        {
            if (StatsManager.instance != null)
            {
                StatsManager.instance.item.Clear();
                StatsManager.instance.takenItemNames.Clear();
                QuickReload.Logger.LogInfo("Cleared StatsManager.item and takenItemNames.");
            }
        }

        private void SyncAndCleanup()
        {
            _network.SyncDictionariesToClients();

            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                PhotonNetwork.RemoveRPCs(PhotonNetwork.LocalPlayer);
                QuickReload.Logger.LogInfo("Cleared local RPCs.");
            }
        }

        private Level GetRandomLevelOfSameType(RunManager rm, Level current)
        {
            if (current == null || rm == null)
                return current;

            var pool = rm.levels.Contains(current) ? rm.levels :
                       rm.levelShop.Contains(current) ? rm.levelShop :
                       null;

            if (pool == null || pool.Count <= 1)
                return current;

            Level target = current;
            int attempts = 0;
            do
            {
                target = pool[Random.Range(0, pool.Count)];
                attempts++;
            } while (target == current && attempts < MaxRandomAttempts);
            return target;
        }
    }
}