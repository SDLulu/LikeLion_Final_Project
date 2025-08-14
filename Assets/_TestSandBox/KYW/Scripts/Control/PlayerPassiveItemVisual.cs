using Fusion;
using UnityEngine;
using DG.Tweening;

// 🎨 패시브 아이템 비주얼 효과 관리 컴포넌트
// 로켓과 날개의 비주얼 효과를 담당
public class PlayerPassiveItemVisual : NetworkBehaviour, ISoftReset
{
    [Header("Visual References")]
    [SerializeField] private Transform backTransform; // Back 오브젝트 (로켓/날개 부모)
    [SerializeField] private GameObject rocketVisual; // 로켓 비주얼 오브젝트
    [SerializeField] private GameObject rocketFlame; // 로켓 불꽃 효과
    [SerializeField] private GameObject leftWingVisual; // 왼쪽 날개 비주얼 오브젝트
    [SerializeField] private GameObject rightWingVisual; // 오른쪽 날개 비주얼 오브젝트
    [SerializeField] private Transform leftWingTransform; // 왼쪽 날개 Transform (애니메이션용)
    [SerializeField] private Transform rightWingTransform; // 오른쪽 날개 Transform (애니메이션용)
    
    [Header("Wings Animation Settings")]
    [SerializeField] private float wingsFlapDuration = 0.3f; // 날개 펄럭임 지속시간
    [SerializeField] private float wingsFlapAngle = 90f; // 날개 펄럭임 각도 (1/4바퀴)
    [SerializeField] private Ease wingsFlapEase = Ease.OutQuad; // 날개 펄럭임 이징
    
    // 참조 컴포넌트들
    private PlayerInventory playerInventory;
    private PlayerJump playerJump;
    
    // 날개 애니메이션 관련
    private Sequence leftWingFlapSequence;
    private Sequence rightWingFlapSequence;
    private Vector3 originalLeftWingRotation;
    private Vector3 originalRightWingRotation;
    
    public override void Spawned()
    {
        // 컴포넌트 참조 설정
        playerInventory = GetComponentInChildren<PlayerInventory>();
        playerJump = GetComponent<PlayerJump>();
        
        // 비주얼 컴포넌트들 찾기
        FindVisualComponents();
        
        // 필수 컴포넌트 검증
        if (playerInventory == null)
            Debug.LogError($"[{name}] PlayerInventory 컴포넌트를 찾을 수 없습니다!");
        if (playerJump == null)
            Debug.LogError($"[{name}] PlayerJump 컴포넌트를 찾을 수 없습니다!");
        
        // 초기 비주얼 상태 설정
        UpdateVisualEffects();
    }
    
    // 비주얼 효과 컴포넌트들 찾기
    private void FindVisualComponents()
    {
        // Back 오브젝트 찾기
        if (backTransform == null)
        {
            Transform back = transform.Find("Back");
            if (back != null)
            {
                backTransform = back;
                
                // 로켓 비주얼 찾기
                rocketVisual = back.Find("Rocket")?.gameObject;
                if (rocketVisual != null)
                {
                    rocketFlame = rocketVisual.transform.Find("Flame")?.gameObject;
                }
                
                // 날개 비주얼 찾기
                leftWingVisual = back.Find("LeftWing")?.gameObject;
                rightWingVisual = back.Find("RightWing")?.gameObject;
                
                if (leftWingVisual != null)
                {
                    leftWingTransform = leftWingVisual.transform;
                    // 원래 각도를 0으로 초기화 (안전한 시작점)
                    originalLeftWingRotation = Vector3.zero;
                    Debug.Log($"[{name}] 왼쪽 날개 원래 각도 설정: {originalLeftWingRotation}");
                }
                
                if (rightWingVisual != null)
                {
                    rightWingTransform = rightWingVisual.transform;
                    // 원래 각도를 0으로 초기화 (안전한 시작점)
                    originalRightWingRotation = Vector3.zero;
                    Debug.Log($"[{name}] 오른쪽 날개 원래 각도 설정: {originalRightWingRotation}");
                }
            }
        }
    }
    
    // 매 프레임 비주얼 효과 업데이트
    public void UpdateVisualEffects()
    {
        if (playerInventory == null) return;
        
        // 로켓 비주얼 효과
        if (playerInventory.hasRocket)
        {
            if (rocketVisual != null) rocketVisual.SetActive(true);
            if (rocketFlame != null) rocketFlame.SetActive(IsRocketThrusting());
        }
        else
        {
            if (rocketVisual != null) rocketVisual.SetActive(false);
            if (rocketFlame != null) rocketFlame.SetActive(false);
        }
        
        // 날개 비주얼 효과 (로켓 여부와 상관없이 보임)
        if (leftWingVisual != null) leftWingVisual.SetActive(playerInventory.hasWings);
        if (rightWingVisual != null) rightWingVisual.SetActive(playerInventory.hasWings);
    }
    
    // 로켓 추진 중인지 확인
    private bool IsRocketThrusting()
    {
        if (playerJump == null || !playerInventory.hasRocket) return false;
        
        // PlayerJump의 로켓 추진 상태를 확인
        // Networked 프로퍼티이므로 직접 접근 가능
        return playerJump.IsRocketThrusting;
    }
    
    // 날개 펄럭임 애니메이션 트리거
    public void TriggerWingsFlap()
    {
        if ((leftWingTransform == null && rightWingTransform == null) || !playerInventory.hasWings) return;
        
        // 기존 애니메이션 중지 및 날개 각도 리셋
        if (leftWingFlapSequence != null && leftWingFlapSequence.IsActive())
        {
            leftWingFlapSequence.Kill();
            // 애니메이션 중단 시 원래 각도로 리셋
            if (leftWingTransform != null)
            {
                leftWingTransform.localEulerAngles = originalLeftWingRotation;
            }
        }
        if (rightWingFlapSequence != null && rightWingFlapSequence.IsActive())
        {
            rightWingFlapSequence.Kill();
            // 애니메이션 중단 시 원래 각도로 리셋
            if (rightWingTransform != null)
            {
                rightWingTransform.localEulerAngles = originalRightWingRotation;
            }
        }
        
        // 왼쪽 날개 펄럭임 애니메이션 (반시계방향)
        if (leftWingTransform != null)
        {
            leftWingFlapSequence = DOTween.Sequence();
            
            // 반시계방향으로 펄럭임 각도만큼 회전 (상대값)
            leftWingFlapSequence.Append(leftWingTransform.DOLocalRotate(
                Vector3.forward * (-wingsFlapAngle), 
                wingsFlapDuration * 0.5f, 
                RotateMode.FastBeyond360
            ).SetRelative().SetEase(wingsFlapEase));
            
            // 다시 원래 각도로 돌아가기 (상대값)
            leftWingFlapSequence.Append(leftWingTransform.DOLocalRotate(
                Vector3.forward * wingsFlapAngle, 
                wingsFlapDuration * 0.5f, 
                RotateMode.FastBeyond360
            ).SetRelative().SetEase(wingsFlapEase));
            
            // 애니메이션 완료 후 시퀀스 정리
            leftWingFlapSequence.OnComplete(() => {
                leftWingFlapSequence = null;
            });
        }
        
        // 오른쪽 날개 펄럭임 애니메이션 (시계방향)
        if (rightWingTransform != null)
        {
            rightWingFlapSequence = DOTween.Sequence();
            
            // 시계방향으로 펄럭임 각도만큼 회전 (상대값)
            rightWingFlapSequence.Append(rightWingTransform.DOLocalRotate(
                Vector3.forward * wingsFlapAngle, 
                wingsFlapDuration * 0.5f, 
                RotateMode.FastBeyond360
            ).SetRelative().SetEase(wingsFlapEase));
            
            // 다시 원래 각도로 돌아가기 (상대값)
            rightWingFlapSequence.Append(rightWingTransform.DOLocalRotate(
                Vector3.forward * (-wingsFlapAngle), 
                wingsFlapDuration * 0.5f, 
                RotateMode.FastBeyond360
            ).SetRelative().SetEase(wingsFlapEase));
            
            // 애니메이션 완료 후 시퀀스 정리
            rightWingFlapSequence.OnComplete(() => {
                rightWingFlapSequence = null;
            });
        }
    }
    
    // PlayerJump의 상태 변화를 감지하여 날개 펄럭임 트리거
    private bool wasJumping = false;
    private int lastJumpCount = 0;
    
    public override void FixedUpdateNetwork()
    {
        UpdateVisualEffects();
        
        // 날개 펄럭임 감지 (로켓 여부와 상관없이)
        if (playerJump != null && playerInventory.hasWings)
        {
            // 점프 상태가 변경되었거나 점프 횟수가 증가했을 때 날개 펄럭임
            if ((!wasJumping && playerJump.IsJumping) || 
                (playerJump.CurrentJumpCount > lastJumpCount))
            {
                TriggerWingsFlap();
            }
            
            wasJumping = playerJump.IsJumping;
            lastJumpCount = playerJump.CurrentJumpCount;
        }
    }
    
    // 컴포넌트 파괴 시 정리
    private void OnDestroy()
    {
        if (leftWingFlapSequence != null && leftWingFlapSequence.IsActive())
        {
            leftWingFlapSequence.Kill();
        }
        if (rightWingFlapSequence != null && rightWingFlapSequence.IsActive())
        {
            rightWingFlapSequence.Kill();
        }
    }

    /// <summary>
    /// ISoftReset 구현: 모든 비주얼 이펙트 상태를 초기화하고 꺼둡니다.
    /// </summary>
    public void SoftReset()
    {
        if (rocketVisual != null) rocketVisual.SetActive(false);
        if (rocketFlame != null) rocketFlame.SetActive(false);
        if (leftWingVisual != null) leftWingVisual.SetActive(false);
        if (rightWingVisual != null) rightWingVisual.SetActive(false);
        if (leftWingFlapSequence != null && leftWingFlapSequence.IsActive()) leftWingFlapSequence.Kill();
        if (rightWingFlapSequence != null && rightWingFlapSequence.IsActive()) rightWingFlapSequence.Kill();
        if (leftWingTransform != null) leftWingTransform.localEulerAngles = originalLeftWingRotation;
        if (rightWingTransform != null) rightWingTransform.localEulerAngles = originalRightWingRotation;
    }
}
