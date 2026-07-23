using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Bridge;
using Dinwlooc.Common.Core;
using UnityEngine;

namespace MonsterHighlight
{
    [BepInPlugin("Dinwlooc.MonsterHighlight", "MonsterHighlight", "1.0.0")]
    [BepInDependency("Dinwlooc.Common")]
    public class MonsterHighlight : BaseUnityPlugin
    {
        public new static ManualLogSource Logger { get; private set; } = null!;

        private MonsterHighlightConfig _config = null!;
        private HighlightController _controller = null!;

        private void Awake()
        {
            Logger = base.Logger;
            _config = new MonsterHighlightConfig(Config);
            var enemyBridge = BridgeLocator.Enemy;
            var playerBridge = BridgeLocator.Player;
            var gameStateBridge = BridgeLocator.GameState;

            _controller = new HighlightController(
                _config,
                enemyBridge,
                playerBridge,
                gameStateBridge
            );
        }

        private void Start()
        {
            if (_config.EnableMod.Value)
                _controller.Start();
        }

        private void OnDestroy()
        {
            _controller.Stop();
        }
    }
}