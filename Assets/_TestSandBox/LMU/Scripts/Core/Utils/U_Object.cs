using System.Collections.Generic;
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
        public static T FindObjectByTypeAtCurScene<T>(this MonoBehaviour mono) where T : Component
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            //Debug.Log($"FindObjectByTypeAtCurScene - rootObjects.Length: {rootObjects.Length}");
            foreach (var root in rootObjects)
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }

            Debug.Log($"{scene.name}에서 {typeof(T).Name} 찾을 수 없음");
            return null;
        }

        /// <summary>
        /// 현재씬에서 특정 타입 탐색하고 리스트로 반환
        /// </summary>
        public static List<T> FindObjectsByTypeAtCurScene<T>(this MonoBehaviour mono) where T : Component
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            var list = new List<T>();

            foreach (var root in rootObjects)
            {
                var components = root.GetComponentsInChildren<T>(true);
                if (components != null && components.Length > 0)
                    list.AddRange(components);
            }

            if (list == null || list.Count <= 0)
                Debug.Log($"{scene.name}에서 {typeof(T).Name} 찾을 수 없음");
            return list;
        }
    }
}
