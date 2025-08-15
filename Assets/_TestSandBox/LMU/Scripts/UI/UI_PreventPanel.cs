using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_PreventPanel : MonoBehaviour
{
    public void SetActivePanel(bool value)
    {
        string lobbyScene = GlobalSetting.Inst.LobbyScenePath;
        string gameScene = GlobalSetting.Inst.GameScenePath;
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == lobbyScene || currentScene == gameScene)
            return;

        if (value)
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
