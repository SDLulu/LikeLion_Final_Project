using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_PreventPanel : MonoBehaviour
{
    public void SetActivePanel(bool value)
    {
        string lobbyScene = GlobalSetting.Inst.LobbyScenePath;
        string gameScene = GlobalSetting.Inst.GameScenePath;
        string currentScene = SceneManager.GetActiveScene().name;

        // 로비씬이거나 게임씬인경우 투명도가 0인 패널을 활성화
        if ((currentScene == lobbyScene || currentScene == gameScene) && value)
        {
            gameObject.SetActive(value);
            var image = gameObject.GetComponent<Image>();
            var originColor = image.color;
            originColor.a = 0.0f;
            image.color = originColor;
            return;
        }

        if (value)
        {
            gameObject.SetActive(true);
            var image = gameObject.GetComponent<Image>();
            var originColor = image.color;
            originColor.a = 0.5f;
            image.color = originColor;
        }
        else
        {
            gameObject.SetActive(false);
            var image = gameObject.GetComponent<Image>();
            var originColor = image.color;
            originColor.a = 0.5f;
            image.color = originColor;
        }
    }
}
