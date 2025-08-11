using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CreateNickName : MonoBehaviour
{
    [Header("닉네임 패널")]
    [field: SerializeField] public RectTransform Holder { get; private set; }
    [SerializeField] private Button createNickNameBtn;
    [SerializeField] private TMP_InputField nickNameInputField;


    private void Awake()
    {
        createNickNameBtn.onClick.AddListener(OnClickCreateNickNameBtn);
        nickNameInputField.onValueChanged.AddListener(OnValueChangedNickName);

        OnValueChangedNickName(string.Empty);
        OnClickCreateNickNameBtn();
    }

    private void OnDestroy()
    {
        createNickNameBtn.onClick.RemoveAllListeners();
        nickNameInputField.onValueChanged.RemoveAllListeners();
    }

    private void OnClickCreateNickNameBtn()
    {
        if(string.IsNullOrEmpty(nickNameInputField.text))
        {
            Debug.LogWarning("닉네임이 비어있습니다.");
            return;
        }

        Debug.Log($"닉네임 설정 : {nickNameInputField.text}");
    }

    private void OnValueChangedNickName(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 5)
        {
            createNickNameBtn.interactable = false;
            return;
        }

        createNickNameBtn.interactable = true;
    }
}