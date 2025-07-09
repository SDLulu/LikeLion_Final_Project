using UnityEngine;

[CreateAssetMenu(fileName = "SO_SkinData", menuName = "스크립터블/SkinData")]
public class SO_SkinData : ScriptableObject
{
    [field: SerializeField] public SkinInfo Character_Info {get; private set;}

    public static SkinInfo GetDefaultCharacterData()
    {
        var characterData = Resources.Load<SO_SkinData>("Data/Skin/Beige");
        if (characterData == null)
        {
            Debug.LogError($"경로에 캐릭터 데이터가 존재하지 않습니다 Data/Skin/Beige");
            return null;
        }

        return characterData.Character_Info;
    }

    [System.Serializable]
    public class SkinInfo
    {
        public string SkinName;
        public Sprite SkinImage;
        public string SkinPath;
        public Vector2 UILayoutSize;
    }
}
