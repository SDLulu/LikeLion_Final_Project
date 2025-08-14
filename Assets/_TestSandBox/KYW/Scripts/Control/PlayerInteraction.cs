using Fusion;
using UnityEngine;
using System.Collections.Generic;

// 🎯 플레이어 상호작용 컴포넌트
// 📍 위치: Player 오브젝트
// 🎯 목적: F키로 주변 상호작용 가능한 오브젝트와 상호작용
public class PlayerInteraction : NetworkBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private LayerMask interactionLayerMask = -1;  // 🎛️ 상호작용 가능한 레이어들
    [SerializeField] private float interactionRange = 2f;          // 🔍 상호작용 범위
    [SerializeField] private bool showDebugInfo = true;            // 🔍 디버그 정보 표시
    
    // 디버그용 public 프로퍼티
    public float InteractionRange { get { return interactionRange; } }
    
    // 🌐 네트워크 동기화 변수들
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }  // 🎮 이전 프레임 버튼 상태
    
    // 🎯 상호작용 가능한 오브젝트들 (로컬에서만 관리)
    private List<GameObject> interactableObjects = new List<GameObject>();
    
    // 📎 참조할 다른 컴포넌트들
    private SpelunkyPlayerController playerController;
    private Transform playerTransform;
    
    public override void Spawned()
    {
        // 컴포넌트 참조 설정
        playerController = GetComponent<SpelunkyPlayerController>();
        playerTransform = transform;
        
        // 필수 컴포넌트 검증
        if (playerController == null)
            Debug.LogError($"[{name}] SpelunkyPlayerController 컴포넌트를 찾을 수 없습니다!");
    }
    
    // 🎮 입력 처리 (SpelunkyPlayerController에서 호출)
    public void ProcessInput(SpelunkyPlayerInputData input)
    {
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;
        
        // 🎮 상호작용 입력 감지 (F키)
        if (pressed.IsSet(SpelunkyInputButtons.buy))
        {
            Debug.Log("[PlayerInteraction] 상호작용 입력 감지됨");
            
            // 상태 확인 - 상호작용 불가능한 상태면 처리하지 않음
            if (playerController.IsDead || playerController.IsStunned || playerController.IsHeld || playerController.IsThrown)
            {
                Debug.Log("[PlayerInteraction] 현재 상태에서 상호작용 불가능");
                return;
            }
            
            // 가장 가까운 상호작용 가능한 오브젝트 찾기
            var nearest = FindNearestInteractable();
            if (nearest != null)
            {
                Debug.Log($"[PlayerInteraction] 상호작용 대상: {nearest.name}");
                InteractWithObject(nearest);
            }
            else
            {
                Debug.Log("[PlayerInteraction] 주변에 상호작용 가능한 오브젝트 없음");
            }
        }
    }
    
    // 🎯 상호작용 실행
    private void InteractWithObject(GameObject interactable)
    {
        // IInteractable 인터페이스를 구현한 컴포넌트 찾기 여기에 이거대신 스크립트 연결해주면댐댐
        var interactableComponent = interactable.GetComponent<IInteractable>();
        var shopitem = interactable.GetComponent<ShopItem>();
        if (interactableComponent != null)
        {
            // 상호작용 실행
            shopitem.OnInteract(this);
            Debug.Log($"[PlayerInteraction] {interactable.name}과 상호작용 완료");
        }
        else
        {
            Debug.LogWarning($"[PlayerInteraction] {interactable.name}에 IInteractable 컴포넌트가 없습니다!");
        }
    }
    
    // 🔍 가장 가까운 상호작용 가능한 오브젝트 찾기
    public GameObject FindNearestInteractable()
    {
        GameObject nearest = null;
        float nearestDistance = float.MaxValue;
        
        // 주변의 모든 Collider2D 검사
        Collider2D[] colliders = Physics2D.OverlapCircleAll(playerTransform.position, interactionRange, interactionLayerMask);
        
        foreach (var collider in colliders)
        {
            // IInteractable 인터페이스를 구현한 컴포넌트가 있는지 확인
            if (collider.GetComponent<IInteractable>() != null)
            {
                float distance = Vector2.Distance(playerTransform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = collider.gameObject;
                }
            }
        }
        
        return nearest;
    }
    
    // 🔍 디버그 정보 표시 (PlayerDebugManager로 통합됨)
    // private void OnGUI() 메서드 제거됨
    
    // 🔍 디버그용 기즈모 표시
    private void OnDrawGizmosSelected()
    {
        if (showDebugInfo)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}

// 🎯 상호작용 가능한 오브젝트가 구현해야 하는 인터페이스
public interface IInteractable
{
    void OnInteract(PlayerInteraction player);
}