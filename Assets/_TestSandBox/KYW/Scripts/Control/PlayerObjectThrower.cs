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

    
    // 📎 참조할 다른 컴포넌트
    // private PlayerItemPickup itemPickup;  // 📦 아이템 보유 상태 확인용 (삭제)
    private PlayerInventory inventory; // 인벤토리 참조

    // 🌐 네트워크 동기화: 버튼 래칭용
    [Networked]
    private NetworkButtons ButtonsPrevious { get; set; }  // 🎮 이전 프레임 버튼 상태 (클릭 래칭용)
    
    // 아이템 던질 때 부모 해제 지연용 타이머
    [Networked] private TickTimer delayedParentReleaseTimer { get; set; }
    private GameObject delayedParentReleaseObject;
    
    // 🚀 NetworkBehaviour 생성 시 호출 (모든 클라이언트에서 실행)
    public override void Spawned()
    {
        Transform parentPlayer = transform.parent;
        if (parentPlayer != null)
        {
            inventory = parentPlayer.GetComponentInChildren<PlayerInventory>(); // 자식에서만 찾음
            if (inventory == null)
            {
                Debug.LogError($"[{name}] PlayerInventory 컴포넌트를 찾을 수 없습니다! (자식에서만 탐색)");
            }
        }
        else
        {
            Debug.LogError($"[{name}] PlayerObjectThrower이 Player 오브젝트의 하위가 아닙니다!");
        }
    }
    
    // 🎮 입력 처리 (SpelunkyPlayerController에서 호출)
    // 👉 InputAuthority(로컬 플레이어)에서만 호출됨
    public void ProcessInput(SpelunkyPlayerInputData input)
    {
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;

        // 🎮 오브젝트 던지기 조건: 우클릭 + 손에 든 오브젝트 존재
        if (pressed.IsSet(SpelunkyInputButtons.ThrowItem) && inventory != null && inventory.CurrentHeldObject != null)
        {
            ThrowObjectRpc(input.MouseWorldPosition);
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        // 지연된 부모 해제 타이머 체크 (TickTimer는 자동으로 시간이 흐름)
        if (delayedParentReleaseTimer.Expired(Runner))
        {
            if (delayedParentReleaseObject != null)
            {
                delayedParentReleaseObject.transform.SetParent(null);
                delayedParentReleaseObject = null;
            }
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
        if (Object.HasStateAuthority)
        {
            // 공통 해제 로직 사용 (힘 적용)
            ReleaseObject(obj, true, direction);
        }
    }
    
    // ⚡ 아이템의 물리 시뮬레이션 활성화 (던졌을 때)
    private void EnableItemPhysics(GameObject item, Vector2 direction)
    {
        var rigidbody = item.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            // 🎯 물리 상태 완전 초기화
            float currentZAngle = item.transform.eulerAngles.z;
            rigidbody.rotation = currentZAngle;
            rigidbody.simulated = true; // 항상 활성화
            rigidbody.bodyType = RigidbodyType2D.Dynamic;
            rigidbody.gravityScale = 1f; // 중력 복구
            rigidbody.linearVelocity = Vector2.zero; // 속도 초기화
            rigidbody.angularVelocity = 0f; // 회전 속도 초기화
            
            // 🚀 던지기 힘 적용 (AddForce 사용)
            rigidbody.AddForce(direction * throwForce, ForceMode2D.Impulse);
        }
        
        var collider = item.GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = false;
    }
    
    // 🎯 공통: 오브젝트 해제 및 물리 복구 (던지기/탈출 공통 로직)
    public void ReleaseObject(GameObject obj, bool applyForce = false, Vector2 forceDirection = default)
    {
        if (!Object.HasStateAuthority) return;
        
        var netObj = obj.GetComponent<NetworkObject>();
        int layer = obj.layer;

        // 1) 캐릭터(IPlayerInteraction)
        var character = obj.GetComponent<IPlayerInteraction>();
        if (character != null)
        {
            character.OnReleased();

            // 던진 상태 설정 (던질 때만)
            if (applyForce)
            {
                // 플레이어: PlayerStunInvincibleDie, 적/NPC: 각자 구현에서 처리되도록 위임
                var stun = obj.GetComponent<PlayerStunInvincibleDie>();
                if (stun != null) stun.SetThrown(1.5f);
                var enemy = obj.GetComponent<EnemyBase>();
                if (enemy != null) enemy.SetThrown(1.5f);
            }
        }
        else
        {
            // 2) 아이템(IItemInteraction)
            var item = obj.GetComponent<IItemInteraction>();
            if (item != null)
            {
                item.OnReleased();
            }
        }
        
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
        
        // 🎯 캐릭터인 경우 로테이션만 초기화, 아이템은 부모만 해제
        if (layer == LayerMask.NameToLayer("Player") || 
            layer == LayerMask.NameToLayer("Enemy") || 
            layer == LayerMask.NameToLayer("Npc"))
        {
            obj.transform.SetParent(null);
            obj.transform.rotation = Quaternion.identity; // 🔄 로테이션만 0으로 초기화
            Debug.Log($"[PlayerObjectThrower] 캐릭터 로테이션 초기화: {obj.name}");
        }
        else
        {
            // 아이템의 경우 부모 해제를 살짝 늦춰서 던진 직후 바로 맞는 것을 방지
            if (applyForce)
            {
                // 던지기인 경우 TickTimer로 부모 해제를 지연
                delayedParentReleaseTimer = TickTimer.CreateFromSeconds(Runner, 0.05f);
                delayedParentReleaseObject = obj;
            }
            else
            {
                // 일반 해제인 경우 즉시 부모 해제
                obj.transform.SetParent(null);
            }
        }

        // 물리/충돌 복구
        if (applyForce)
        {
            EnableItemPhysics(obj, forceDirection);
        }
        else
        {
            EnableItemPhysicsWithoutForce(obj);
        }
    }
    
    // ⚡ 아이템의 물리 시뮬레이션 활성화 (힘 없이)
    private void EnableItemPhysicsWithoutForce(GameObject item)
    {
        var rigidbody = item.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            // 🎯 물리 상태 완전 초기화
            float currentZAngle = item.transform.eulerAngles.z;
            rigidbody.rotation = currentZAngle;
            rigidbody.simulated = true; // 항상 활성화
            rigidbody.bodyType = RigidbodyType2D.Dynamic;
            rigidbody.gravityScale = 1f; // 중력 복구
            rigidbody.linearVelocity = Vector2.zero; // 속도 초기화
            rigidbody.angularVelocity = 0f; // 회전 속도 초기화
        }
        
        var collider = item.GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = false;
    }
    

} 