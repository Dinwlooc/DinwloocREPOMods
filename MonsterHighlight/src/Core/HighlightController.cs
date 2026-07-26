using System.Collections;
using System.Collections.Generic;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using Dinwlooc.Common.IBridge;
using MonsterHighlight.Events;
using UnityEngine;

namespace MonsterHighlight
{
    public class HighlightController
    {
        private readonly MonsterHighlightConfig _config;
        private readonly IEnemyBridge _enemyBridge;
        private readonly IPlayerBridge _playerBridge;
        private readonly IGameStateBridge _gameState;

        private readonly IndicatorRenderer _indicatorRenderer = new();
        private Coroutine? _indicatorRoutine;
        private bool _isSubscribed = false;
        private HashSet<int> _visibleMonsters = new();

        public HighlightController(
            MonsterHighlightConfig config,
            IEnemyBridge enemyBridge,
            IPlayerBridge playerBridge,
            IGameStateBridge gameState)
        {
            _config = config;
            _enemyBridge = enemyBridge;
            _playerBridge = playerBridge;
            _gameState = gameState;
        }

        public void Start()
        {
            if (!_config.Enabled.Value) return;

            int stepFrames = _config.GetCheckStepFrames();
            VisionEventGenerator.Instance.RegisterStep(stepFrames);
            MonsterHighlight.Logger.LogInfo($"[HighlightController] Registered step {stepFrames}");

            if (!_isSubscribed)
            {
                EventBus.Subscribe<MonsterVisibilityChangedEvent>(OnVisibilityChanged);
                _isSubscribed = true;
                MonsterHighlight.Logger.LogInfo("[HighlightController] Subscribed.");
            }

            _indicatorRoutine = CommonService.Instance.RunCoroutine(IndicatorRoutine());
            PerformInitialVisionCheck();
        }

        private void PerformInitialVisionCheck()
        {
            var localPlayer = _playerBridge.GetLocalPlayer();
            if (localPlayer == null || localPlayer.isDisabled)
            {
                MonsterHighlight.Logger.LogInfo("[HighlightController] No local player for initial check.");
                return;
            }

            var allEnemies = _enemyBridge.GetAllEnemies();
            var seenIDs = new List<int>();
            foreach (var ep in allEnemies)
            {
                if (!_enemyBridge.IsEnemyValid(ep)) continue;
                Vector3 enemyPos = _enemyBridge.GetEnemyPosition(ep);
                if (SemiFunc.PlayerVisionCheck(enemyPos, 999f, localPlayer, false))
                {
                    seenIDs.Add(_enemyBridge.GetEnemyInstanceId(ep));
                }
            }

            _visibleMonsters = new HashSet<int>(seenIDs);
            ApplyTextureHighlights(allEnemies);
            MonsterHighlight.Logger.LogInfo($"[HighlightController] Initial visible: {seenIDs.Count}");
        }

        public void Stop()
        {
            if (_indicatorRoutine != null)
                CommonService.Instance.StopCoroutineSafe(_indicatorRoutine);

            if (_isSubscribed)
            {
                EventBus.Unsubscribe<MonsterVisibilityChangedEvent>(OnVisibilityChanged);
                _isSubscribed = false;
            }

            VisionEventGenerator.Instance.UnregisterStep(_config.GetCheckStepFrames());
            _indicatorRenderer.ClearAllIndicators();
            _visibleMonsters.Clear();
        }

        private void OnVisibilityChanged(MonsterVisibilityChangedEvent evt)
        {
            // 高频日志已移除，避免刷屏
            if (evt.IsVisible)
                _visibleMonsters.Add(evt.EnemyInstanceId);
            else
                _visibleMonsters.Remove(evt.EnemyInstanceId);

            if (_config.EnableEmission.Value)
            {
                var enemies = _enemyBridge.GetAllEnemies();
                ApplyTextureHighlights(enemies);
            }
        }

        private void ApplyTextureHighlights(IReadOnlyList<EnemyParent> enemies)
        {
            Color color = MonsterHighlightConfig.GetHighlightColor(_config.HighlightPreset.Value);
            foreach (var ep in enemies)
            {
                if (!_enemyBridge.IsEnemyValid(ep)) continue;
                int id = _enemyBridge.GetEnemyInstanceId(ep);
                bool shouldHighlight = _visibleMonsters.Contains(id);
                _enemyBridge.ApplyHighlight(ep, shouldHighlight, color);
                EventBus.Publish(new MonsterHighlightAppliedEvent(id, shouldHighlight));
            }
        }

        private IEnumerator IndicatorRoutine()
        {
            yield return null;
            while (true)
            {
                if (!_config.Enabled.Value || !SemiFunc.RunIsLevel())
                {
                    _indicatorRenderer.ClearAllIndicators();
                    yield return null;
                    continue;
                }

                if (_config.EnableIndicator.Value)
                {
                    var positions = new Dictionary<int, Vector3>();
                    var allEnemies = _enemyBridge.GetAllEnemies();
                    foreach (var ep in allEnemies)
                    {
                        if (!_enemyBridge.IsEnemyValid(ep)) continue;
                        int id = _enemyBridge.GetEnemyInstanceId(ep);
                        if (_visibleMonsters.Contains(id)) continue;
                        Vector3 pos = _enemyBridge.GetEnemyPosition(ep);
                        float offset = _enemyBridge.GetIndicatorHeightOffset(ep);
                        positions[id] = pos + Vector3.up * offset * 0.5f;
                    }

                    int step = Mathf.Max(1, _config.IndicatorUpdateStep.Value);
                    if (Time.frameCount % step == 0)
                    {
                        var localPlayer = _playerBridge.GetLocalPlayer();
                        Vector3 playerPos = (localPlayer != null && !localPlayer.isDisabled)
                            ? localPlayer.transform.position
                            : Vector3.zero;

                        Color color = MonsterHighlightConfig.GetHighlightColor(_config.HighlightPreset.Value);
                        _indicatorRenderer.RenderIndicators(
                            positions,
                            color,
                            _config.IndicatorSize.Value,
                            playerPos,
                            _config.MinDistance.Value,
                            _config.MaxDistance.Value,
                            _config.MinSizeRatio.Value,
                            _config.IndicatorAlpha.Value
                        );
                    }
                }
                else
                {
                    _indicatorRenderer.ClearAllIndicators();
                }

                yield return null;
            }
        }
    }
}