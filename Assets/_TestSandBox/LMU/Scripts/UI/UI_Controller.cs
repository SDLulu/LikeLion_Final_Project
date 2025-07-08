using UnityEngine;

public interface I_UICanvas
{
    void Active();
    void Deactive();
}
public class UI_Controller : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_Title uiTitle;
    [SerializeField] private UI_Lobby uiLobby;

    private void Awake()
    {
        ActiveTitleUI();
    }

    public void ActiveTitleUI()
    {
        uiTitle.gameObject.SetActive(true);
        uiLobby.gameObject.SetActive(false);
    }

    public void ActiveLobbyUI()
    {
        uiTitle.gameObject.SetActive(false);
        uiLobby.gameObject.SetActive(true);
    }

}
