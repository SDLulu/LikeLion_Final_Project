using UnityEngine;

[CreateAssetMenu(fileName = "SO_CharacterData", menuName = "스크립터블/CharacterData")]
public class SO_CharacterData : ScriptableObject
{
    public string CharacterName;
    public Sprite CharacterImage;
    public Vector2 UILayoutSize;
}
