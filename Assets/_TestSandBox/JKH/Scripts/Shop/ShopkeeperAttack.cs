// ShopkeeperAttack.cs
// 이 스크립트는 AttackHitbox 오브젝트에 붙여주세요.

using Fusion;
using UnityEngine;

public class ShopkeeperAttack : NetworkBehaviour
{
    [SerializeField] private int damage = 10;
    private Collider2D _attackCollider;

    private void Awake()
    {
        _attackCollider = GetComponent<Collider2D>();
        if (_attackCollider != null)
        {
            _attackCollider.enabled = false; // 시작 시 비활성화 확실히
        }
    }

    // 애니메이션 이벤트에서 호출될 함수 1
    public void EnableAttackCollider()
    {
        if (_attackCollider != null)
        {
            _attackCollider.enabled = true;
        }
    }

    // 애니메이션 이벤트에서 호출될 함수 2
    public void DisableAttackCollider()
    {
        if (_attackCollider != null)
        {
            _attackCollider.enabled = false;
        }
    }

    // 트리거에 다른 콜라이더가 닿았을 때 호출
    private void OnTriggerEnter2D(Collider2D other)
    {
        // ⭐️ 피해 판정은 반드시 호스트에서만 실행
        if (!Runner.IsServer) return;

        // 닿은 대상이 'Player' 태그를 가지고 있는지 확인
        if (other.CompareTag("Player"))
        {
            Debug.Log("상점주인 공격이 플레이어와 충돌");
            // 대상에서 PlayerHealth 컴포넌트를 찾음
            PlayerInteractionBase player = other.GetComponentInParent<PlayerInteractionBase>();
            if (player != null)
            {
                // 데미지 처리
                player.TakeDamage(damage);

                // 한 번의 공격에 여러 번 피해를 입지 않도록 즉시 콜라이더를 비활성화
                //DisableAttackCollider();
            }
        }
    }
}