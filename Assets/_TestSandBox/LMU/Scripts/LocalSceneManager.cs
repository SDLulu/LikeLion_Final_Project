using LMCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class LocalSceneManager : BaseManager<LocalSceneManager>
{
    public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        SceneManager.LoadScene(sceneName, mode);
    }

    public async Awaitable LoadSceneAsync(string sceneName,
                                            LoadSceneMode mode = LoadSceneMode.Single,
                                            bool activeScene = false,
                                            Action onPreLoad = default,
                                            Action onPostLoad = default)
    {
        onPreLoad?.Invoke();
        await SceneManager.LoadSceneAsync(sceneName, mode);
        if (activeScene)
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        }
        await Awaitable.NextFrameAsync();
        onPostLoad?.Invoke();
    }

    public async Awaitable UnloadSceneAsync(string sceneName)
    {
        await SceneManager.UnloadSceneAsync(sceneName);

        // 게임씬 종료시 카메라 SkyBox 복구
        string gameName = GlobalSetting.Inst.GameScenePath;
        if (sceneName == gameName)
        {
            CameraMover.Inst.SetSkyBoxEnv();
        }
    }

    public Scene GetActiveScene()
    {
        return SceneManager.GetActiveScene();
    }

    /// <summary>
    /// 현재 로드된 모든 씬 정보를 가져오는 함수
    /// </summary>
    public List<Scene> GetAllLoadedScenes()
    {
        List<Scene> loadedScenes = new();
        int sceneCount = SceneManager.sceneCount;

        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                loadedScenes.Add(scene);
            }
        }

        return loadedScenes;
    }

    /// <summary>
    /// 특정 씬이 현재 로드되어 있는지 여부 반환
    /// </summary>
    public bool IsSceneLoaded(string sceneName)
    {
        int sceneCount = SceneManager.sceneCount;

        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name == sceneName)
            {
                return true;
            }
        }

        return false;
    }
}
