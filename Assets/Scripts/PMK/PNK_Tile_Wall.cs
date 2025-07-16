using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PNK_Tile_Wall : MonoBehaviour
{
    public LayerMask whatisPlatform;

    private void Update()
    {
        Destroy(gameObject, 0.1f); // 0.1초 후에 오브젝트 제거
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 실제 충돌 지점 계산
        Vector2 contactPoint = collision.ClosestPoint(transform.position);

        // 디버깅
        Debug.Log($"충돌 지점: {contactPoint}");

        // 플랫폼 레이어에 해당하는 콜라이더 탐색
        Collider2D hit = Physics2D.OverlapCircle(contactPoint, 0.01f, whatisPlatform);
        if (hit != null)
        {
            PMK_Bricks bricks = hit.GetComponent<PMK_Bricks>();
            if (bricks != null)
            {
                bricks.MakeDot(contactPoint); // 올바른 월드 위치 전달
                Destroy(gameObject); // 벽 제거
            }
            else
            {
                Debug.LogWarning("PMK_Bricks 없음");
            }
        }
    }


    private void Dig()
    {
        //int radiusInt = Mathf.RoundToInt(circleCollider2D.radius);
        //for (int i = -radiusInt; i <= radiusInt; i++)
        //{
        //    for (int j = -radiusInt; j <= radiusInt; j++)
        //    {
        //        Vector3 checkCellPos = new Vector3(transform.position.x + i, transform.position.y + j, 0);
        //        float distance = Vector2.Distance(transform.position, checkCellPos) - 0.001f;

        //        if (distance <= radiusInt)
        //        {
        //            Collider2D overCollider2d = Physics2D.OverlapCircle(checkCellPos,0.01f,whatisPlatform);
        //            if (overCollider2d != null)
        //            {
        //                overCollider2d.transform.GetComponent<PMK_Bricks>().MakeDot(checkCellPos);
        //            }
        //        }
        //    }
        //}
    }

}
