using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerSlot : MonoBehaviour
{
    [Header("플레이어 기본 정보")]
    [SerializeField] private TMP_Text _playerNickName;
    [SerializeField] private TMP_Text _playerMoney;
    [SerializeField] private Image _playerFaceIcon;
    
    [Header("체력 UI")]
    [SerializeField] private TMP_Text _playerHealthText;
    [SerializeField] private Image _playerHealthIcon;
    [SerializeField] private Sprite _normalHealthIcon; // 일반 체력 아이콘
    [SerializeField] private Sprite _deadHealthIcon;   // 사망 시 체력 아이콘
    
    [Header("패시브 아이템 아이콘들")]
    [SerializeField] private GameObject _rocketIcon;
    [SerializeField] private GameObject _wingsIcon;
    [SerializeField] private GameObject _speedShoesIcon;
    [SerializeField] private GameObject _jumpShoesIcon;
    [SerializeField] private GameObject _magnetIcon;
    [SerializeField] private GameObject _headsetIcon;
    [SerializeField] private GameObject _sunglassesIcon;

    // 현재 연결된 플레이어 데이터
    private PlayerData _currentPlayerData;
    private PlayerInventory _currentInventory;
    private PlayerHealth _currentHealth;
    private bool _isInitialized = false;

    // --- Public API ---
    public void UpdateData(PlayerData playerData)
    {
        // 플레이어가 변경된 경우에만 초기화
        if (_currentPlayerData != playerData)
        {
            InitializePlayerData(playerData);
        }
        
        // 닉네임은 매번 업데이트 (변경될 수 있으므로) 이부분은 안에 넣어도 되긴할듯
        _playerNickName.text = playerData.NickName;
    }
    
    // --- Initialization ---
    private void InitializePlayerData(PlayerData playerData)
    {
        // 기존 이벤트 구독 해제
        UnsubscribeFromEvents();
        
        _currentPlayerData = playerData;
        
        // 플레이어 인벤토리와 체력 정보 가져오기 (한 번만)
        _currentInventory = playerData.GetComponentInChildren<PlayerInventory>();
        _currentHealth = playerData.GetComponentInChildren<PlayerHealth>();
        
        // 초기 UI 업데이트
        if (_currentInventory != null)
        {
            UpdateMoneyUI(_currentInventory.CurrentMoney);
            UpdatePassiveItemsUI(_currentInventory);
            
            // 이벤트 구독 (변경될 때만 업데이트)
            _currentInventory.OnInventoryDataChanged += OnInventoryDataChanged;
        }
        
        if (_currentHealth != null)
        {
            // 초기 1회 갱신 후 이벤트 구독
            UpdateHealthUI(_currentHealth.Health);
            _currentHealth.OnHealthChangedEvent += OnHealthChanged;
        }
        
        // 플레이어 얼굴 아이콘 업데이트
        UpdatePlayerFaceIcon(playerData);
        
        _isInitialized = true;
    }
    
    // --- Event Handlers ---
    private void OnInventoryDataChanged()
    {
        // 인벤토리 데이터가 변경될 때만 UI 업데이트
        if (_currentInventory != null)
        {
            UpdateMoneyUI(_currentInventory.CurrentMoney);
            UpdatePassiveItemsUI(_currentInventory);
        }
    }
    private void OnHealthChanged()
    {
        if (_currentHealth != null)
        {
            UpdateHealthUI(_currentHealth.Health);
        }
    }
    
    // --- Cleanup / Unsubscribe ---
    private void UnsubscribeFromEvents()
    {
        if (_currentInventory != null)
        {
            _currentInventory.OnInventoryDataChanged -= OnInventoryDataChanged;
        }
        if (_currentHealth != null)
        {
            _currentHealth.OnHealthChangedEvent -= OnHealthChanged;
        }
    }
    
    // --- Unity Lifecycle ---
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        _currentPlayerData = null;
        _currentInventory = null;
        _currentHealth = null;
        _isInitialized = false;
    }
    
    // --- UI Update Helpers ---
    private void UpdateMoneyUI(int money)
    {
        if (_playerMoney != null)
            _playerMoney.text = $"${money}";
    }
    
    private void UpdateHealthUI(int currentHealth)
    {
        if (_playerHealthText != null)
            _playerHealthText.text = $"{currentHealth}";

        if (_playerHealthIcon != null)
        {
            // 체력이 0일 때 아이콘 이미지 변경
            if (currentHealth <= 0)
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
            // 체력 0일 때 0.5배, 체력 5일 때 1배, 체력 10일 때 1.5배
            float scaleMultiplier;
            if (currentHealth <= 0)
            {
                scaleMultiplier = 0.5f; // 최소 크기
            }
            else if (currentHealth >= 10)
            {
                scaleMultiplier = 1.5f; // 최대 크기
            }
            else
            {
                // 0~10 사이에서 선형 보간
                scaleMultiplier = 0.5f + (currentHealth / 10f) * 1.0f;
            }
            
            // 아이콘 크기 적용
            _playerHealthIcon.transform.localScale = Vector3.one * scaleMultiplier;
        }
    }
    
    private void UpdatePassiveItemsUI(PlayerInventory inventory)
    {
        // 각 패시브 아이템 아이콘 활성화/비활성화
        if (_rocketIcon != null) _rocketIcon.SetActive(inventory.hasRocket);
        if (_wingsIcon != null) _wingsIcon.SetActive(inventory.hasWings);
        if (_speedShoesIcon != null) _speedShoesIcon.SetActive(inventory.hasSpeedShoes);
        if (_jumpShoesIcon != null) _jumpShoesIcon.SetActive(inventory.hasJumpShoes);
        if (_magnetIcon != null) _magnetIcon.SetActive(inventory.hasMagnet);
        if (_headsetIcon != null) _headsetIcon.SetActive(inventory.hasHeadset);
        if (_sunglassesIcon != null) _sunglassesIcon.SetActive(inventory.hasSunglasses);
    }

    //스킨데이터는 제대로 짠거아니니까 나중에 수정해야함
    //스킨데이터는 제대로 짠거아니니까 나중에 수정해야함
    //스킨데이터는 제대로 짠거아니니까 나중에 수정해야함
    private void UpdatePlayerFaceIcon(PlayerData playerData)
    {
        if (_playerFaceIcon != null && !string.IsNullOrEmpty(playerData.SkinPath))
        {
            // 플레이어 스킨 경로에서 스프라이트 로드
            Sprite faceSprite = Resources.Load<Sprite>(playerData.SkinPath);
            if (faceSprite != null)
            {
                _playerFaceIcon.sprite = faceSprite;
            }
        }
    }
}
