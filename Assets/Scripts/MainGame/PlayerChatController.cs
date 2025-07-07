using Fusion;
using TMPro;
using UnityEngine;

// 💬 멀티플레이어 채팅 시스템을 관리하는 컨트롤러
// 📡 NetworkBehaviour: 네트워크 동기화가 필요한 컴포넌트
//
// 🎯 RPC Targets 차이점 요약:
// 🖥️ StateAuthority: 서버에서만 실행 → [Networked] 프로퍼티 변경 → 자동 동기화
//    - 장점: 안전하고 일관성 보장, 서버가 진실의 근원
//    - 단점: 서버를 거쳐서 약간 느림
//    - 용도: 게임 상태, 중요한 데이터 (예: 타이핑 상태)
//
// 🌐 All: 모든 클라이언트에서 직접 실행 → 즉시 반영
//    - 장점: 빠른 반응성, 즉시 UI/애니메이션 실행
//    - 단점: 클라이언트 신뢰 필요, 일회성 (상태 저장 안됨)
//    - 용도: 시각적 효과, UI 업데이트 (예: 말풍선 애니메이션)
public class PlayerChatController : NetworkBehaviour
{
    // 📝 현재 플레이어가 채팅을 입력 중인지 네트워크로 동기화
    // 다른 플레이어들이 "이 플레이어가 타이핑 중"인지 알 수 있음
    //
    // 💡 ChangeDetector를 사용하지 않는 이유:
    //    - IsTyping은 단순 상태 동기화만 필요 (true/false만 공유)
    //    - 변화 시 특별한 로직 처리가 필요 없음
    //    - vs. 체력: 변화 시 UI 업데이트, 효과, 사운드 등 복잡한 처리 필요
    [Networked] public bool IsTyping { get; private set; }
    
    // 🎮 채팅 입력을 받는 UI 요소들
    [SerializeField] private TMP_InputField inputField; // 텍스트 입력 필드
    [SerializeField] private Animator bubbleAnimator;   // 말풍선 애니메이션
    [SerializeField] private TextMeshProUGUI bubbleText; // 말풍선 내 텍스트

    // 🎬 네트워크 오브젝트가 생성될 때 한 번 호출
    public override void Spawned()
    {
        // 🎯 로컬 플레이어인지 확인 (자신의 캐릭터인지 체크)
        var isLocalPlayer = Object.InputAuthority == Runner.LocalPlayer;
        
        // 💡 채팅 UI는 로컬 플레이어에게만 표시
        // 다른 플레이어의 채팅 입력창은 보일 필요 없음!
        gameObject.SetActive(isLocalPlayer);

        // ✅ 로컬 플레이어인 경우에만 이벤트 등록
        if (isLocalPlayer)
        {
            // 📝 입력 필드가 선택되었을 때 (타이핑 시작)
            inputField.onSelect.AddListener(arg0 => Rpc_UpdateServerTypingStatus(true));
            
            // 🚫 입력 필드가 해제되었을 때 (타이핑 종료)
            inputField.onDeselect.AddListener(arg0 => Rpc_UpdateServerTypingStatus(false));
            
            // 📤 엔터키를 눌러서 채팅을 전송할 때
            inputField.onSubmit.AddListener(OnInputFieldSubmit);
        }
    }

    // 📡 RPC: 타이핑 상태를 서버에 업데이트
    // 🎯 sources: InputAuthority (플레이어 본인만 호출 가능)
    // 🖥️ targets: StateAuthority (서버에서만 실행)
    // 💡 서버 방식: [Networked] 프로퍼티 변경 → Fusion이 자동으로 모든 클라이언트에 동기화
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_UpdateServerTypingStatus(bool isTyping)
    {
        Debug.Log(isTyping);
        // 📝 네트워크 변수 업데이트 (모든 클라이언트에 동기화됨)
        IsTyping = isTyping;
    }

    // ✍️ 채팅 입력이 완료되었을 때 호출되는 함수
    private void OnInputFieldSubmit(string arg0)
    {
        // ✅ 빈 문자열이 아닌 경우에만 전송
        if (!string.IsNullOrEmpty(arg0))
        {
            // 💬 모든 플레이어에게 채팅 메시지 전송
            RpcSetBubbleSpeech(arg0);
        }
    }

    // 💬 채팅 메시지를 모든 플레이어에게 전송하는 RPC
    // 🎯 sources: InputAuthority (플레이어 본인만 호출 가능)
    // 🌐 targets: All (모든 클라이언트에서 실행)
    // 💡 직접 방식: 각 클라이언트에서 UI/애니메이션 즉시 실행 (네트워크 상태 저장 안됨)
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RpcSetBubbleSpeech(NetworkString<_64> txt)
    {
        // 💭 말풍선에 텍스트 설정 (최대 64자)
        bubbleText.text = txt.Value;

        // 🎭 말풍선 애니메이션 트리거
        const string TRIGGER = "Open";
        bubbleAnimator.SetTrigger(TRIGGER);
    }
}























