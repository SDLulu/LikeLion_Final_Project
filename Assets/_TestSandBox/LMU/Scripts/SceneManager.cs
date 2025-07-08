using LMCore;
using UnityEngine;

public class LM_SceneManager : BaseManager<LM_SceneManager>
{
    public void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
