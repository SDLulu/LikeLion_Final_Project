using System.Collections.Generic;
using UnityEngine;

public class PMK_TileItem : MonoBehaviour
{
    private GameObject prefab;
    [SerializeField] private int itemIndex = 30000; // 아이템 인덱스

    private void Awake()
    {
        var item = DataManager.Inst.ItemData[itemIndex].PrefabPath;
        prefab = Resources.Load<GameObject>(item);
    }

    public void DestroyItem()
    {
        Instantiate(prefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
