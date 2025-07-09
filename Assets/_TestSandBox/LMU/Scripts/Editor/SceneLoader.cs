using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : EditorWindow
{
    [MenuItem("에디터툴/편의기능/씬로드 언로드")]
    public static void ShowWindow()
    {
        GetWindow<SceneLoader>("Scene Load-Unload Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Open Scenes", EditorStyles.boldLabel);

        // 현재 열려있는 모든 씬 목록 가져오기
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            Scene scene = EditorSceneManager.GetSceneAt(i);
            if (scene.IsValid())
            {
                bool isLoaded = scene.isLoaded;
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(sceneName, GUILayout.Width(200));

                // 로드/언로드 버튼
                if (isLoaded)
                {
                    if (GUILayout.Button("Unload"))
                    {
                        if (PromptSaveModifiedScenes(new Scene[] { scene }))
                        {
                            EditorSceneManager.CloseScene(scene, false);
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("Load"))
                    {
                        if (PromptSaveModifiedScenes(GetLoadedScenes()))
                        {
                            EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
                        }
                    }
                }

                // 선택한 씬만 로드하고 나머지 언로드
                if (GUILayout.Button("Load This, Unload Others"))
                {
                    if (PromptSaveModifiedScenes(GetLoadedScenes()))
                    {
                        LoadSingleScene(scene.path);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private Scene[] GetLoadedScenes()
    {
        // 현재 로드된 씬들만 수집
        Scene[] loadedScenes = new Scene[EditorSceneManager.sceneCount];
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            loadedScenes[i] = EditorSceneManager.GetSceneAt(i);
        }
        return loadedScenes;
    }

    private bool PromptSaveModifiedScenes(Scene[] scenesToCheck)
    {
        // 변경된 씬이 있는 경우 저장 여부 대화상자 표시
        if (EditorSceneManager.SaveModifiedScenesIfUserWantsTo(scenesToCheck))
        {
            return true; // 저장했거나 저장하지 않아도 진행 가능
        }
        return false; // 사용자가 취소를 선택한 경우
    }

    private void LoadSingleScene(string scenePath)
    {
        // 선택한 씬을 로드
        if (!EditorSceneManager.GetSceneByPath(scenePath).isLoaded)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        // 선택한 씬을 활성 씬으로 설정
        Scene targetScene = EditorSceneManager.GetSceneByPath(scenePath);
        if (targetScene.IsValid())
        {
            EditorSceneManager.SetActiveScene(targetScene);
        }

        // 모든 씬을 먼저 언로드
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            Scene scene = EditorSceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.path != scenePath)
            {
                EditorSceneManager.CloseScene(scene, false);
            }
        }
    }

    private void OnEnable()
    {
        // Hierarchy 창이 변경될 때 UI 갱신
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.sceneClosed += OnSceneClosed;
    }

    private void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneClosed -= OnSceneClosed;
    }

    private void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        Repaint(); // UI 갱신
    }

    private void OnSceneClosed(Scene scene)
    {
        Repaint(); // UI 갱신
    }
}