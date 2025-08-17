using System.Collections.Generic;
using UnityEngine;

// 플레이어 스킨 매핑 데이터베이스 (ScriptableObject)
[CreateAssetMenu(menuName = "KYW/Player/PlayerSkinDatabase", fileName = "PlayerSkinDatabase")]
public class PlayerSkinDatabase : ScriptableObject
{
    [System.Serializable]
    public class SkinEntry
    {
        [Tooltip("스킨을 식별할 문자열 키 (예: 'Penguin', 'Rabbit')")]
        public string SkinKey;
        public RuntimeAnimatorController AnimatorController; // 플레이어용
        public RuntimeAnimatorController GhostAnimatorController; // 유령용
        [Tooltip("UI에 표시할 스킨 스프라이트")]
        public Sprite SkinSprite; // UI용 스킨 스프라이트 추가
        [Tooltip("해당 스킨의 시체 프리팹")]
        public GameObject CorpsePrefab; // 스킨별 시체 프리팹 추가
    }

    [SerializeField]
    private List<SkinEntry> skins = new List<SkinEntry>();

    private Dictionary<string, RuntimeAnimatorController> keyToAnimator;
    private Dictionary<string, RuntimeAnimatorController> keyToGhostAnimator;

    private void OnEnable()
    {
        if (keyToAnimator == null)
            keyToAnimator = new Dictionary<string, RuntimeAnimatorController>(System.StringComparer.OrdinalIgnoreCase);
        else
            keyToAnimator.Clear();

        if (keyToGhostAnimator == null)
            keyToGhostAnimator = new Dictionary<string, RuntimeAnimatorController>(System.StringComparer.OrdinalIgnoreCase);
        else
            keyToGhostAnimator.Clear();

        for (int i = 0; i < skins.Count; i++)
        {
            var entry = skins[i];
            if (entry == null) continue;
            if (string.IsNullOrEmpty(entry.SkinKey)) continue;
            if (!keyToAnimator.ContainsKey(entry.SkinKey))
            {
                keyToAnimator.Add(entry.SkinKey, entry.AnimatorController);
            }
            if (!keyToGhostAnimator.ContainsKey(entry.SkinKey))
            {
                keyToGhostAnimator.Add(entry.SkinKey, entry.GhostAnimatorController != null ? entry.GhostAnimatorController : entry.AnimatorController);
            }
        }
    }

    public RuntimeAnimatorController GetAnimatorByKey(string skinKey)
    {
        if (string.IsNullOrEmpty(skinKey)) return null;
        if (keyToAnimator == null) OnEnable();
        keyToAnimator.TryGetValue(skinKey, out var controller);
        return controller;
    }

    public RuntimeAnimatorController GetGhostAnimatorByKey(string skinKey)
    {
        if (string.IsNullOrEmpty(skinKey)) return null;
        if (keyToGhostAnimator == null) OnEnable();
        keyToGhostAnimator.TryGetValue(skinKey, out var controller);
        return controller;
    }
    
    /// <summary>
    /// 스킨 키에 해당하는 스프라이트를 가져옵니다.
    /// </summary>
    /// <param name="skinKey">스킨 키</param>
    /// <returns>해당하는 스프라이트, 없으면 null</returns>
    public Sprite GetSpriteByKey(string skinKey)
    {
        if (string.IsNullOrEmpty(skinKey)) return null;
        
        // skins 리스트에서 직접 찾기
        foreach (var skin in skins)
        {
            if (skin != null && skin.SkinKey.Equals(skinKey, System.StringComparison.OrdinalIgnoreCase))
            {
                return skin.SkinSprite;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 스킨 키에 해당하는 시체 프리팹을 가져옵니다.
    /// </summary>
    /// <param name="skinKey">스킨 키</param>
    /// <returns>해당하는 시체 프리팹, 없으면 null</returns>
    public GameObject GetCorpsePrefabByKey(string skinKey)
    {
        if (string.IsNullOrEmpty(skinKey)) return null;
        
        // skins 리스트에서 직접 찾기
        foreach (var skin in skins)
        {
            if (skin != null && skin.SkinKey.Equals(skinKey, System.StringComparison.OrdinalIgnoreCase))
            {
                return skin.CorpsePrefab;
            }
        }
        
        return null;
    }
}


