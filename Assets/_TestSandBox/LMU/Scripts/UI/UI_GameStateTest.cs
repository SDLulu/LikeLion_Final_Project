using TMPro;
using UnityEngine;

public class UI_GameStateTest : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private RectTransform _testHolder;
    [SerializeField] private TMP_Text _stateText;
    [SerializeField] private TMP_Text _timerText;


    private void Awake()
    {
#if UNITY_EDITOR
        _testHolder.gameObject.SetActive(true);
#else
        _testHolder.gameObject.SetActive(false);
#endif
    }
    
}
