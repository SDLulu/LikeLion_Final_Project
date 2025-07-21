using UnityEngine;
using System.Collections.Generic;

public class PMK_DeathTrap : MonoBehaviour
{
    private HashSet<Rigidbody2D> affectedPlayers = new HashSet<Rigidbody2D>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (rb != null && !affectedPlayers.Contains(rb))
        {
            // 중력 제거
            rb.gravityScale = 1f;
            affectedPlayers.Add(rb);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (rb != null && affectedPlayers.Contains(rb))
        {
            // 중력 복원 (기본 1으로 설정)
            rb.gravityScale = 1f;
            affectedPlayers.Remove(rb);
        }
    }

    private void FixedUpdate()
    {
        foreach (Rigidbody2D rb in affectedPlayers)
        {
            // 아래 방향으로 천천히 당기기 (힘)
            Vector2 downwardForce = new Vector2(0, 10f);
            rb.AddForce(downwardForce, ForceMode2D.Force);
        }
    }
}
