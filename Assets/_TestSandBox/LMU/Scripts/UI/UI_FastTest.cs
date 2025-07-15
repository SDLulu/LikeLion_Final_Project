using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class UI_FastTest : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private RectTransform _fastTestHolder;
    [SerializeField] private Button _fastTestStartButton;
    
    private void Awake()
    {
        _fastTestHolder.gameObject.SetActive(true);
        _fastTestStartButton.onClick.AddListener(OnFastTestStartButtonClick);
        
// #if UNITY_EDITOR
//         _fastTestHolder.gameObject.SetActive(true);
//         _fastTestStartButton.onClick.AddListener(OnFastTestStartButtonClick);
// #else
//         _fastTestHolder.gameObject.SetActive(false);
// #endif
    }

    private async void OnFastTestStartButtonClick()
    {
        try
        {
            var title = UI_Controller.Inst.uiTitle;
            if (title == null)
            {
                Debug.LogError("UI_Title 컴포넌트를 찾을 수 없습니다.");
                return;
            }

            // 네트워크 연결
            await title.RunFastMode();
            await Awaitable.NextFrameAsync();

            // PlayerManager 폴링 대기
            PlayerManager playerM = null;
            float waitTime = 0f;
            const float maxWaitTime = 10f;

            while (playerM == null && waitTime < maxWaitTime)
            {
                await Awaitable.WaitForSecondsAsync(0.2f);
                waitTime += 0.2f;
                playerM = FindAnyObjectByType<PlayerManager>();
            }

            if (playerM == null)
            {
                Debug.LogError("PlayerManager를 찾을 수 없습니다.");
                return;
            }

            // 서버(Host)인 경우에만 게임 시작 처리
            if (playerM.Runner.IsServer)
            {
                await StartGameAsHost(playerM);
            }
            else
            {
                await WaitForGameStart();
                await Awaitable.WaitForSecondsAsync(0.5f);
                playerM.RPC_SetLobbyUI(false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FastTest 실행 중 오류 발생: {e.Message}");
        }
    }

    /// <summary>
    /// 호스트에서 게임 시작 처리
    /// </summary>
    private async Awaitable StartGameAsHost(PlayerManager playerM)
    {
        playerM.SetTestMode(true);
        var result = await playerM.TryStartGameAsync(true);
        if (result)
        {
            GameStates.Inst.DelayForceActiveState<GameStageWaitingState>();
        }
    }

    /// <summary>
    /// 클라이언트에서 게임 시작 대기
    /// </summary>
    private async Awaitable WaitForGameStart(float maxWaitTime = 30f)
    {
        float waitTime = 0f;

        while (waitTime < maxWaitTime)
        {
            await Awaitable.WaitForSecondsAsync(0.5f);
            waitTime += 0.5f;

            // 게임 상태가 변경되었는지 확인
            if (GameStates.Inst != null && GameStates.Inst.StateMachine != null)
            {
                var currentState = GameStates.Inst.StateMachine.ActiveState;
                if (currentState is GameStageWaitingState)
                {
                    Debug.Log("FastTest 완료 - 게임 시작됨");
                    return;
                }
            }
        }

        Debug.LogWarning("게임 시작 대기 시간");
    }

    /// <summary>
    /// 로컬에서 안전한 UI 제어 (폴백용)
    /// </summary>
    private void TrySetLobbyUILocal(bool active)
    {
        try
        {
            if (UI_Controller.Inst != null && UI_Controller.Inst.uiLobby != null)
            {
                UI_Controller.Inst.uiLobby.gameObject.SetActive(active);
                Debug.Log($"로컬 UI 제어: 로비 UI {(active ? "활성화" : "비활성화")}");
            }
            else
            {
                Debug.LogWarning("UI_Controller 또는 uiLobby가 null입니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"로컬 UI 제어 중 오류: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        _fastTestStartButton?.onClick.RemoveAllListeners();
    }
}
