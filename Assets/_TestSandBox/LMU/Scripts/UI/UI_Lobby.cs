using UnityEngine;
using UnityEngine.UI;

public class UI_Lobby : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private Button _backButton;

    private void Awake()
    {
        _backButton.onClick.AddListener(OnClickBackButton);
    }

    private void OnDestroy()
    {
        _backButton.onClick.RemoveAllListeners();
    }

    private UI_Controller uiController;
    public UI_Controller UIController
    {
        get
        {
            return uiController ??= FindAnyObjectByType<UI_Controller>();
        }
    }
    private void OnClickBackButton()        
    {
        LobbyManager.Inst.NetRunner.Shutdown();
        UIController.ActiveTitleUI();
    }
}
