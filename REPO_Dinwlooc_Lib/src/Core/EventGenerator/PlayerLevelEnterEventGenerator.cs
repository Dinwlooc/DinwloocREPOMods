using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.src.Bridge.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    /// <summary>
    /// 检测玩家加入并发布 PlayerJoinedEvent（仅主机/单机模式）
    /// 使用时必须调用 RegisterStep(帧数) 启用检测。
    /// </summary>
    public class PlayerLevelEnterEventGenerator : EventGeneratorBase<PlayerLevelEnteredEvent>
    {
        private static PlayerLevelEnterEventGenerator? _instance;
        public static PlayerLevelEnterEventGenerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(nameof(PlayerLevelEnterEventGenerator));
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PlayerLevelEnterEventGenerator>();
                }
                return _instance;
            }
        }

        private IPlayerBridge _playerBridge = null!;
        private HashSet<string> _previousPlayerSteamIDs = new();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _playerBridge = BridgeLocator.Player;
            // 注意：不默认注册步长，由调用者注册
        }

        protected override void GenerateEvent()
        {
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer())
                return;

            if (!SemiFunc.RunIsLevel())
                return;

            var players = _playerBridge.GetAllPlayers();
            if (players == null || players.Count == 0)
            {
                _previousPlayerSteamIDs.Clear();
                return;
            }

            var currentSteamIDs = new HashSet<string>();
            foreach (var p in players)
            {
                if (p == null) continue;
                string steamID = SemiFunc.PlayerGetSteamID(p);
                if (!string.IsNullOrEmpty(steamID))
                    currentSteamIDs.Add(steamID);
            }

            foreach (var steamID in currentSteamIDs)
            {
                if (!_previousPlayerSteamIDs.Contains(steamID))
                {
                    PlayerAvatar? newPlayer = players.Find(p => p != null && SemiFunc.PlayerGetSteamID(p) == steamID);
                    if (newPlayer != null)
                    {
                        EventBus.Publish(new PlayerLevelEnteredEvent(newPlayer));
                        CommonPlugin.Logger.LogInfo($"[PlayerJoinEventGenerator] Player joined: {steamID}");
                    }
                }
            }

            _previousPlayerSteamIDs = currentSteamIDs;
        }
    }
}