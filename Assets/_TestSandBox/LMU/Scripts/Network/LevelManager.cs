using System;
using System.Collections;
using Fusion;
using LMCore;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : NetworkSceneManagerDefault
{
    public static LevelManager Inst => BaseManager<LevelManager>.Inst;

    public static void LoadScene(string sceneName)
    {
        Inst.Runner.LoadScene(sceneName);
    }

    public static async Awaitable LoadSceneAsync(string sceneName,
                    LoadSceneMode loadSceneMode,
                    bool setActiveOnLoad = true,
                    Action onLoadComplete = null)
    {
        try
        {
            await Inst.Runner.LoadScene(sceneName, new LoadSceneParameters(loadSceneMode), setActiveOnLoad);
            await Awaitable.NextFrameAsync();
            onLoadComplete?.Invoke();
            await Awaitable.NextFrameAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"{sceneName} 씬 로드중 오류");
            Debug.LogError($"오류 내용: {e.Message}");
        }
    }

    protected override IEnumerator LoadSceneCoroutine(SceneRef sceneRef, NetworkLoadSceneParameters sceneParams)
    {
        yield return base.LoadSceneCoroutine(sceneRef, sceneParams);

        var scene = SceneManager.GetSceneByBuildIndex(sceneRef.AsIndex);
        string lobbyName = GlobalSetting.Inst.LobbyScenePath;
        string gameName = GlobalSetting.Inst.GameScenePath;
        if (scene.name == lobbyName || scene.name == gameName)
        {
            CameraMover.Inst.SetSolidColorEnv();
        }
        else
        {
            CameraMover.Inst.SetSkyBoxEnv();
        }
    }

    protected override IEnumerator UnloadSceneCoroutine(SceneRef sceneRef)
    {
        return base.UnloadSceneCoroutine(sceneRef);
    }
}
