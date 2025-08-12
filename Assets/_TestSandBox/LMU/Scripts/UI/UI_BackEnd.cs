using UnityEngine;

public class UI_BackEnd : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_PanelEdgeTransition _nicknameTransition;
    [SerializeField] private UI_Continue _nicknameContinue;

    private void Awake()
    {
        _nicknameTransition.gameObject.SetActive(false);
        _nicknameContinue.gameObject.SetActive(false);
    }

    public void ShowNickNamePanel(bool value)
    {
        if (value && this.gameObject.activeSelf == false)
            this.gameObject.SetActive(true);

        if (value)
            _nicknameTransition.Show();
        else
            _nicknameTransition.Hide();
    }

    public void ShowContinuePanel(bool value)
    {
        if (value && this.gameObject.activeSelf == false)
            this.gameObject.SetActive(true);

        if (value)
            _nicknameContinue.Show();
        else
            _nicknameContinue.Hide();
    }
}
