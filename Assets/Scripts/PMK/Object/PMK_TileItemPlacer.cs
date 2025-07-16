using UnityEngine;

public class PMK_TileItemPlacer : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision == null)
        {
            Destroy(gameObject);
        }
    }
}
