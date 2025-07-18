using Fusion;
using UnityEngine;
using UnityEngine.UIElements;

// 아이템 던지기 담당 컴포넌트
// 📍 위치: Hand 하위 오브젝트 (Player > Hand > PlayerItemThrower)
// 🎯 목적: 우클릭으로 현재 들고 있는 아이템을 마우스 방향으로 던지기
public class PlayerItemThrower : NetworkBehaviour
{
    [Header("Throw Settings")]
    [SerializeField] private float throwForce = 10f;      // 💪 던지기 힘 (Rigidbody2D.velocity에 적용)
    [SerializeField] private float throwOffset = 0.5f;    // 📏 던지기 시작 위치 오프셋 (플레이어로부터 얼마나 떨어뜨릴지)
    
    // 📎 참조할 다른 컴포넌트
    private PlayerItemPickup itemPickup;  // 📦 아이템 보유 상태 확인용

    // 🌐 네트워크 동기화: 버튼 래칭용
    [Networked]
    private NetworkButtons ButtonsPrevious { get; set; }  // 🎮 이전 프레임 버튼 상태 (클릭 래칭용)
    
    // 🚀 NetworkBehaviour 생성 시 호출 (모든 클라이언트에서 실행)
    public override void Spawned()
    {
        itemPickup = GetComponent<PlayerItemPickup>();  // 📎 같은 오브젝트의 PlayerItemPickup 참조
    }
    
    // 🎮 입력 처리 (SpelunkyPlayerController에서 호출)
    // 👉 InputAuthority(로컬 플레이어)에서만 호출됨
    public void ProcessInput(SpelunkyPlayerData input)
    {
        // 🎯 버튼 래칭: 이전 프레임과 비교해서 새로 눌린 버튼만 감지
        // 👉 이렇게 하면 "한 번 우클릭"이 여러 프레임에 걸쳐 씹히지 않음
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;  // 🔄 다음 프레임을 위해 현재 상태 저장
        
        // 🎮 아이템 던지기 조건: 우클릭 + 아이템 보유 중
        if (pressed.IsSet(SpelunkyInputButtons.ThrowItem) && itemPickup.HasItem)
        {
            // 📡 모든 클라이언트에게 던지기 명령 RPC 전송
            // 👉 InputAuthority → All Clients로 전송 (동기화된 던지기)
            ThrowItemRpc(input.MouseWorldPosition);
        }
    }
    
    // 📡 RPC: InputAuthority → All Clients로 던지기 명령 전송
    // 👉 모든 클라이언트에서 동시에 던지기 실행 (시각적 동기화)
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void ThrowItemRpc(Vector2 mouseWorldPosition)
    {
        // ✅ 아이템 보유 상태 재확인 (RPC 지연으로 인한 상태 변화 방지)
        if (!itemPickup.HasItem) return;
        
        // 📏 던지기 방향 계산
        Vector2 playerPosition = transform.parent != null ? transform.parent.position : transform.position;
        Vector2 throwDirection = (mouseWorldPosition - playerPosition).normalized;  // 🎯 마우스 방향으로 정규화
        
        ThrowItem(throwDirection);  // 🚀 실제 던지기 실행
    }
    
    // 🚀 실제 아이템 던지기 처리
    // 👉 모든 클라이언트에서 실행되지만, 물리 적용은 StateAuthority에서만
    private void ThrowItem(Vector2 direction)
    {
        GameObject itemToThrow = itemPickup.CurrentItem;  // 📦 현재 들고 있는 아이템
        if (itemToThrow == null) return;
        
        // 🗑️ 아이템 소유권 해제
        itemPickup.ClearItem();  // 📦 PlayerItemPickup에서 아이템 참조 제거

        itemToThrow.transform.SetParent(null);  // 🏠 부모 관계 해제 (Hand에서 분리)
        
        // ⚡ 물리 적용 (StateAuthority에서만 의미 있음)
        EnableItemPhysics(itemToThrow, direction);

    }

    
    // ⚡ 아이템 물리 시뮬레이션 활성화 (던진 후)
    private void EnableItemPhysics(GameObject item, Vector2 throwDirection)
    {
        var rigidbody = item.GetComponent<Rigidbody2D>();
        var netObj = item.GetComponent<NetworkObject>();
        
        // 🌪️ StateAuthority에서만 물리값 변경
        // 👉 Host가 물리 계산 → 모든 클라이언트에게 동기화
        if (rigidbody != null && netObj != null && netObj.HasStateAuthority)
        {
            // 🔄 현재 Transform 각도를 Rigidbody에 먼저 설정 (Unity 공식 권장)
            float currentZAngle = item.transform.eulerAngles.z;
            rigidbody.rotation = currentZAngle;  // 물리 엔진에 현재 각도 전달
            
            rigidbody.simulated = true;        // ▶️ 물리 시뮬레이션 활성화
            rigidbody.isKinematic = false;     // 🔓 키네마틱 모드 해제 (외력 영향 받음)
            rigidbody.linearVelocity = throwDirection * throwForce;  // 💨 던지기 속도 적용
            rigidbody.angularVelocity = 0f;    // 🔄 회전 속도 0 (직선으로 날아감)
        }
        
        // 🔵 콜라이더는 모든 클라이언트에서 활성화
        // 👉 시각적 충돌 감지 및 트리거 이벤트를 위해
        var collider = item.GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = false;  // ✅ 충돌 감지 활성화
    }
} 