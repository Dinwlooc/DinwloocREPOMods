using UnityEngine;

namespace Dinwlooc.Common.Core
{
    /// <summary>
    /// 泛型单例基类，自动处理实例创建、重复实例销毁，并默认 DontDestroyOnLoad。
    /// </summary>
    /// <typeparam name="T">继承 MonoBehaviour 的类型</typeparam>
    public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T? _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            GameObject gameObject = new GameObject(typeof(T).Name);
                            DontDestroyOnLoad(gameObject);
                            _instance = gameObject.AddComponent<T>();
                        }
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}