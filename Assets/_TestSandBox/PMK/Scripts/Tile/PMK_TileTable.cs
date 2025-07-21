using UnityEngine;

[CreateAssetMenu(fileName = "NewTileItem", menuName = "Tile/Tile Item")]
public class PMK_TileTable : ScriptableObject
{
    public GameObject prefab;
    [Range(0, 100)] public int spawnChance;
}
