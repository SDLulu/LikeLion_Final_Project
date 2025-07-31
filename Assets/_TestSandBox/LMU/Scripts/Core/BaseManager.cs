using UnityEngine;

namespace LMCore
{
    public abstract class BaseManager<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region Singleton
        private static bool _shuttingDown = false;
        private static readonly object _lock = new object();
        private static T _instance;

        public static bool HasInstance => !_shuttingDown && (_instance != null && _instance.gameObject != null);

        public static T Inst
        {
            get
            {
                if (_shuttingDown)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindAnyObjectByType<T>();
                        if (_instance == null)
                        {
                            var singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = $"{typeof(T)} (Singleton)";
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                    return _instance;
                }
            }
        }
        #endregion

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(this.gameObject);
            }
            else if (_instance != this)
            {
                Debug.Log($"[Singleton] Instance of '{typeof(T)}' already exists. Destroying duplicate.");
                Destroy(this.gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            _shuttingDown = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _shuttingDown = true;
            }
        }
    }
}