using UnityEngine;

// 🎭 플레이어의 시각적 요소(애니메이션, 방향, 스케일)를 관리하는 컨트롤러
// 📦 MonoBehaviour: 순수 Unity 컴포넌트 (네트워크 동기화 불필요)
//
// 🔑 중요한 동기화 개념:
// ❌ 애니메이션 자체는 동기화 안됨 (MonoBehaviour라서)
// ✅ 애니메이션을 결정하는 데이터가 동기화됨 (PlayerController에서 전달)
//
// 🎮 동기화 원리:
// 1️⃣ PlayerController (NetworkBehaviour): 네트워크 데이터 관리
//     - rigid.linearVelocity → 🌐 자동 동기화
//     - IsHoldingShootingKey → 🌐 자동 동기화
//
// 2️⃣ PlayerVisualController (MonoBehaviour): 로컬 애니메이션 실행
//     - RendererVisuals(velocity, isShooting) 호출받음
//     - 동일한 데이터로 동일한 애니메이션 실행
//
// 🍳 비유: "같은 재료로 각자 요리하면 같은 음식이 나온다"
//     - 🥕 재료 = 동기화된 데이터 (velocity, shooting)
//     - 👨‍🍳 요리 = 각 클라이언트의 애니메이션 시스템  
//     - 🍽️ 결과 = 모든 클라이언트에서 동일한 애니메이션
public class PlayerVisualController : MonoBehaviour
{
    // 🎨 시각적 요소들
    [SerializeField] private Animator animator;        // 플레이어 애니메이션
    [SerializeField] private Transform pivotGunTr;     // 무기 피벗
    [SerializeField] private Transform canvasTr;       // UI 캔버스

    // 🎬 애니메이션 파라미터 해시 (성능 최적화)
    // 문자열 대신 해시값을 사용해서 더 빠른 접근
    private readonly int isMovingHash = Animator.StringToHash("IsWalking");   // 걷기 애니메이션
    private readonly int isShootingHash = Animator.StringToHash("IsShooting"); // 사격 애니메이션
    
    // 🔄 상태 변수들
    private bool isFacingRight = true; // 플레이어가 오른쪽을 보고 있는지
    private bool init;                 // 초기화 완료 여부
    
    // 📏 원본 스케일 값들 (방향 변경 시 참조용)
    private Vector3 originalPlayerScale; // 플레이어 원본 크기
    private Vector3 originalCanvasScale; // 캔버스 원본 크기  
    private Vector3 originalGunPivotScale; // 무기 피벗 원본 크기

    // 🎬 게임 시작 시 한 번 호출
    private void Start()
    {
        // 📏 모든 오브젝트의 원본 스케일 저장
        originalPlayerScale = this.transform.localScale;
        originalCanvasScale = canvasTr.transform.localScale;
        originalGunPivotScale = pivotGunTr.transform.localScale;

        // 🎯 사격 애니메이션 레이어 활성화
        const int SHOOTING_LAYER_INDEX = 1;
        animator.SetLayerWeight(SHOOTING_LAYER_INDEX, 1);
        
        // ✅ 초기화 완료 플래그
        init = true;
    }

    // 💀 사망 애니메이션을 트리거하는 함수
    public void TriggerDieAnimation()
    {
        const string TRIGGER = "Die";
        animator.SetTrigger(TRIGGER);
    }
    
    // 🌟 리스폰 애니메이션을 트리거하는 함수
    public void TriggerRespawnAnimation()
    {
        const string TRIGGER = "Respawn";
        animator.SetTrigger(TRIGGER);
    }
    
    // 🎨 시각적 렌더링 업데이트 (애니메이션 상태 제어)
    // Render()에서 호출되어 매 프레임 실행
    public void RendererVisuals(Vector2 velocity, bool isShooting)
    {
        // ❌ 초기화가 안됐으면 실행하지 않음
        if (!init) return;
        
        // 🏃 이동 중인지 판단 (약간의 여유값으로 미세한 떨림 방지)
        var isMoving = velocity.x > 0.1f || velocity.x < -0.1f;
        
        // 🎬 애니메이션 파라미터 설정
        animator.SetBool(isMovingHash, isMoving);     // 걷기 애니메이션 ON/OFF
        animator.SetBool(isShootingHash, isShooting); // 사격 애니메이션 ON/OFF
    }

    // 🔄 플레이어 방향에 따른 스케일 업데이트
    // FixedUpdateNetwork()에서 호출되어 네트워크 틱마다 실행
    public void UpdateScaleTransforms(Vector2 velocity)
    {
        // ❌ 초기화가 안됐으면 실행하지 않음
        if (!init) return;
        
        // 🔄 이동 방향에 따른 바라보는 방향 결정
        if (velocity.x > 0.1f)
        {
            isFacingRight = true;  // 오른쪽으로 이동 = 오른쪽 바라보기
        }
        else if (velocity.x < -0.1f)
        {
            isFacingRight = false; // 왼쪽으로 이동 = 왼쪽 바라보기
        }
        // 📝 움직임이 없으면 이전 방향 유지

        // 🔄 모든 오브젝트의 스케일을 방향에 맞게 조정
        SetObjectLocalScaleBasedOnDir(gameObject, originalPlayerScale);           // 플레이어 본체
        SetObjectLocalScaleBasedOnDir(canvasTr.gameObject, originalCanvasScale);  // UI 캔버스
        SetObjectLocalScaleBasedOnDir(pivotGunTr.gameObject, originalGunPivotScale); // 무기 피벗
    }

    // 🔄 오브젝트의 방향을 설정하는 헬퍼 함수
    private void SetObjectLocalScaleBasedOnDir(GameObject obj, Vector3 originalScale)
    {
        // 📏 Y, Z 값은 원본 그대로 유지
        var yValue = originalScale.y;
        var zValue = originalScale.z;
        
        // 🔄 X 값만 방향에 따라 조정
        // 오른쪽: 원본 그대로 (+), 왼쪽: 뒤집기 (-)
        var xValue = isFacingRight ? originalScale.x : -originalScale.x;
        
        // ✨ 새로운 스케일 적용
        obj.transform.localScale = new Vector3(xValue, yValue, zValue);
    }
}
