using UnityEngine;

public class PMK_TileItem : MonoBehaviour
{
    [SerializeField] private GameObject dropItem;

    public void DestroyItem()
    {
        Instantiate(dropItem, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
