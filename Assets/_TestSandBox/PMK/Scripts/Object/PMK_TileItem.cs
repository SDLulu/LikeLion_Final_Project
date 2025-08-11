using System.Collections.Generic;
using UnityEngine;

public class PMK_TileItem : MonoBehaviour
{
    private GameObject prefab;

    private void Awake()
    {
        var item = DataManager.Inst.ItemData[30000].PrefabPath;
        prefab = Resources.Load<GameObject>(item);
    }

    public void DestroyItem()
    {
        Instantiate(prefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
