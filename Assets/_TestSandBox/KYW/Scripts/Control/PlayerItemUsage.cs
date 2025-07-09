using Fusion;
using UnityEngine;

// 🎮 플레이어 아이템 사용 컨트롤러
// 아이템 사용(좌클릭) 담당 - 근접무기/원거리무기/소모품/기타잡템 처리
public class PlayerItemUsage : NetworkBehaviour
{
    [Header("Usage Settings")]
    [SerializeField] private bool showDebugInfo = true;        // 디버그 정보 표시
    [SerializeField] private float useRange = 2f;              // 사용 범위
    [SerializeField] private LayerMask targetLayerMask = -1;   // 타겟 레이어 마스크
    
    [Header("Effect Settings")]
    [SerializeField] private float effectDuration = 0.5f;     // 이펙트 지속 시간
    
    // Fusion 2 공식 패턴: 이전 버튼 상태 추적
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    
    // 📦 컴포넌트 참조들
    private SpelunkyPlayerController playerController;
    private PlayerItemPickup itemPickup;
    
    public override void Spawned()
    {
        // 컴포넌트 찾기
        playerController = GetComponent<SpelunkyPlayerController>();
        itemPickup = GetComponent<PlayerItemPickup>();
        
        Debug.Log($"🎮 PlayerItemUsage 생성 - HasInputAuthority: {Object.HasInputAuthority}");
    }
    
    // 아이템 사용 관련 모든 처리를 통합한 메서드 (Fusion 2 공식 패턴)
    public void ProcessInput(SpelunkyPlayerData input)
    {
        // Fusion 2 공식 패턴: GetPressed로 버튼 눌림 감지
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        
        // 이전 상태 업데이트 (공식 패턴)
        ButtonsPrevious = input.NetworkButtons;
        
        // 아이템 사용 (좌클릭)
        if (pressed.IsSet(SpelunkyInputButtons.UseItem))
        {
            if (itemPickup?.CurrentItem != null)
            {
                Debug.Log("🎮 아이템 사용 시도 (GetPressed)");
                UseItemRpc(input.MouseWorldPosition);
            }
            else
            {
                Debug.Log("🎮 사용할 아이템이 없음");
            }
        }
    }
    
    // 🎮 아이템 사용 RPC
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void UseItemRpc(Vector2 mouseWorldPosition)
    {
        if (itemPickup?.CurrentItem == null) return;
        
        GameObject currentItem = itemPickup.CurrentItem;
        string itemName = currentItem.name;
        
        // 사용 방향 계산
        Vector2 useDirection = (mouseWorldPosition - (Vector2)transform.position).normalized;
        
        // 아이템 종류에 따른 사용 처리
        ProcessItemUsage(currentItem, useDirection, mouseWorldPosition);
        
        Debug.Log($"🎮 아이템 사용: {itemName} -> 방향: {useDirection}");
    }
    
    // 🔧 아이템 사용 처리 (종류별 분기)
    private void ProcessItemUsage(GameObject item, Vector2 direction, Vector2 targetPosition)
    {
        string itemName = item.name.ToLower();
        
        // 아이템 이름으로 종류 판별 (임시)
        if (IsNearWeapon(itemName))
        {
            // 1. 근접무기 - 휘두르기
            ProcessNearWeaponUsage(item, direction);
        }
        else if (IsRangedWeapon(itemName))
        {
            // 2. 원거리무기 - 발사
            ProcessRangedWeaponUsage(item, direction, targetPosition);
        }
        else if (IsConsumable(itemName))
        {
            // 3. 소모품 - 사용
            ProcessConsumableUsage(item, direction, targetPosition);
        }
        else
        {
            // 4. 기타잡템 - 무반응
            ProcessOtherItemUsage(item);
        }
    }
    
    // ⚔️ 근접무기 사용 처리
    private void ProcessNearWeaponUsage(GameObject weapon, Vector2 direction)
    {
        Debug.Log($"⚔️ 근접무기 휘두름: {weapon.name}");
        
        // 휘두르기 범위 내 타겟 감지
        Vector2 swingPosition = (Vector2)transform.position + direction * (useRange * 0.5f);
        Collider2D[] targets = Physics2D.OverlapCircleAll(swingPosition, useRange, targetLayerMask);
        
        foreach (var target in targets)
        {
            if (target.gameObject != gameObject) // 자기 자신 제외
            {
                // 타겟에게 데미지 처리 (추후 확장)
                Debug.Log($"⚔️ 근접 공격 적중: {target.name}");
                // TODO: 데미지 시스템 연동
            }
        }
        
        // 휘두르기 이펙트 (시각적 피드백)
        CreateSwingEffect(direction);
    }
    
    // 🏹 원거리무기 사용 처리
    private void ProcessRangedWeaponUsage(GameObject weapon, Vector2 direction, Vector2 targetPosition)
    {
        Debug.Log($"🏹 원거리무기 발사: {weapon.name}");
        
        // 발사체 생성 위치
        Vector2 firePosition = (Vector2)transform.position + direction * 0.5f;
        
        // 발사체 생성 (추후 확장)
        CreateProjectile(firePosition, direction, targetPosition);
        
        // TODO: 탄약 소모 시스템 추가
    }
    
    // 💊 소모품 사용 처리
    private void ProcessConsumableUsage(GameObject consumable, Vector2 direction, Vector2 targetPosition)
    {
        Debug.Log($"💊 소모품 사용: {consumable.name}");
        
        string itemName = consumable.name.ToLower();
        
        if (itemName.Contains("potion") || itemName.Contains("포션"))
        {
            // 포션 사용 - 즉시 효과
            ApplyPotionEffect(consumable);
        }
        else if (itemName.Contains("bomb") || itemName.Contains("폭탄"))
        {
            // 폭탄 사용 - 방향으로 던지기
            ThrowBomb(consumable, direction);
        }
        else if (itemName.Contains("rope") || itemName.Contains("로프"))
        {
            // 로프 사용 - 위치에 설치
            PlaceRope(targetPosition);
        }
        else
        {
            // 기타 소모품 - 기본 사용
            UseGenericConsumable(consumable, direction);
        }
        
        // 소모품은 사용 후 제거
        // TODO: 아이템 소모 처리
    }
    
    // 📦 기타잡템 사용 처리 (무반응)
    private void ProcessOtherItemUsage(GameObject item)
    {
        Debug.Log($"📦 기타 아이템 - 사용 불가: {item.name}");
        // 기타잡템은 사용해도 아무 일이 일어나지 않음
    }
    
    // 🔍 아이템 종류 판별 메서드들
    private bool IsNearWeapon(string itemName)
    {
        return itemName.Contains("whip") || itemName.Contains("sword") || itemName.Contains("axe") || 
               itemName.Contains("채찍") || itemName.Contains("검") || itemName.Contains("도끼");
    }
    
    private bool IsRangedWeapon(string itemName)
    {
        return itemName.Contains("gun") || itemName.Contains("bow") || itemName.Contains("shooter") ||
               itemName.Contains("총") || itemName.Contains("활") || itemName.Contains("샷건");
    }
    
    private bool IsConsumable(string itemName)
    {
        return itemName.Contains("potion") || itemName.Contains("bomb") || itemName.Contains("rope") ||
               itemName.Contains("포션") || itemName.Contains("폭탄") || itemName.Contains("로프");
    }
    
    // 🌟 이펙트 메서드들 (추후 확장)
    private void CreateSwingEffect(Vector2 direction)
    {
        // TODO: 휘두르기 파티클/애니메이션 이펙트
        Debug.Log($"🌟 휘두르기 이펙트 생성 - 방향: {direction}");
    }
    
    private void CreateProjectile(Vector2 position, Vector2 direction, Vector2 target)
    {
        // TODO: 발사체 프리팹 생성
        Debug.Log($"🌟 발사체 생성 - 위치: {position}, 방향: {direction}");
    }
    
    private void ApplyPotionEffect(GameObject potion)
    {
        // TODO: 포션 효과 적용 (체력 회복 등)
        Debug.Log($"🌟 포션 효과 적용: {potion.name}");
    }
    
    private void ThrowBomb(GameObject bomb, Vector2 direction)
    {
        // TODO: 폭탄 던지기 처리
        Debug.Log($"🌟 폭탄 던지기: {bomb.name}");
    }
    
    private void PlaceRope(Vector2 position)
    {
        // TODO: 로프 설치 처리
        Debug.Log($"🌟 로프 설치: {position}");
    }
    
    private void UseGenericConsumable(GameObject item, Vector2 direction)
    {
        // TODO: 일반 소모품 사용 처리
        Debug.Log($"🌟 일반 소모품 사용: {item.name}");
    }
    
    // 📊 상태 확인 프로퍼티들
    public bool CanUseItem => itemPickup?.CurrentItem != null;
    public string CurrentItemType
    {
        get
        {
            if (itemPickup?.CurrentItem == null) return "없음";
            
            string itemName = itemPickup.CurrentItem.name.ToLower();
            if (IsNearWeapon(itemName)) return "근접무기";
            if (IsRangedWeapon(itemName)) return "원거리무기";
            if (IsConsumable(itemName)) return "소모품";
            return "기타";
        }
    }
    
    // 🔍 디버그 정보 표시
    private void OnGUI()
    {
        if (!showDebugInfo || !Object.HasInputAuthority) return;
        
        GUILayout.BeginArea(new Rect(10, 380, 300, 100));
        GUILayout.Box("🎮 아이템 사용");
        GUILayout.Label($"사용 가능: {(CanUseItem ? "예" : "아니오")}");
        GUILayout.Label($"아이템 종류: {CurrentItemType}");
        GUILayout.Label($"사용 범위: {useRange}m");
        GUILayout.Label("조작법: 좌클릭 - 사용");
        GUILayout.EndArea();
    }
    
    // 🎯 기즈모 그리기 (사용 범위)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, useRange);
    }
} 