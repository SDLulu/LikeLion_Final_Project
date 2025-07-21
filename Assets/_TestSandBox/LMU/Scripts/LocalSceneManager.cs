using LMCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LocalSceneManager : BaseManager<LocalSceneManager>
{
    public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        SceneManager.LoadScene(sceneName, mode);
    }

    public void UnloadScene(string sceneName)
    {
        SceneManager.UnloadSceneAsync(sceneName);
    }

    public async Awaitable LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, bool activeScene = false)
    {
        await SceneManager.LoadSceneAsync(sceneName, mode);
        if (activeScene)
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        }
        await Awaitable.NextFrameAsync();
    }

    public async Awaitable UnloadSceneAsync(string sceneName)
    {
        await SceneManager.UnloadSceneAsync(sceneName);
    }

    public Scene GetActiveScene()
    {
        return SceneManager.GetActiveScene();
    }

    /// <summary>
    /// 현재 로드된 모든 씬 정보를 가져옵니다.
    /// </summary>
    public List<Scene> GetAllLoadedScenes()
    {
        List<Scene> loadedScenes = new List<Scene>();
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
}
