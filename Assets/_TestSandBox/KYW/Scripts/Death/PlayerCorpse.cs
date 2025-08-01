using Fusion;
using UnityEngine;

// 💀 플레이어 시체 프리팹 컴포넌트
public class PlayerCorpse : NetworkBehaviour
{
    [Header("시체 설정")]
    [SerializeField] private float fadeOutDuration = 5f; // 페이드아웃 지속 시간
    [SerializeField] private float fadeOutDelay = 2f; // 페이드아웃 시작 전 대기 시간
    
    private SpriteRenderer spriteRenderer;
    private float fadeTimer = 0f;
    private bool isFading = false;
    
    public override void Spawned()
    {
        // 스프라이트 렌더러 찾기
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (spriteRenderer == null)
        {
            Debug.LogError($"[{name}] SpriteRenderer를 찾을 수 없습니다!");
            return;
        }
        
        // 초기 알파값 설정
        Color color = spriteRenderer.color;
        color.a = 1f;
        spriteRenderer.color = color;
        
        Debug.Log($"[{name}] 플레이어 시체 스폰됨!");
    }
    
    public override void FixedUpdateNetwork()
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 페이드아웃 타이머 업데이트
        if (!isFading)
        {
            fadeTimer += Runner.DeltaTime;
            
            // 페이드아웃 시작
            if (fadeTimer >= fadeOutDelay)
            {
                isFading = true;
                fadeTimer = 0f;
            }
        }
        else
        {
            fadeTimer += Runner.DeltaTime;
            
            // 페이드아웃 진행
            float alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeOutDuration);
            
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }
            
            // 완전히 투명해지면 제거
            if (fadeTimer >= fadeOutDuration)
            {
                Runner.Despawn(Object);
            }
        }
    }
} 