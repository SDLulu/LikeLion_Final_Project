using Fusion;
using UnityEngine;
using System;

// 플레이어 외형(스킨) 동기화 및 적용 담당
public class PlayerAppearance : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerSkinDatabase skinDatabase;
    [SerializeField, Tooltip("플레이어의 시각 루트(Visual) Transform. 비워두면 자동 탐색")]
    private Transform visualRoot;
    [SerializeField, Tooltip("체크 시 유령용 애니메이터를 적용합니다")] private bool useGhostAnimator = false;

    [Networked] public NetworkString<_32> SkinKey { get; set; }
    
    // 스킨 변경 이벤트 추가
    public event Action<string> OnSkinChanged;

    private string _lastSkinKey = string.Empty;

    public override void Spawned()
    {
        // Visual 루트 우선 탐색 (인스펙터에서 지정 가능)
        if (visualRoot == null)
        {
            var root = transform.root;
            if (root != null)
            {
                // 이름이 "Visual"인 자식을 우선 찾음
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t != null && t.name == "Visual")
                    {
                        visualRoot = t;
                        break;
                    }
                }
                // 못 찾으면 루트 자체를 후보로 사용
                if (visualRoot == null)
                    visualRoot = root;
            }
        }

        if (animator == null)
            animator = visualRoot != null ? visualRoot.GetComponentInChildren<Animator>() : GetComponentInChildren<Animator>();
    }

    // StateAuthority 전용: 스킨을 변경 (네트워크 값만 변경)
    public void ChangeSkin(string newSkinKey)
    {
        if (!HasStateAuthority) return;
        SkinKey = newSkinKey;
    }

    public override void Render()
    {
        if (animator == null || skinDatabase == null) return;

        var key = SkinKey.ToString();
        
        // 스킨 변경 감지 및 이벤트 발생
        if (key != _lastSkinKey)
        {
            _lastSkinKey = key;
            OnSkinChanged?.Invoke(key);
        }
        
        var targetController = useGhostAnimator
            ? skinDatabase.GetGhostAnimatorByKey(key)
            : skinDatabase.GetAnimatorByKey(key);
        if (targetController != null && animator.runtimeAnimatorController != targetController)
        {
            animator.runtimeAnimatorController = targetController;
        }
    }
    
    /// <summary>
    /// 스킨 키에 해당하는 스프라이트를 가져옵니다.
    /// </summary>
    /// <param name="skinKey">스킨 키</param>
    /// <returns>해당하는 스프라이트, 없으면 null</returns>
    public Sprite GetSkinSprite(string skinKey)
    {
        if (skinDatabase == null) return null;
        return skinDatabase.GetSpriteByKey(skinKey);
    }

    /// <summary>
    /// 스킨 키에 해당하는 시체 프리팹을 가져옵니다.
    /// </summary>
    /// <param name="skinKey">스킨 키</param>
    /// <returns>해당하는 시체 프리팹, 없으면 null</returns>
    public GameObject GetCorpsePrefab(string skinKey)
    {
        if (skinDatabase == null) return null;
        return skinDatabase.GetCorpsePrefabByKey(skinKey);
    }
}


