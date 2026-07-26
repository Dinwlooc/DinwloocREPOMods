using System;
using System.Collections.Generic;
using System.Reflection;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    public class EnemyBridge : IEnemyBridge
    {
        private static EnemyBridge? _instance;
        public static EnemyBridge Instance => _instance ??= new EnemyBridge();
        private EnemyBridge() { }

        private static FieldInfo? _rigidField;

        public IReadOnlyList<EnemyParent> GetAllEnemies()
        {
            EnemyDirector director = EnemyDirector.instance;
            if (director == null)
            {
                return Array.Empty<EnemyParent>();
            }
            List<EnemyParent> list = director.enemiesSpawned;
            if (list == null)
            {
                return Array.Empty<EnemyParent>();
            }
            return list;
        }

        public bool IsEnemyValid(EnemyParent enemy)
        {
            if (enemy == null)
            {
                return false;
            }
            if (!enemy.Spawned)
            {
                return false;
            }
            if (enemy.Enemy == null)
            {
                return false;
            }
            if (enemy.Enemy.Health == null)
            {
                return false;
            }
            if (enemy.Enemy.Health.health <= 0)
            {
                return false;
            }
            if (!enemy.Enemy.gameObject.activeInHierarchy)
            {
                return false;
            }
            return true;
        }

        public Vector3 GetEnemyPosition(EnemyParent enemy)
        {
            if (enemy?.Enemy == null)
            {
                return Vector3.zero;
            }
            Enemy enemyComp = enemy.Enemy;
            if (enemyComp.CenterTransform != null)
            {
                return enemyComp.CenterTransform.position;
            }
            if (enemyComp.transform != null)
            {
                return enemyComp.transform.position;
            }
            return Vector3.zero;
        }

        public int GetEnemyInstanceId(EnemyParent enemy)
        {
            if (enemy?.Enemy == null)
            {
                return 0;
            }
            return enemy.Enemy.GetInstanceID();
        }

        public void ApplyHighlight(EnemyParent enemy, bool active, Color color)
        {
            if (enemy == null || enemy.EnableObject == null)
            {
                return;
            }

            GameObject enableObj = enemy.EnableObject;
            Transform modelTransform = enableObj.transform.Find("[VISUALS]");
            if (modelTransform == null)
            {
                modelTransform = enableObj.transform.Find("Visual");
            }
            if (modelTransform == null)
            {
                modelTransform = enableObj.transform.Find("Model");
            }

            GameObject modelTarget = modelTransform != null ? modelTransform.gameObject : enableObj;
            Renderer[] renderers = modelTarget.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rend in renderers)
            {
                if (rend == null)
                {
                    continue;
                }
                if (rend.GetComponent<ParticleSystem>() != null)
                {
                    continue;
                }
                Material mat = rend.material;
                if (!mat.HasProperty("_EmissionColor"))
                {
                    continue;
                }

                if (active)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 2f);
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        public float GetIndicatorHeightOffset(EnemyParent enemy)
        {
            if (enemy?.Enemy == null)
            {
                return 0.5f;
            }
            Enemy enemyComp = enemy.Enemy;

            if (_rigidField == null)
            {
                _rigidField = typeof(Enemy).GetField("Rigidbody",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
            }

            try
            {
                EnemyRigidbody? rigid = _rigidField?.GetValue(enemyComp) as EnemyRigidbody;
                if (rigid != null)
                {
                    Collider[] colliders = rigid.GetComponentsInChildren<Collider>();
                    Vector3 center = GetEnemyPosition(enemy);
                    Bounds bounds = new Bounds(center, Vector3.zero);
                    bool hasBounds = false;
                    foreach (Collider col in colliders)
                    {
                        if (col == null || col.isTrigger)
                        {
                            continue;
                        }
                        if (!hasBounds)
                        {
                            bounds = col.bounds;
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(col.bounds);
                        }
                    }
                    if (hasBounds)
                    {
                        float height = bounds.max.y - center.y + 0.15f;
                        return Mathf.Clamp(height, 0.3f, 5f);
                    }
                }
            }
            catch
            {
                // 忽略异常，继续尝试备用方法
            }

            try
            {
                Transform model = enemyComp.CenterTransform;
                if (model != null)
                {
                    Renderer renderer = model.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        float height = renderer.bounds.size.y * 0.8f;
                        return Mathf.Clamp(height, 0.3f, 3f);
                    }
                    float scaleY = model.lossyScale.y;
                    if (scaleY > 0.5f)
                    {
                        return Mathf.Clamp(scaleY * 0.8f, 0.3f, 3f);
                    }
                }
            }
            catch
            {
                // 忽略异常
            }

            return 0.5f;
        }
    }
}