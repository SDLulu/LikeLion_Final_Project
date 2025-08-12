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
        // 필요 시 2D Animation 사용 시 SpriteLibraryAsset 등도 추가
        // public SpriteLibraryAsset SpriteLibrary;
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
}


