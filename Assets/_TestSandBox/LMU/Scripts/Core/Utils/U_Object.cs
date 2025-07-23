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
            var objects = scene.GetRootGameObjects();
            foreach (var obj in objects)
            {
                var component = obj.GetComponent<T>();
                if (component != null)
                    return component;
            }
            return null;
        }

        /// <summary>
        /// 현재씬에서 특정 타입 탐색하고 리스트로 반환
        /// </summary>
        public static List<T> FindObjectsByTypeAtCurScene<T>(this MonoBehaviour mono) where T : Component
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var objects = scene.GetRootGameObjects();
            var list = new List<T>();
            foreach (var obj in objects)
            {
                var component = obj.GetComponent<T>();
                if (component != null)
                    list.Add(component);
            }
            return list;
        }
    }
}
