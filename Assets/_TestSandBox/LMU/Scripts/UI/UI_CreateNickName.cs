using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CreateNickName : MonoBehaviour
{
    [Header("닉네임 패널")]
    [field: SerializeField] public RectTransform Holder { get; private set; }
    [SerializeField] private Button _createNickNameBtn;
    [SerializeField] private InputField _nickNameInputField;
    public static string InputFieldStr { get; private set; }


    private void Awake()
    {
        _createNickNameBtn.onClick.AddListener(OnClickCreateNickNameBtn);
        _nickNameInputField.onValueChanged.AddListener(OnValueChangedNickName);

        OnValueChangedNickName(string.Empty);
        OnClickCreateNickNameBtn();
    }

    private void OnDestroy()
    {
        _createNickNameBtn.onClick.RemoveAllListeners();
        _nickNameInputField.onValueChanged.RemoveAllListeners();
    }

    private void OnClickCreateNickNameBtn()
    {
        if(string.IsNullOrEmpty(_nickNameInputField.text))
        {
            Debug.LogWarning("닉네임이 비어있습니다.");
            return;
        }

        InputFieldStr = _nickNameInputField.text;
    }

    private void OnValueChangedNickName(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 3)
        {
            _createNickNameBtn.interactable = false;
            ColorUtility.TryParseHtmlString("#767676", out Color grayColor);
            _createNickNameBtn.image.color = grayColor;
            return;
        }

        ColorUtility.TryParseHtmlString("#2FB6FF", out Color blueColor);
        _createNickNameBtn.image.color = blueColor;
        _createNickNameBtn.interactable = true;
    }
}