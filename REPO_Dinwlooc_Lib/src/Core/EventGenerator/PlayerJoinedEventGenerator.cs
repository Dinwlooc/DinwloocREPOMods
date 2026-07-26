using System.Collections.Generic;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    /// <summary>
    /// 检测玩家加入（任意场景）并发布 <see cref="PlayerJoinedEvent"/>（仅主机/单机模式）。
    /// 使用时必须调用 <see cref="RegisterStep(int)"/> 启用检测。
    /// 本生成器不限制场景，适用于需要感知所有玩家加入的模组。
    /// </summary>
    public class PlayerJoinedEventGenerator : EventGeneratorBase<PlayerJoinedEvent>
    {
        private static PlayerJoinedEventGenerator? _instance;
        public static PlayerJoinedEventGenerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject(nameof(PlayerJoinedEventGenerator));
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PlayerJoinedEventGenerator>();
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
            // 不默认注册步长，由调用者注册
        }

        protected override void GenerateEvent()
        {
            // 仅主机/单机发布
            if (!CoreBridge.Instance.IsMasterClientOrSingleplayer())
                return;

            // 不限制场景，任意场景均检测
            List<PlayerAvatar> players = _playerBridge.GetAllPlayers();
            if (players == null || players.Count == 0)
            {
                _previousPlayerSteamIDs.Clear();
                return;
            }

            HashSet<string> currentSteamIDs = new HashSet<string>();
            foreach (PlayerAvatar p in players)
            {
                if (p == null) continue;
                string steamID = SemiFunc.PlayerGetSteamID(p);
                if (!string.IsNullOrEmpty(steamID))
                    currentSteamIDs.Add(steamID);
            }

            foreach (string steamID in currentSteamIDs)
            {
                if (!_previousPlayerSteamIDs.Contains(steamID))
                {
                    PlayerAvatar? newPlayer = players.Find(p => p != null && SemiFunc.PlayerGetSteamID(p) == steamID);
                    if (newPlayer != null)
                    {
                        EventBus.Publish(new PlayerJoinedEvent(newPlayer));
                        CommonPlugin.Logger.LogInfo($"[PlayerJoinedEventGenerator] Player joined (scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}): {steamID}");
                    }
                }
            }

            _previousPlayerSteamIDs = currentSteamIDs;
        }
    }
}