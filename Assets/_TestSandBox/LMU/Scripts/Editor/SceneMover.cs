#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

namespace LMCoreEditor
{
    public class SceneMover : EditorWindow
    {
        private const string SCENE_PATH = "Assets/Scenes";
        private List<SceneAsset> _scenes = new();

        [MenuItem("에디터툴/편의기능/SceneMover")]
        public static void ShowWindow()
        {
            GetWindow<SceneMover>("SceneMover");
        }

        private void OnEnable()
        {
            _scenes = GetAllScenes(SCENE_PATH);
        }

        private void OnDisable()
        {
            _scenes.Clear();
        }

        private void OnGUI()
        {
            if (_scenes == null || _scenes.Count <= 0)
            {
                EditorGUILayout.LabelField("SceneRefs에 씬이 설정되지 않았습니다.");
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("새로고침"))
            {
                _scenes.Clear();
                _scenes = GetAllScenes(SCENE_PATH);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("씬 목록", EditorStyles.boldLabel);
            GUILayout.Space(10);

            foreach (var scene in _scenes)
            {
                if (GUILayout.Button(scene.name, GUILayout.Height(30)))
                {
                    LoadScene(scene);
                }
            }
        }

        private List<SceneAsset> GetAllScenes(string folderPath)
        {
            List<SceneAsset> sceneAssets = new List<SceneAsset>();
            
            if (Directory.Exists(folderPath))
            {
                string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });
                
                foreach (string guid in sceneGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    
                    if (sceneAsset != null)
                    {
                        sceneAssets.Add(sceneAsset);
                    }
                }
            }
            
            return sceneAssets;
        }

        private void LoadScene(SceneAsset sceneAsset)
        {
            if (sceneAsset == null) return;

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
    }
}

#endif