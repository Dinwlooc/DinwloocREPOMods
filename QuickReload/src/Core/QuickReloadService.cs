using Photon.Pun;
using UnityEngine;

namespace QuickReload
{
    public sealed class QuickReloadService
    {
        private const int MaxRandomAttempts = 50;

        private readonly RepoGameBridge _bridge;

        public QuickReloadService(RepoGameBridge bridge)
        {
            _bridge = bridge;
        }

        /// <summary>
        /// 重载当前场景（根据场景类型执行不同前置操作）。
        /// </summary>
        public void ReloadCurrentScene()
        {
            if (!_bridge.IsMasterClientOrSingleplayer())
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
            if (!_bridge.IsMasterClientOrSingleplayer())
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
                _bridge.LoadCurrentSave();

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

        /// <summary>
        /// 商店/大厅重载：保存当前进度。
        /// </summary>
        private void ReloadShopOrLobby()
        {
            QuickReload.Logger.LogInfo("Saving current progress...");
            _bridge.SaveCurrentProgress();
        }

        /// <summary>
        /// 关卡重载：加载存档、可选的随机切换关卡、通知其他客户端。
        /// </summary>
        private void ReloadLevel(RunManager rm)
        {
            QuickReload.Logger.LogInfo("Loading save to restore initial state...");
            _bridge.LoadCurrentSave();

            // 随机切换到同类型关卡（如果启用）
            bool random = QuickReload.ReloadRandomScene?.Value ?? false;
            if (random)
            {
                Level target = GetRandomLevelOfSameType(rm, rm.levelCurrent);
                if (target != null && target != rm.levelCurrent)
                {
                    rm.levelCurrent = target;
                    QuickReload.Logger.LogInfo($"Randomly switched to level: {target.name}");
                }
            }

            // 通知其他客户端关卡变更
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

        /// <summary>
        /// 清空 StatsManager 的 item 字典（修复电量显示问题）。
        /// </summary>
        private void ClearItemDictionary()
        {
            if (StatsManager.instance != null)
            {
                StatsManager.instance.item.Clear();
                StatsManager.instance.takenItemNames.Clear();
                QuickReload.Logger.LogInfo("Cleared StatsManager.item and takenItemNames.");
            }
        }

        /// <summary>
        /// 同步字典数据给其他客户端，并清理本地 RPC。
        /// </summary>
        private void SyncAndCleanup()
        {
            _bridge.SyncToClients();

            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                PhotonNetwork.RemoveRPCs(PhotonNetwork.LocalPlayer);
                QuickReload.Logger.LogInfo("Cleared local RPCs.");
            }
        }

        /// <summary>
        /// 从当前关卡所在的池子中随机选取一个同类型关卡（避免选中自身）。
        /// </summary>
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