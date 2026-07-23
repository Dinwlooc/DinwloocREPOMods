using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterHighlight
{
    public class IndicatorRenderer
    {
        private Dictionary<int, GameObject> _activeIndicators = new();
        private Queue<GameObject> _pool = new();
        private const int MAX_POOL_SIZE = 50;
        private const float CORNER_SIZE = 50f;
        private bool _loggedUIAssetFailure = false;

        private bool GetUIAssets(out GameObject prefab, out RectTransform canvasRect)
        {
            prefab = null;
            canvasRect = null;

            var discover = ValuableDiscover.instance;
            if (discover == null)
            {
                if (!_loggedUIAssetFailure)
                {
                    MonsterHighlight.Logger.LogWarning("ValuableDiscover.instance is null, cannot render indicators.");
                    _loggedUIAssetFailure = true;
                }
                return false;
            }

            if (discover.graphicPrefab == null)
            {
                if (!_loggedUIAssetFailure)
                {
                    MonsterHighlight.Logger.LogWarning("ValuableDiscover.graphicPrefab is null.");
                    _loggedUIAssetFailure = true;
                }
                return false;
            }

            if (discover.canvasRect == null)
            {
                if (!_loggedUIAssetFailure)
                {
                    MonsterHighlight.Logger.LogWarning("ValuableDiscover.canvasRect is null.");
                    _loggedUIAssetFailure = true;
                }
                return false;
            }

            prefab = discover.graphicPrefab;
            canvasRect = discover.canvasRect;
            return true;
        }

        private void SetupIndicator(GameObject indicatorGO, RectTransform canvasRect)
        {
            if (indicatorGO == null || canvasRect == null) return;

            var graphic = indicatorGO.GetComponent<ValuableDiscoverGraphic>();
            if (graphic != null)
            {
                graphic.enabled = false;
                // 确保所有子元素都存在
                if (graphic.middle != null) { graphic.middle.gameObject.SetActive(true); }
                else { MonsterHighlight.Logger.LogWarning("graphic.middle is null"); }
                if (graphic.topLeft != null) { graphic.topLeft.gameObject.SetActive(true); }
                else { MonsterHighlight.Logger.LogWarning("graphic.topLeft is null"); }
                if (graphic.topRight != null) { graphic.topRight.gameObject.SetActive(true); }
                else { MonsterHighlight.Logger.LogWarning("graphic.topRight is null"); }
                if (graphic.botLeft != null) { graphic.botLeft.gameObject.SetActive(true); }
                else { MonsterHighlight.Logger.LogWarning("graphic.botLeft is null"); }
                if (graphic.botRight != null) { graphic.botRight.gameObject.SetActive(true); }
                else { MonsterHighlight.Logger.LogWarning("graphic.botRight is null"); }

                SetRectPivotCenter(graphic.middle);
                SetRectPivotCenter(graphic.topLeft);
                SetRectPivotCenter(graphic.topRight);
                SetRectPivotCenter(graphic.botLeft);
                SetRectPivotCenter(graphic.botRight);
            }
            else
            {
                MonsterHighlight.Logger.LogWarning("ValuableDiscoverGraphic component not found on indicator prefab.");
            }

            RectTransform rootRect = indicatorGO.GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = canvasRect.rect.size;
                rootRect.localRotation = Quaternion.identity;
                rootRect.localScale = Vector3.one;
            }

            var canvasGroup = indicatorGO.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        private void SetRectPivotCenter(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private GameObject GetIndicator(GameObject prefab, RectTransform canvasRect)
        {
            while (_pool.Count > 0 && _pool.Peek() == null)
                _pool.Dequeue();

            if (_pool.Count > 0)
            {
                GameObject go = _pool.Dequeue();
                if (go != null)
                {
                    go.SetActive(true);
                    SetupIndicator(go, canvasRect);
                    return go;
                }
            }

            if (prefab == null || canvasRect == null)
                return null;

            GameObject newGo = Object.Instantiate(prefab, canvasRect, false);
            SetupIndicator(newGo, canvasRect);
            return newGo;
        }

        private void ReturnToPool(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            if (_pool.Count < MAX_POOL_SIZE)
                _pool.Enqueue(go);
            else
                Object.Destroy(go);
        }

        public void ClearAllIndicators()
        {
            foreach (var kv in _activeIndicators)
            {
                if (kv.Value != null)
                    ReturnToPool(kv.Value);
            }
            _activeIndicators.Clear();
        }

        public void Dispose()
        {
            ClearAllIndicators();
            while (_pool.Count > 0)
            {
                GameObject go = _pool.Dequeue();
                if (go != null)
                    Object.Destroy(go);
            }
        }

        public void RenderIndicators(
            Dictionary<int, Vector3> worldPositions,
            Color color,
            float scale,
            Vector3 playerPos,
            int minDist,
            int maxDist,
            float minRatio,
            float alpha)
        {
            if (!GetUIAssets(out GameObject prefab, out RectTransform canvasRect))
                return;

            if (worldPositions == null || worldPositions.Count == 0)
            {
                ClearAllIndicators();
                return;
            }

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                MonsterHighlight.Logger.LogWarning("Camera.main is null, cannot render indicators.");
                return;
            }

            List<int> toRemove = new List<int>();
            foreach (var kv in _activeIndicators)
                if (!worldPositions.ContainsKey(kv.Key))
                    toRemove.Add(kv.Key);
            foreach (int id in toRemove)
            {
                if (_activeIndicators.TryGetValue(id, out GameObject go))
                {
                    ReturnToPool(go);
                    _activeIndicators.Remove(id);
                }
            }

            Vector2 canvasPixelSize = canvasRect.rect.size;
            float minDistF = (float)minDist;
            float maxDistF = (float)maxDist;

            Color centerColor = color;
            centerColor.a = Mathf.Clamp01(alpha);
            Color cornerColor = color;
            cornerColor.a = 1f;

            foreach (var kv in worldPositions)
            {
                int id = kv.Key;
                Vector3 worldPos = kv.Value;

                Vector3 viewportPos = mainCam.WorldToViewportPoint(worldPos);
                bool visible = viewportPos.z > 0 &&
                               viewportPos.x >= 0 && viewportPos.x <= 1 &&
                               viewportPos.y >= 0 && viewportPos.y <= 1;

                if (!_activeIndicators.TryGetValue(id, out GameObject indicatorGO) || indicatorGO == null)
                {
                    indicatorGO = GetIndicator(prefab, canvasRect);
                    if (indicatorGO == null) continue;
                    _activeIndicators[id] = indicatorGO;
                }

                indicatorGO.SetActive(visible);
                if (!visible) continue;

                RectTransform rootRect = indicatorGO.GetComponent<RectTransform>();
                if (rootRect != null && rootRect.sizeDelta != canvasPixelSize)
                {
                    rootRect.sizeDelta = canvasPixelSize;
                }

                ValuableDiscoverGraphic graphic = indicatorGO.GetComponent<ValuableDiscoverGraphic>();
                if (graphic == null) continue;

                Vector2 localPoint = new Vector2(
                    (viewportPos.x - 0.5f) * canvasPixelSize.x,
                    (viewportPos.y - 0.5f) * canvasPixelSize.y
                );

                float dist = Vector3.Distance(playerPos, worldPos);
                float sizeScale = 1f;
                if (minDistF < maxDistF && dist > minDistF)
                {
                    float t = Mathf.InverseLerp(minDistF, maxDistF, dist);
                    sizeScale = Mathf.Lerp(1f, minRatio, t);
                }
                float finalSize = CORNER_SIZE * scale * sizeScale;

                // 中间指示器
                graphic.middle.anchoredPosition = localPoint;
                graphic.middle.sizeDelta = new Vector2(finalSize * 0.4f, finalSize * 0.4f);
                SetImageColor(graphic.middle, centerColor);

                // 四个角指示器
                float cornerOffset = finalSize * 0.5f;
                graphic.topLeft.anchoredPosition = localPoint + new Vector2(-cornerOffset, cornerOffset);
                graphic.topRight.anchoredPosition = localPoint + new Vector2(cornerOffset, cornerOffset);
                graphic.botLeft.anchoredPosition = localPoint + new Vector2(-cornerOffset, -cornerOffset);
                graphic.botRight.anchoredPosition = localPoint + new Vector2(cornerOffset, -cornerOffset);

                graphic.topLeft.sizeDelta = new Vector2(finalSize, finalSize);
                graphic.topRight.sizeDelta = new Vector2(finalSize, finalSize);
                graphic.botLeft.sizeDelta = new Vector2(finalSize, finalSize);
                graphic.botRight.sizeDelta = new Vector2(finalSize, finalSize);

                SetImageColor(graphic.topLeft, cornerColor);
                SetImageColor(graphic.topRight, cornerColor);
                SetImageColor(graphic.botLeft, cornerColor);
                SetImageColor(graphic.botRight, cornerColor);
            }
        }

        private void SetImageColor(RectTransform rt, Color c)
        {
            if (rt == null) return;
            var img = rt.GetComponent<Image>();
            if (img != null)
            {
                img.enabled = true;
                img.color = c;
            }
        }
    }
}