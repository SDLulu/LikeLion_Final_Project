using Fusion;
using UnityEngine;

// 🎮 스펠렁키 플레이어 메인 컨트롤러
// 컴포넌트들을 조합하고 네트워크 동기화 담당
public class SpelunkyPlayerController : NetworkBehaviour, IBeforeUpdate
{
    [Header("Player State")]
    [Networked] public bool IsAlive { get; private set; } = true;
    
    // 🎮 로컬 변수들
    private float horizontalInput;
    private float verticalInput;
    private Vector2 mouseWorldPosition;
    private bool pickupPressed;
    private bool useItemPressed;
    
    // 📦 컴포넌트 참조들
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerAnimation playerAnimation;
    private PlayerItemController itemController;
    
    public override void Spawned()
    {
        // 컴포넌트들 자동으로 찾기
        groundCheck = GetComponent<PlayerGroundCheck>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        playerAnimation = GetComponent<PlayerAnimation>();
        itemController = GetComponent<PlayerItemController>();
        
        // 네트워크 물리 설정
        Runner.SetIsSimulated(Object, true);
        
        // 로컬 플레이어 설정
        if (Object.HasInputAuthority)
        {
            // 카메라 등 로컬 전용 설정
        }
        else
        {
            // 원격 플레이어 설정
            Object.RenderSource = RenderSource.Interpolated;
        }
        
        IsAlive = true;
    }
    
    // 입력 수집 (매 프레임)
    public void BeforeUpdate()
    {
        if (Object.HasInputAuthority && IsAlive)
        {
            // 방향키 입력
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
            
            // 마우스 월드 위치 계산
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            
            // 아이템 관련 입력
            pickupPressed = Input.GetKeyDown(KeyCode.Space) && IsDucking;
            useItemPressed = Input.GetMouseButtonDown(0);
        }
    }
    
    // 네트워크 고정 업데이트
    public override void FixedUpdateNetwork()
    {
        if (!IsAlive) return;
        
        // 입력 데이터 가져오기
        if (Runner.TryGetInputForPlayer<SpelunkyPlayerData>(Object.InputAuthority, out var input))
        {
            // 각 컴포넌트에 처리 위임
            movement?.HandleMovement(input);
            jump?.HandleJump(input);
            jump?.ApplyGravity();
            jump?.ClampVelocity();
            
            // 아이템 관련 처리
            HandleItemInteractions(input);
        }
    }
    
    // 렌더링 업데이트 (애니메이션 + 스프라이트 뒤집기)
    public override void Render()
    {
        playerAnimation?.UpdateAnimations();
        movement?.UpdateSpriteRendering();
    }
    
    // 입력 데이터 생성 (LocalInputPoller에서 호출)
    public SpelunkyPlayerData GetNetworkInputData()
    {
        SpelunkyPlayerData data = new SpelunkyPlayerData();
        
        if (IsAlive)
        {
            data.HorizontalInput = horizontalInput;
            data.VerticalInput = verticalInput;
            data.MouseWorldPosition = mouseWorldPosition;
            
            // 버튼 입력 설정
            data.NetworkButtons.Set(SpelunkyInputButtons.Jump, Input.GetKey(KeyCode.Space) && !IsDucking);
            data.NetworkButtons.Set(SpelunkyInputButtons.PickupItem, pickupPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.UseItem, useItemPressed);
        }
        
        return data;
    }
    
    // 🏺 아이템 상호작용 처리 (ItemController에 위임)
    private void HandleItemInteractions(SpelunkyPlayerData input)
    {
        itemController?.HandleItemInteractions(input);
    }
    
    // 📊 상태 접근 프로퍼티들 (다른 시스템에서 사용)
    public bool IsGrounded => groundCheck?.IsGrounded ?? false;
    public bool IsDucking => movement?.IsDucking ?? false;
    public bool IsFacingLeft => movement?.FacingLeft ?? false;
    public Vector2 Velocity => jump?.Velocity ?? Vector2.zero;
    public float CurrentSpeed => movement?.CurrentSpeed ?? 0f;
    public bool HasItem => itemController?.HasItem ?? false;
} 