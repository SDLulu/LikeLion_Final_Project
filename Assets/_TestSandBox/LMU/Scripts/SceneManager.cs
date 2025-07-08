using LMCore;
using UnityEngine;

public class LM_SceneManager : BaseManager<LM_SceneManager>
{
    public void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public async Awaitable LoadSceneAsync(string sceneName)
    {
        await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
    }
}
