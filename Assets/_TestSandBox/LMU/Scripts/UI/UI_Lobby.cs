using UnityEngine;
using UnityEngine.UI;

public class UI_Lobby : MonoBehaviour
{
    private UI_Controller uiController;
    public UI_Controller UIController
    {
        get
        {
            return uiController ??= FindAnyObjectByType<UI_Controller>();
        }
    }

    [Header("인스펙터 참조")]
    [SerializeField] private Button _backButton;
    [SerializeField] private UI_CharacterSlotContainer _characterSlotContainer;

    private void Awake()
    {
        _backButton.onClick.AddListener(OnClickBackButton);
    }

    private void OnDestroy()
    {
        _backButton.onClick.RemoveAllListeners();
        uiController = null;
    }


    private void OnClickBackButton()        
    {
        LobbyManager.Inst.NetRunner.Shutdown();
        UIController.ActiveTitleUI();
    }

    public void UpdateData(Fusion.NetworkDictionary<int, TempNetPlayer> players)
    {
        _characterSlotContainer?.UpdateData(players);
    }
}
