using Fusion;
using UnityEngine;
using UnityEngine.UIElements;

// 아이템 던지기 담당 컴포넌트
// 📍 위치: Hand 하위 오브젝트 (Player > Hand > PlayerItemThrower)
// 🎯 목적: 우클릭으로 현재 들고 있는 아이템을 마우스 방향으로 던지기
public class PlayerObjectThrower : NetworkBehaviour
{
    [Header("Throw Settings")]
    [SerializeField] private float throwForce = 10f;      // 💪 던지기 힘 (Rigidbody2D.velocity에 적용)
    [SerializeField] private float throwOffset = 0.5f;    // 📏 던지기 시작 위치 오프셋 (플레이어로부터 얼마나 떨어뜨릴지)
    
    // 📎 참조할 다른 컴포넌트
    // private PlayerItemPickup itemPickup;  // 📦 아이템 보유 상태 확인용 (삭제)
    private PlayerInventory inventory; // 인벤토리 참조

    // 🌐 네트워크 동기화: 버튼 래칭용
    [Networked]
    private NetworkButtons ButtonsPrevious { get; set; }  // 🎮 이전 프레임 버튼 상태 (클릭 래칭용)
    
    // 🚀 NetworkBehaviour 생성 시 호출 (모든 클라이언트에서 실행)
    public override void Spawned()
    {
        inventory = GetComponentInParent<PlayerInventory>(); // 인벤토리 캐싱 (부모에서 찾음)
    }
    
    // 🎮 입력 처리 (SpelunkyPlayerController에서 호출)
    // 👉 InputAuthority(로컬 플레이어)에서만 호출됨
    public void ProcessInput(SpelunkyPlayerData input)
    {
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;

        // 🎮 오브젝트 던지기 조건: 우클릭 + 손에 든 오브젝트 존재
        if (pressed.IsSet(SpelunkyInputButtons.ThrowItem) && inventory != null && inventory.CurrentHeldObject != null)
        {
            ThrowObjectRpc(input.MouseWorldPosition);
        }
    }
    
    // 📡 RPC: InputAuthority → StateAuthority로 던지기 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void ThrowObjectRpc(Vector2 mouseWorldPosition)
    {
        var obj = inventory.CurrentHeldObject;
        if (obj == null) return;

        Vector2 playerPosition = transform.parent != null ? transform.parent.position : transform.position;
        Vector2 throwDirection = (mouseWorldPosition - playerPosition).normalized;

        ThrowObject(obj, throwDirection);
    }

    // 🚀 실제 오브젝트 던지기 처리
    private void ThrowObject(GameObject obj, Vector2 direction)
    {
        var netObj = obj.GetComponent<NetworkObject>();
        int layer = obj.layer;
        
        if (Object.HasStateAuthority)
        {
            // 🎮 InputAuthority 해제 (아이템만)
            if (layer != LayerMask.NameToLayer("Player") && layer != LayerMask.NameToLayer("Enemy") && layer != LayerMask.NameToLayer("Npc"))
            {
                if (netObj != null && netObj.HasInputAuthority)
                {
                    netObj.RemoveInputAuthority();
                }
            }
            
            // 손에서 해제 (데이터만 관리)
            inventory.DropHeldObject();
            obj.transform.SetParent(null);

            // 물리/충돌 복구
            EnableItemPhysics(obj, direction);
        }
    }
    
    // ⚡ 아이템의 물리 시뮬레이션 활성화 (던졌을 때)
    private void EnableItemPhysics(GameObject item, Vector2 direction)
    {
        var rigidbody = item.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            float currentZAngle = item.transform.eulerAngles.z;
            rigidbody.rotation = currentZAngle;
            rigidbody.simulated = true; // 항상 활성화
            rigidbody.isKinematic = false;
            rigidbody.linearVelocity = direction * throwForce;
            rigidbody.angularVelocity = 0f;
        }
        
        var collider = item.GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = false;
    }
} 