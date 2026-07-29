using Dinwlooc.Common.Core;
using Dinwlooc.Common.Events;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    /// <summary>
    /// 挂载到怪物 GameObject 上，捕获游戏内实时事件（受伤、死亡、视觉、调查、被抓住等）。
    /// 通过通用 <see cref="EventBus"/> 发布事件。仅在主机端生效。
    /// 
    /// 注意：生成事件（OnSpawn）和消失事件（OnDespawn）由 EnemyEventGenerator 周期比对处理，不在此处发布。
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyEventRelay : MonoBehaviour
    {
        private Enemy _enemy;
        private EnemyParent _enemyParent;
        private EnemyHealth _enemyHealth;

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            if (_enemy != null)
            {
                _enemyParent = _enemy.EnemyParent;
                _enemyHealth = _enemy.Health;
            }

            // 绑定 UnityEvent 监听（受伤、死亡）
            if (_enemyHealth != null)
            {
                _enemyHealth.onHurt.AddListener(OnHurtUnityEvent);
                _enemyHealth.onDeath.AddListener(OnDeathUnityEvent);
            }
        }

        private void OnDestroy()
        {
            if (_enemyHealth != null)
            {
                _enemyHealth.onHurt.RemoveListener(OnHurtUnityEvent);
                _enemyHealth.onDeath.RemoveListener(OnDeathUnityEvent);
            }
        }

        // ---- UnityEvent 监听器 ----
        private void OnHurtUnityEvent()
        {
            if (!IsHost()) return;
            if (_enemyParent != null)
                EventBus.Publish(new EnemyHurtEvent(_enemyParent));
        }

        private void OnDeathUnityEvent()
        {
            if (!IsHost()) return;
            if (_enemyParent != null)
                EventBus.Publish(new EnemyDiedEvent(_enemyParent));
        }

        // ---- 公共方法（通过 SendMessage 触发，如 EnemyVision 调用） ----
        public void OnVision()
        {
            if (!IsHost()) return;
            if (_enemyParent != null)
                EventBus.Publish(new EnemyVisionEvent(_enemyParent));
        }

        public void OnInvestigate()
        {
            if (!IsHost()) return;
            if (_enemyParent != null)
                EventBus.Publish(new EnemyInvestigateEvent(_enemyParent));
        }

        public void OnGrabbed()
        {
            if (!IsHost()) return;
            if (_enemyParent != null)
                EventBus.Publish(new EnemyGrabbedEvent(_enemyParent));
        }

        // ---- 兼容性方法（不发布事件，以防误调用） ----
        public void OnSpawn() { }        // 由生成器处理
        public void OnDespawn() { }      // 由生成器处理
        public void OnHurt() { }         // 已通过 UnityEvent 处理，此方法保留不操作
        public void OnDeath() { }        // 已通过 UnityEvent 处理

        private static bool IsHost()
        {
            return SemiFunc.IsMasterClientOrSingleplayer();
        }
    }
}