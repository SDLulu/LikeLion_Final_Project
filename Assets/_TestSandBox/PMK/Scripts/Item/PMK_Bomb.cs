using UnityEngine;

public class PMK_Bomb : PMK_TileDestroyItem
{
    private void Awake()
    {
        base.Start(); // 부모의 Start 호출 (코루틴 실행)
    }
}
