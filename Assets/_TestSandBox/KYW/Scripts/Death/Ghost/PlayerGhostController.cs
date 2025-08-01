using Fusion;
using UnityEngine;

// 👻 유령 플레이어 컨트롤러
// 플레이어가 죽은 후 유령으로 조작하는 시스템
public class PlayerGhostController : NetworkBehaviour, IBeforeUpdate
{
    [Header("유령 이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float breathRange = 3f;
    
    // 네트워크 상태
    [Networked] private Vector3 moveDirection { get; set; }
    [Networked] private float rotationAngle { get; set; }
    [Networked] private bool isBreathing { get; set; }
    
    // 참조
    private NetworkObject originalPlayer;
    private PlayerInventory ghostInventory;
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    
    // 📎 분리된 컴포넌트들
    private GhostObjectPickup ghostPickup;
    private GhostObjectThrower ghostThrower;
    
    // 입력 변수들
    private float horizontalInput;
    private float verticalInput;
    private Vector2 mouseWorldPosition;
    private bool leftClickPressed;
    private bool rightClickPressed;
    
    public override void Spawned()
    {
        // 스프라이트 렌더러 찾기
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (spriteRenderer == null)
        {
            Debug.LogError($"[{name}] SpriteRenderer를 찾을 수 없습니다!");
            return;
        }
        
        // 초기 위치 저장
        startPosition = transform.position;
        
        // 초기 알파값 설정 (반투명)
        Color color = spriteRenderer.color;
        color.a = 0.7f;
        spriteRenderer.color = color;
        
        // 인벤토리 찾기
        ghostInventory = GetComponent<PlayerInventory>();
        if (ghostInventory == null)
        {
            ghostInventory = GetComponentInChildren<PlayerInventory>();
        }
        
        // 분리된 컴포넌트들 찾기
        ghostPickup = GetComponentInChildren<GhostObjectPickup>();
        ghostThrower = GetComponentInChildren<GhostObjectThrower>();
        
        if (ghostPickup == null)
        {
            Debug.LogError($"[{name}] GhostObjectPickup 컴포넌트를 찾을 수 없습니다!");
        }
        
        if (ghostThrower == null)
        {
            Debug.LogError($"[{name}] GhostObjectThrower 컴포넌트를 찾을 수 없습니다!");
        }
        
        Debug.Log($"[{name}] 유령 플레이어 컨트롤러 초기화 완료!");
    }
    
    // 🎮 입력 수집 (매 프레임)
    public void BeforeUpdate()
    {
        if (!Object.HasInputAuthority) return;
        
        // 방향키 입력
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        
        // 마우스 월드 위치 계산
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        
        // 클릭 입력
        leftClickPressed = Input.GetMouseButtonDown(0);
        rightClickPressed = Input.GetMouseButtonDown(1);
    }
    
    public override void FixedUpdateNetwork()
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 입력이 필요한 것들 (InputAuthority에서만)
        if (Runner.TryGetInputForPlayer<GhostInputData>(Object.InputAuthority, out var input))
        {
            ProcessMovement(input);
            ProcessRotation(input);
            ProcessBreathing(input);
            ProcessPickupAndThrow(input);
        }
    }
    
    // 🏃 유령 이동 (공중 부유)
    private void ProcessMovement(GhostInputData input)
    {
        Vector3 direction = new Vector3(input.HorizontalInput, input.VerticalInput, 0);
        Vector3 newPosition = transform.position + direction * moveSpeed * Runner.DeltaTime;
        
        // 물체 통과 가능 (Collider2D 없음)
        transform.position = newPosition;
        
        // 들고 있는 아이템도 함께 이동
        if (ghostInventory?.CurrentHeldObject != null)
        {
            ghostInventory.CurrentHeldObject.transform.position = newPosition;
        }
    }
    
    // 🔄 회전 (마우스 방향)
    private void ProcessRotation(GhostInputData input)
    {
        Vector2 direction = input.MouseWorldPosition - (Vector2)transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    // 💨 입김 (좌클릭)
    private void ProcessBreathing(GhostInputData input)
    {
        if (input.NetworkButtons.IsSet(GhostInputButtons.LeftClick))
        {
            // 입김 이펙트 생성
            SpawnBreathEffect();
            
            // 범위 내 오브젝트에 영향
            var colliders = Physics2D.OverlapCircleAll(transform.position, breathRange);
            foreach (var col in colliders)
            {
                HandleBreathInteraction(col.gameObject);
            }
            
            Debug.Log($"[{name}] 유령 입김!");
        }
    }
    
    // 🤲 들기/던지기 처리 (분리된 컴포넌트들 사용)
    private void ProcessPickupAndThrow(GhostInputData input)
    {
        // 들고 있는 것이 없으면 픽업 시도
        if (ghostInventory?.CurrentHeldObject == null)
        {
            ghostPickup?.ProcessInput(input);
        }
        // 들고 있는 것이 있으면 던지기 시도
        else
        {
            ghostThrower?.ProcessInput(input);
        }
    }
    
    // 💨 입김 이펙트 생성
    private void SpawnBreathEffect()
    {
        // TODO: 입김 이펙트 프리팹 스폰
        Debug.Log($"[{name}] 입김 이펙트 생성!");
    }
    
    // 💨 입김 상호작용 처리
    private void HandleBreathInteraction(GameObject obj)
    {
        // TODO: 오브젝트 타입에 따른 반응 처리
        // 예: 불꽃 끄기, 물건 밀기 등
        Debug.Log($"[{name}] {obj.name}와 입김 상호작용!");
    }
    
    // 🔗 원래 플레이어 설정
    public void SetOriginalPlayer(NetworkObject player)
    {
        originalPlayer = player;
        Debug.Log($"[{name}] 원래 플레이어 설정: {player?.name ?? "null"}");
    }
    
    // 🔄 부활 시 유령 제거
    public void DespawnGhost()
    {
        if (HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
    
    // 📡 입력 데이터 생성 (GhostLocalInputPoller에서 호출)
    public GhostInputData GetNetworkInputData()
    {
        GhostInputData data = new GhostInputData();
        
        if (Object.HasInputAuthority)
        {
            data.HorizontalInput = horizontalInput;
            data.VerticalInput = verticalInput;
            data.MouseWorldPosition = mouseWorldPosition;
            
            // 🔘 버튼 입력 설정
            data.NetworkButtons.Set(GhostInputButtons.LeftClick, leftClickPressed);
            data.NetworkButtons.Set(GhostInputButtons.RightClick, rightClickPressed);
        }
        
        return data;
    }
    
    // 🔍 게임 로직에 필요한 속성 (PlayerInventory에서 가져옴)
    public GameObject CurrentHeldObject => ghostInventory != null ? ghostInventory.CurrentHeldObject : null; // 손에 든 오브젝트
    public bool HasHeldObject => CurrentHeldObject != null; // 손에 든 것 보유 여부
} 