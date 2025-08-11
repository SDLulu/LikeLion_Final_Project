using UnityEngine;

public class AwakeDestroy : MonoBehaviour
{
    private void Awake()
    {
        GameObject.Destroy(this.gameObject);
    }
}
