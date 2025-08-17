using Fusion;
using UnityEngine;

// 🏃 플레이어 이동 컴포넌트
// 좌우 이동, 덕킹 담당 (순수 로직만)
public class PlayerMovement : NetworkBehaviour, ISoftReset
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float duckMoveSpeed = 2.5f;
    [Networked] public float NormalizedSpeed { get; private set; }
    
    [Header("Passive Items - Speed")]
    [SerializeField] private float speedShoesMultiplier = 1.5f; // 이속신발 배율
    
    // 🌐 네트워크 동기화 상태
    [Networked] public bool IsDucking { get; private set; }
    [Networked] public bool IsLookingUp { get; private set; }
    [Networked] public bool IsFacingLeft { get; private set; }
    
    // 참조 컴포넌트들
    private PlayerGroundCheck groundCheck;
    private Rigidbody2D rb; // ⚠️ 순간이동 문제의 핵심 원인! 일반 Rigidbody2D 사용 중
    private PlayerClimbing climbing;
    private SpelunkyPlayerController playerController;
    private PlayerInventory playerInventory;
    private PlayerWallCheck wallCheck;

    public override void Spawned()
    {
        // 모든 컴포넌트 참조를 한 번에 설정
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        climbing = GetComponent<PlayerClimbing>();
        playerController = GetComponent<SpelunkyPlayerController>();
        playerInventory = GetComponentInChildren<PlayerInventory>();
        wallCheck = GetComponentInChildren<PlayerWallCheck>();
        
        // 필수 컴포넌트 검증
        if (rb == null)
            Debug.LogError($"[{name}] Rigidbody2D 컴포넌트를 찾을 수 없습니다!");
        if (groundCheck == null)
            Debug.LogError($"[{name}] PlayerGroundCheck 컴포넌트를 찾을 수 없습니다!");
        if (climbing == null)
            Debug.LogError($"[{name}] PlayerClimbing 컴포넌트를 찾을 수 없습니다!");
        if (playerController == null)
            Debug.LogError($"[{name}] SpelunkyPlayerController 컴포넌트를 찾을 수 없습니다!");
        if (playerInventory == null)
            Debug.LogError($"[{name}] PlayerInventory 컴포넌트를 찾을 수 없습니다!");
        if (wallCheck == null)
            Debug.LogError($"[{name}] PlayerWallCheck 컴포넌트를 찾을 수 없습니다!");
    }
    
    // 이동 관련 모든 처리를 통합한 메서드
    public void ProcessInput(SpelunkyPlayerInputData input)
    {
        // 상태 확인 - 이동 불가능한 상태면 처리하지 않음
        if (playerController.IsDead || playerController.IsStunned || playerController.IsHeld || playerController.IsThrown)
        {
            return;
        }
        
        // 덕킹 처리
        HandleDucking(input);
        
        // 이동 처리
        ProcessMovement(input);
        
        // 스프라이트 방향 전환 (네트워크 상태만)
        UpdateFacingDirection(input);

        // 정규화된 속도 업데이트
        NormalizedSpeed = Mathf.Abs(rb.linearVelocity.x) / moveSpeed;
    }
    
    private void HandleDucking(SpelunkyPlayerInputData input)
    {
        // 🎮 웅크리기 (아래키 + 땅에 있을 때)
        bool shouldDuck = input.VerticalInput < 0f && groundCheck.IsGrounded;
        
        // 🎮 위를 보기 (위키 + 땅에 있을 때)
        bool shouldLookUp = input.VerticalInput > 0f && groundCheck.IsGrounded;
        
        // 상태가 변경될 때만 업데이트 (깜빡임 방지)
        if (IsDucking != shouldDuck)
        {
            IsDucking = shouldDuck;
            // LookUp과 Ducking은 동시에 불가능
            if (shouldDuck) IsLookingUp = false;
        }
        
        if (IsLookingUp != shouldLookUp)
        {
            IsLookingUp = shouldLookUp;
            // LookUp과 Ducking은 동시에 불가능
            if (shouldLookUp) IsDucking = false;
        }
    }
    
    private void ProcessMovement(SpelunkyPlayerInputData input)
    {
        // 🎮 사다리 오르는 중에는 플레이어 입력에 의한 수평 이동만 금지 (중앙 정렬은 허용)
        if (climbing.IsClimbing)
        {
            // 🎯 사다리 중앙 정렬을 위해 기존 X 속도는 유지 (PlayerClimbing에서 조절)
            // 플레이어 입력에 의한 수평 이동만 막음
            return;
        }
        
        // 🎮 LookUp 중에는 수평 이동 제한
        if (IsLookingUp)
        {
            // 수평 속도만 0으로 설정 (수직 속도는 유지)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        
        // 🎮 웅크린 상태에 따라 속도 조절
        float currentMoveSpeed = IsDucking ? duckMoveSpeed : moveSpeed;
        
        // 이속신발 효과 적용
        if (playerInventory.hasSpeedShoes)
        {
            currentMoveSpeed *= speedShoesMultiplier;
        }
        
        // 벽에 붙어있을 때 해당 방향 입력 무시
        float adjustedHorizontalInput = input.HorizontalInput;
        if (wallCheck != null && !groundCheck.IsGrounded)
        {
            // 왼쪽 벽에 붙어있고 왼쪽으로 가려고 하면 입력 무시
            if (wallCheck.IsTouchingWallLeft && input.HorizontalInput < 0)
            {
                adjustedHorizontalInput = 0f;
            }
            // 오른쪽 벽에 붙어있고 오른쪽으로 가려고 하면 입력 무시
            else if (wallCheck.IsTouchingWallRight && input.HorizontalInput > 0)
            {
                adjustedHorizontalInput = 0f;
            }
        }
        
        float targetSpeed = adjustedHorizontalInput * currentMoveSpeed;
        
        if (adjustedHorizontalInput != 0)
        {
            rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }
    
    private void UpdateFacingDirection(SpelunkyPlayerInputData input)
    {
        // 네트워크 동기화되는 방향 상태 업데이트
        if (input.HorizontalInput != 0)
        {
            IsFacingLeft = input.HorizontalInput < 0;
        }
    }

    /// <summary>
    /// ISoftReset 구현: 이동/방향/입력 상태 초기화
    /// </summary>
    public void SoftReset()
    {
        if (HasStateAuthority == false)
        {
            return;
        }
        IsDucking = false;
        IsLookingUp = false;
        NormalizedSpeed = 0f;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
} 