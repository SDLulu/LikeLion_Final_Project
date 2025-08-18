using UnityEngine;
using Fusion;

// 원형으로 지정 개수만큼, 지정 주기마다, 지정 반경에서 네트워크 프리팹을 랜덤 스폰
public class CircularFireworkSpawner : NetworkBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private NetworkPrefabRef[] spawnPrefabs; // 후보 프리팹
    [SerializeField] private int countPerBurst = 8;           // 한 번에 뿌릴 개수
    [SerializeField] private float radius = 1.5f;             // 원 반경
    [SerializeField] private float intervalSeconds = 0.5f;    // 주기
    [SerializeField] private int maxBursts = 3;               // 최대 라운드 횟수 (0 이하면 무한)
    [SerializeField] private bool randomizeAngleOffset = true;// 시작 각도 랜덤화

    [Header("속도 부여(선택)")]
    [SerializeField] private float initialSpeed = 3f;         // 스폰 직후 바깥 방향 속도

    [Networked] private TickTimer SpawnTimer { get; set; }
    [Networked] private int BurstsDone { get; set; }
    [Networked] private bool IsRunning { get; set; }

    public void StartSpawningOnAuthority()
    {
        if (!Object.HasStateAuthority) return;
        if (spawnPrefabs == null || spawnPrefabs.Length == 0) return;
        BurstsDone = 0;
        IsRunning = true;
        SpawnTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.01f, intervalSeconds));
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !IsRunning) return;
        if (!SpawnTimer.IsRunning) return;

        if (SpawnTimer.Expired(Runner))
        {
            SpawnTimer = TickTimer.None;
            SpawnBurst();

            BurstsDone += 1;
            if (maxBursts > 0 && BurstsDone >= maxBursts)
            {
                IsRunning = false;
                return;
            }

            SpawnTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.01f, intervalSeconds));
        }
    }

    private void SpawnBurst()
    {
        if (spawnPrefabs == null || spawnPrefabs.Length == 0) return;
        if (countPerBurst <= 0) return;

        float baseAngle = randomizeAngleOffset ? Random.Range(0f, 360f) : 0f;
        float step = 360f / Mathf.Max(1, countPerBurst);

        for (int i = 0; i < countPerBurst; i++)
        {
            float angle = baseAngle + step * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector3 spawnPos = transform.position + (Vector3)(dir * radius);

            var prefabRef = spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
            var spawned = Runner.Spawn(prefabRef, spawnPos, Quaternion.identity, null);

            if (spawned != null && initialSpeed > 0f)
            {
                var rb = spawned.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = dir * initialSpeed;
                }
            }
        }
    }
}


