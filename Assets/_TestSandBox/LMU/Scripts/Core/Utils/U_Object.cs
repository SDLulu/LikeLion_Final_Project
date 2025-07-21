using UnityEngine;

namespace LMCore
{
    public static class U_Object
    {
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component
        {
            return obj.GetComponent<T>() ?? obj.AddComponent<T>();
        }

        public static T GetOrAddComponent<T>(this Component obj) where T : Component
        {
            return obj.GetComponent<T>() ?? obj.gameObject.AddComponent<T>();
        }

        /// <summary>
        /// 현재씬에서 특정 타입 탐색
        /// </summary>
        public static T FindObjectByTypeAtCurScene<T>(this MonoBehaviour mono) where T : MonoBehaviour
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var objects = scene.GetRootGameObjects();
            foreach (var obj in objects)
            {
                var component = obj.GetComponent<T>();
                if (component != null)
                    return component;
            }
            return null;
        }
    }
}
