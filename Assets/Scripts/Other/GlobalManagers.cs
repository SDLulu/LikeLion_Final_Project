using UnityEngine;

public class GlobalManagers : MonoBehaviour
{
    public static GlobalManagers Instance { get; private set; }

    [SerializeField] private GameObject parentObj;
    // 🌐 네트워크 관련: 네트워크 연결을 담당하는 핵심 컨트롤러
    // 전역에서 접근 가능하도록 싱글톤 패턴으로 관리
    [field: SerializeField] public NetworkRunnerController NetworkRunnerController { get; private set; }
    
    // 🌐 네트워크 관련: 게임 내 네트워크 객체들을 관리하는 매니저들
    public PlayerSpawnerController PlayerSpawnerController { get; set; }    // 플레이어 스폰 관리
    public ObjectPoolingManager ObjectPoolingManager { get; set; }          // 네트워크 오브젝트 풀링
    public GameManager GameManager { get; set; }                            // 게임 상태 관리

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(parentObj);
        }
    }
}