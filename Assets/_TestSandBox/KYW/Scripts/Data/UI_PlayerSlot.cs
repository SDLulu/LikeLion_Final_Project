using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerSlot : MonoBehaviour
{
    [Header("돈")]
    [SerializeField] private TMP_Text _playerMoney;
    
    [Header("체력 UI")]
    [SerializeField] private TMP_Text _playerHealthText;
    [SerializeField] private Image _playerHealthIcon;
    [SerializeField] private Sprite _normalHealthIcon; // 일반 체력 아이콘
    [SerializeField] private Sprite _deadHealthIcon;   // 사망 시 체력 아이콘
    
    [Header("플레이어 스킨 UI")]
    [SerializeField] private Image _playerSkinImage; // 플레이어 스킨 이미지
    [SerializeField] private Sprite _defaultSkinSprite; // 기본 스킨 스프라이트 (스킨 로드 실패 시 사용)
    
    [Header("패시브 아이템 아이콘들")]
    [SerializeField] private GameObject _rocketIcon;
    [SerializeField] private GameObject _wingsIcon;
    [SerializeField] private GameObject _speedShoesIcon;
    [SerializeField] private GameObject _jumpShoesIcon;
    [SerializeField] private GameObject _magnetIcon;
    [SerializeField] private GameObject _headsetIcon;
    [SerializeField] private GameObject _sunglassesIcon;

    // 같은 부모(플레이어 프리팹) 아래의 컴포넌트들
    private PlayerInventory _playerInventory;
    private PlayerHealth _playerHealth;
    private PlayerAppearance _playerAppearance; // 플레이어 외형 컴포넌트 추가
    private bool _isInitialized = false;

    private void Awake()
    {
        // 바인딩은 외부에서 명시적으로 수행합니다.
    }

    private void Start()
    {
        // 초기화
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (_isInitialized)
        {
            return;
        }

        // 컴포넌트들이 모두 있는지 확인
        if (_playerInventory == null || _playerHealth == null || _playerAppearance == null)
        {
            Debug.LogWarning("📱 UI_PlayerSlot: 필요한 컴포넌트가 없습니다!");
            return;
        }

        // 이벤트 구독
        SubscribeToEvents();
        
        // 초기 UI 업데이트
        UpdateUI();
        
        // PlayerSlotUIManager에 등록
        // PlayerSlotUIManager.Inst.RegisterPlayerUI(this);
        
        _isInitialized = true;
        Debug.Log($"📱 UI_PlayerSlot: 초기화 완료");
    }

    /// <summary>
    /// 이 UI 슬롯이 참조할 실제 플레이어 오브젝트를 바인딩합니다.
    /// </summary>
    /// <param name="ownerRoot">플레이어의 루트 GameObject</param>
    public void BindOwner(GameObject ownerRoot)
    {
        if (ownerRoot == null)
        {
            Debug.LogWarning("📱 UI_PlayerSlot: ownerRoot 가 null 입니다.");
            return;
        }

        if (_isInitialized)
        {
            return;
        }

        _playerInventory = ownerRoot.GetComponentInChildren<PlayerInventory>();
        _playerHealth = ownerRoot.GetComponentInChildren<PlayerHealth>();
        _playerAppearance = ownerRoot.GetComponentInChildren<PlayerAppearance>(); // 플레이어 외형 컴포넌트 바인딩

        if (_playerInventory == null || _playerHealth == null || _playerAppearance == null)
        {
            Debug.LogWarning("📱 UI_PlayerSlot: 필요한 컴포넌트를 ownerRoot 에서 찾지 못했습니다.");
            return;
        }

        InitializeUI();
    }

    /// <summary>
    /// 이 UI 슬롯이 참조하는 실제 플레이어 오브젝트가 유효한지 검사합니다.
    /// </summary>
    public bool IsOwnerValid()
    {
        if (this == null)
        {
            return false;
        }

        if (gameObject == null)
        {
            return false;
        }

        if (_playerInventory == null)
        {
            return false;
        }

        if (_playerHealth == null)
        {
            return false;
        }

        if (_playerAppearance == null)
        {
            return false;
        }

        return true;
    }

    private void SubscribeToEvents()
    {
        if (_playerInventory != null)
        {
            _playerInventory.OnInventoryDataChanged += OnInventoryDataChanged;
        }
        
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChangedEvent += OnHealthChanged;
        }
        
        // 플레이어 외형 변경 이벤트 구독
        if (_playerAppearance != null)
        {
            _playerAppearance.OnSkinChanged += OnSkinChanged;
        }
    }


    public void UpdateData()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        UpdateMoneyUI();
        UpdateHealthUI();
        UpdateSkinUI(); // 스킨 UI 업데이트 추가
        UpdateItemIcons();
    }

    private void UpdateMoneyUI()
    {
        if (_playerMoney != null && _playerInventory != null)
        {
            _playerMoney.text = $"${_playerInventory.CurrentMoney}";
        }
    }

    private void UpdateHealthUI()
    {
        if (_playerHealthText != null && _playerHealth != null)
        {
            _playerHealthText.text = $"{_playerHealth.Health}";
        }

        if (_playerHealthIcon != null && _playerHealth != null)
        {
            // 체력이 0일 때 아이콘 이미지 변경
            if (_playerHealth.Health <= 0)
            {
                if (_deadHealthIcon != null)
                {
                    _playerHealthIcon.sprite = _deadHealthIcon;
                }
            }
            else
            {
                if (_normalHealthIcon != null)
                {
                    _playerHealthIcon.sprite = _normalHealthIcon;
                }
            }
            
            // 체력에 따른 아이콘 크기 비례 변경
            float scaleMultiplier;
            if (_playerHealth.Health <= 0)
            {
                scaleMultiplier = 0.5f; // 최소 크기
            }
            else if (_playerHealth.Health >= 10)
            {
                scaleMultiplier = 1.5f; // 최대 크기
            }
            else
            {
                // 0~10 사이에서 선형 보간
                scaleMultiplier = 0.5f + (_playerHealth.Health / 10f) * 1.0f;
            }
            
            // 아이콘 크기 적용
            _playerHealthIcon.transform.localScale = Vector3.one * scaleMultiplier;
        }
    }

    /// <summary>
    /// 플레이어 스킨 UI를 업데이트합니다.
    /// </summary>
    private void UpdateSkinUI()
    {
        if (_playerAppearance == null) return;

        string skinKey = _playerAppearance.SkinKey.ToString();
        
        // 스킨 이미지 업데이트
        if (_playerSkinImage != null)
        {
            Sprite skinSprite = GetSkinSprite(skinKey);
            if (skinSprite != null)
            {
                _playerSkinImage.sprite = skinSprite;
            }
            else if (_defaultSkinSprite != null)
            {
                _playerSkinImage.sprite = _defaultSkinSprite;
                Debug.LogWarning($"📱 UI_PlayerSlot: 스킨 '{skinKey}'의 이미지를 찾을 수 없어 기본 이미지를 사용합니다.");
            }
        }
    }

    /// <summary>
    /// 스킨 키에 해당하는 스프라이트를 가져옵니다.
    /// </summary>
    /// <param name="skinKey">스킨 키 (예: "Penguin", "Rabbit")</param>
    /// <returns>해당하는 스프라이트, 없으면 null</returns>
    private Sprite GetSkinSprite(string skinKey)
    {
        if (string.IsNullOrEmpty(skinKey)) return null;

        // PlayerAppearance를 통해 스프라이트 가져오기
        if (_playerAppearance != null)
        {
            return _playerAppearance.GetSkinSprite(skinKey);
        }

        return null;
    }

    private void UpdateItemIcons()
    {
        if (_playerInventory == null)
        {
            return;
        }

        // 각 패시브 아이템 아이콘 활성화/비활성화
        if (_rocketIcon != null)
        {
            _rocketIcon.SetActive(_playerInventory.hasRocket);
        }
        if (_wingsIcon != null)
        {
            _wingsIcon.SetActive(_playerInventory.hasWings);
        }
        if (_speedShoesIcon != null)
        {
            _speedShoesIcon.SetActive(_playerInventory.hasSpeedShoes);
        }
        if (_jumpShoesIcon != null)
        {
            _jumpShoesIcon.SetActive(_playerInventory.hasJumpShoes);
        }
        if (_magnetIcon != null)
        {
            _magnetIcon.SetActive(_playerInventory.hasMagnet);
        }
        if (_headsetIcon != null)
        {
            _headsetIcon.SetActive(_playerInventory.hasHeadset);
        }
        if (_sunglassesIcon != null)
        {
            _sunglassesIcon.SetActive(_playerInventory.hasSunglasses);
        }
    }

    // --- Event Handlers ---
    private void OnInventoryDataChanged()
    {
        UpdateMoneyUI();
        UpdateItemIcons();
    }

    private void OnHealthChanged()
    {
        UpdateHealthUI();
    }
    
    private void OnSkinChanged(string newSkinKey)
    {
        UpdateSkinUI();
    }

    // --- Unity Lifecycle ---
    private void OnDestroy()
    {
        // PlayerSlotUIManager에서 제거
        // PlayerSlotUIManager.Inst?.UnregisterPlayerUI(this);
        
        // 이벤트 구독 해제
        if (_playerInventory != null)
        {
            _playerInventory.OnInventoryDataChanged -= OnInventoryDataChanged;
        }
        
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChangedEvent -= OnHealthChanged;
        }
        
        if (_playerAppearance != null)
        {
            _playerAppearance.OnSkinChanged -= OnSkinChanged;
        }

        // 참조 정리
        _playerInventory = null;
        _playerHealth = null;
        _playerAppearance = null;
        _isInitialized = false;
    }
}
