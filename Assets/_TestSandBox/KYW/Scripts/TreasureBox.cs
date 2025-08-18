using UnityEngine;
using Fusion;

public class TreasureBox : NetworkBehaviour
{
    [Header("보물상자 설정")]
    [SerializeField] private GameObject moveObject;      // 이동할 오브젝트
    [SerializeField] private GameObject effect;     // 활성화할 파티클
    [SerializeField] private GameObject extraActivateObject; // 추가로 활성화할 오브젝트(폭죽 등)
    [SerializeField] private float moveDistance = 3f;    // 위로 이동할 거리
    [SerializeField] private float moveSpeed = 2f;       // 이동 속도
    
    [Header("네트워크 설정")]
    [SerializeField] private float destroyDelay = 1f;    // 파괴까지 대기 시간
    
    [Networked] private bool IsActivated { get; set; }
    [Networked] private TickTimer MoveTimer { get; set; }
    [Networked] private TickTimer DestroyTimer { get; set; }
    
    private Vector3 startPos;
    private Vector3 endPos;
    private bool isInitialized = false;
    
    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            if (moveObject != null)
            {
                startPos = moveObject.transform.position;
                endPos = startPos + Vector3.up * moveDistance;
                isInitialized = true;
            }
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !isInitialized) return;
        
        // 이동 타이머 처리
        if (MoveTimer.IsRunning)
        {
            if (MoveTimer.Expired(Runner))
            {
                MoveTimer = TickTimer.None;
                // 이동을 최종 위치로 스냅
                if (moveObject != null)
                {
                    moveObject.transform.position = endPos;
                }
                // 이동 완료 후 파괴 타이머 시작
                DestroyTimer = TickTimer.CreateFromSeconds(Runner, destroyDelay);
            }
            else
            {
                // 이동 진행
                MoveObject();
            }
        }
        
        // 파괴 타이머 처리
        if (DestroyTimer.IsRunning && DestroyTimer.Expired(Runner))
        {
            DestroyTimer = TickTimer.None;
            // 파티클 재생 후 파괴
            RPC_ActivateEffect();
            // 네트워크 오브젝트면 Despawn, 아니면 비활성화 처리
            if (moveObject != null)
            {
                var netObj = moveObject.GetComponent<NetworkObject>();
                if (netObj != null && netObj.Runner != null)
                {
                    Runner.Despawn(netObj);
                }
                else
                {
                    moveObject.SetActive(false);
                }
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !IsActivated && Object.HasStateAuthority)
        {
            Activate();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !IsActivated && Object.HasStateAuthority)
        {
            Activate();
        }
    }
    
    private void Activate()
    {
        if (IsActivated) return;
        
        IsActivated = true;
        
        // 이동 타이머 시작 (이동 시간 계산)
        float moveDuration = moveDistance / moveSpeed;
        MoveTimer = TickTimer.CreateFromSeconds(Runner, moveDuration);
    }
    
    private void MoveObject()
    {
        if (moveObject == null) return;
        
        // 타이머 진행률에 따른 이동
        float? remainingTime = MoveTimer.RemainingTime(Runner);
        if (remainingTime.HasValue)
        {
            float progress = 1f - (remainingTime.Value / (moveDistance / moveSpeed));
            progress = Mathf.Clamp01(progress);
            moveObject.transform.position = Vector3.Lerp(startPos, endPos, progress);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ActivateEffect()
    {
        // 파티클 오브젝트 활성화 (모든 클라이언트에서 실행)
        if (effect != null)
        {
            effect.gameObject.SetActive(true);
        }
        if (extraActivateObject != null)
        {
            extraActivateObject.gameObject.SetActive(true);
        }

        // 권한 측에서만 폭죽 스포너 시작
        if (Object.HasStateAuthority)
        {
            TryStartFireworkSpawner();
        }
    }

    private void TryStartFireworkSpawner()
    {
        if (!Object.HasStateAuthority) return;
        if (extraActivateObject != null)
        {
            extraActivateObject.SendMessage("StartSpawningOnAuthority", SendMessageOptions.DontRequireReceiver);
        }
        if (effect != null)
        {
            effect.SendMessage("StartSpawningOnAuthority", SendMessageOptions.DontRequireReceiver);
        }
    }
}
