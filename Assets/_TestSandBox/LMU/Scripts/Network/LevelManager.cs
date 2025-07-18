using System;
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
                    UnityEngine.SceneManagement.LoadSceneMode loadSceneMode,
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
}
