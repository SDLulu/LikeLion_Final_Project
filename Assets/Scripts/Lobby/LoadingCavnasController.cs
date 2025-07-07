using UnityEngine;
using UnityEngine.UI;

public class LoadingCavnasController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Button cancelBtn;
    // 🌐 네트워크 관련: 네트워크 연결 상태를 관리하는 컨트롤러 참조
    private NetworkRunnerController networkRunnerController;
    
    private void Start()
    {
        // 🌐 네트워크 관련: 전역 네트워크 컨트롤러 참조 가져오기
        networkRunnerController = GlobalManagers.Instance.NetworkRunnerController;
        
        // 🌐 네트워크 관련: 네트워크 연결 시작 이벤트 구독
        // 게임 연결 시작하면 로딩 화면 표시
        networkRunnerController.OnStartedRunnerConnection += OnStartedRunnerConnection;
        
        // 🌐 네트워크 관련: 플레이어 접속 성공 이벤트 구독
        // 성공적으로 연결되면 로딩 화면 숨기기
        networkRunnerController.OnPlayerJoinedSuccessfully += OnPlayerJoinedSuccessfully;
        
        // 🌐 네트워크 관련: 취소 버튼 클릭시 네트워크 연결 종료
        cancelBtn.onClick.AddListener(networkRunnerController.ShutDownRunner);
        this.gameObject.SetActive(false);
    }
    
    // 🌐 네트워크 관련: 네트워크 연결 시작시 호출되는 콜백
    private void OnStartedRunnerConnection()
    {
        this.gameObject.SetActive(true);
        const string CLIP_NAME = "In";
        StartCoroutine(Utils.PlayAnimAndSetStateWhenFinished(gameObject, animator, CLIP_NAME));
    }
    
    // 🌐 네트워크 관련: 플레이어 접속 성공시 호출되는 콜백
    private void OnPlayerJoinedSuccessfully()
    {
        const string CLIP_NAME = "Out";
        StartCoroutine(Utils.PlayAnimAndSetStateWhenFinished(gameObject, animator, CLIP_NAME, false));
    }

    private void OnDestroy()
    {
        // 🌐 네트워크 관련: 메모리 누수 방지를 위해 이벤트 구독 해제
        networkRunnerController.OnStartedRunnerConnection -= OnStartedRunnerConnection;
        networkRunnerController.OnPlayerJoinedSuccessfully -= OnPlayerJoinedSuccessfully;
    }
}
