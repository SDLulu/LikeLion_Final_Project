using UnityEngine;

[CreateAssetMenu(fileName = "NewTileItem", menuName = "Tile/Tile Item")]
public class PMK_TileItemTable : ScriptableObject
{
    public GameObject prefab;
    [Range(0, 100)] public int spawnChance;
    public int Money;
}
