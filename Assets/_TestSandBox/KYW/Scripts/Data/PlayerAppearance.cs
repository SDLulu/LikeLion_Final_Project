using Fusion;
using UnityEngine;

// 플레이어 외형(스킨) 동기화 및 적용 담당
public class PlayerAppearance : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerSkinDatabase skinDatabase;
    [SerializeField, Tooltip("플레이어의 시각 루트(Visual) Transform. 비워두면 자동 탐색")]
    private Transform visualRoot;

    [Networked] public NetworkString<_32> SkinKey { get; set; }

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

        var targetController = skinDatabase.GetAnimatorByKey(SkinKey.ToString());
        if (targetController != null && animator.runtimeAnimatorController != targetController)
        {
            animator.runtimeAnimatorController = targetController;
        }
    }
}


