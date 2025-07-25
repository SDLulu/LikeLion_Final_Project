using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PMK_Money : MonoBehaviour
{
    [SerializeField] private List<PMK_TileItemTable> tileItems;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            foreach (var item in tileItems)
            {
                //playermoney = item.Money; 플레이어 돈 시스템 만들어지면 넣기
            }
            Destroy(gameObject); // 돈 아이템 파괴
        }
    }
}
