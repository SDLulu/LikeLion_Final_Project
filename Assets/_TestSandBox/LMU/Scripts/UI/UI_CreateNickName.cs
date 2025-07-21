using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CreateNickName : MonoBehaviour
{
    [Header("닉네임 패널")]
    [SerializeField] private RectTransform createNickNamePanel;
    [field: SerializeField] public RectTransform Holder { get; private set; }
    [SerializeField] private Button createNickNameBtn;
    [SerializeField] private TMP_InputField nickNameInputField;

    public static FakeClient.Data FakeClientData {get; private set;}

    private void Awake()
    {
        createNickNameBtn.onClick.AddListener(OnClickCreateNickNameBtn);
        nickNameInputField.onValueChanged.AddListener(OnValueChangedNickName);

        FakeClientData = DataManager.Inst.GetRandomFakeClientData();
        nickNameInputField.text = FakeClientData.NickName;
        OnValueChangedNickName(FakeClientData.NickName);
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

        FakeClientData.NickName = nickNameInputField.text;
        
        Debug.Log($"닉네임 설정 : {nickNameInputField.text}");
    }

    // --- 닉네임 입력 패널
    private void OnValueChangedNickName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            createNickNameBtn.interactable = false;
            return;
        }

        createNickNameBtn.interactable = true;
        FakeClientData.NickName = value;
    }
}
