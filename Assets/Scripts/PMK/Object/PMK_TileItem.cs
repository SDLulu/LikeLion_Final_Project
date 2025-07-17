using UnityEngine;

public class PMK_TileItem : MonoBehaviour
{
    [SerializeField] private GameObject dropItem;

    private void Start()
    {
    }

    public void DestroyItem()
    {

        Instantiate(dropItem, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
