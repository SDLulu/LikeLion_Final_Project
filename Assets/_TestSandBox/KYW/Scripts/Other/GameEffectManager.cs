using Fusion;
using UnityEngine;
using MoreMountains.Feedbacks;

public class GameEffectManager : NetworkBehaviour
{
    public static GameEffectManager Instance { get; private set; }
    
    [Header("타격 효과")]
    [SerializeField] private MMFeedbacks hitSoundFeedback; // 타격음 피드백
    [SerializeField] private ParticleSystem bloodParticleEffect; // 피 파티클
    
    [Header("화살 효과")]
    [SerializeField] private MMFeedbacks arrowShootSound;
    [SerializeField] private MMFeedbacks arrowHitSound;
    
    [Header("타일 효과")]
    [SerializeField] private MMFeedbacks tileDestroySound;
    [SerializeField] private MMFeedbacks tileCreateSound;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // 타격 효과 (데미지 입힐 때)
    public void PlayHitEffect(Vector3 position)
    {
        // 타격음 재생
        hitSoundFeedback?.PlayFeedbacks();
        
        // 피 파티클 재생
        if (bloodParticleEffect != null)
        {
            bloodParticleEffect.transform.position = position;
            bloodParticleEffect.Play();
        }
    }
    
    // 화살 발사 효과
    public void PlayArrowShootEffect()
    {
        arrowShootSound?.PlayFeedbacks();
    }
    
    // 화살 타격 효과
    public void PlayArrowHitEffect(Vector3 position)
    {
        arrowHitSound?.PlayFeedbacks();
        PlayHitEffect(position); // 피 파티클도 함께
    }
    
    // 타일 파괴 효과
    public void PlayTileDestroyEffect()
    {
        tileDestroySound?.PlayFeedbacks();
    }
    
    // 타일 생성 효과
    public void PlayTileCreateEffect()
    {
        tileCreateSound?.PlayFeedbacks();
    }
}
