using UnityEngine;

public class PNK_Tile_Wall : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PNK_Tile_Wall otherTile = collision.gameObject.GetComponent<PNK_Tile_Wall>();

        if (otherTile != null && otherTile != this)
        {
            Destroy(gameObject);
            Destroy(collision.gameObject);
        }
    }
}
