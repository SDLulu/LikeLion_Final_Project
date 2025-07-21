using UnityEngine;

namespace LMCore
{
    public abstract class BaseManager<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region Singlton
        private static bool _shuttingDown = false;
        private static object _lock = new object();
        private static T _instance;

        /// <summary> 인스턴스가 존재하는지 확인</summary>
        public static bool HasInstance => !_shuttingDown && (_instance != null && _instance.gameObject != null);

        /// <summary> Access singleton instance through this propriety. </summary>
        public static T Inst
        {
            get
            {
                if (_shuttingDown)
                {
                    Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                    "' already destroyed. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // Search for existing instance.
                        _instance = FindAnyObjectByType<T>(); 

                        // Create new instance if one doesn't already exist.
                        if (_instance == null)
                        {
                            // Need to create a new GameObject to attach the singleton to.
                            var singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T).ToString() + " (Singleton)";

                            //Make instance persistent.
                            DontDestroyOnLoad(singletonObject);
                        }
                    }

                    return _instance;
                }
            }
        }
        #endregion

        private void OnApplicationQuit()
        {
            _shuttingDown = true;
        }

        private void OnDestroy()
        {
            _instance = null;
            _shuttingDown = true;
        }
    }
}