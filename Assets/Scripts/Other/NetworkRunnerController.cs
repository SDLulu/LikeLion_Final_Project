using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

// 🎯 NetworkRunnerController: Fusion 네트워크의 핵심 관리자
// 역할: 게임 세션 시작/종료, 플레이어 연결 관리, 네트워크 이벤트 처리
// INetworkRunnerCallbacks: Fusion에서 네트워크 이벤트가 발생할 때 자동으로 호출되는 메서드들을 정의한 인터페이스
public class NetworkRunnerController : MonoBehaviour, INetworkRunnerCallbacks
{
    // 🔔 이벤트: 다른 스크립트들이 네트워크 상태 변화를 알 수 있도록 함
    public event Action OnStartedRunnerConnection;      // 네트워크 연결 시작할 때
    public event Action OnPlayerJoinedSuccessfully;    // 플레이어가 성공적으로 접속했을 때
    
    // 🏷️ 로컬 플레이어의 닉네임 저장 (이 컴퓨터의 플레이어 이름)
    public string LocalPlayerNickname { get; private set; }
    
    // 🎮 NetworkRunner 프리팹: Fusion의 핵심 컴포넌트 (Unity의 NetworkManager 같은 역할)
    [SerializeField] private NetworkRunner networkRunnerPrefab;

    // 🎯 실제 생성된 NetworkRunner 인스턴스
    private NetworkRunner networkRunnerInstance;
    
    // 🛠️ 개발용 설정: 현재 씬에서 바로 테스트할지 여부
    private bool skipSceneLoading = false;

    // 🛑 네트워크 연결 종료 메서드
    public void ShutDownRunner()
    {
        networkRunnerInstance.Shutdown();
    }

    // 🏷️ 플레이어 닉네임 설정 (로비에서 입력받은 이름 저장)
    public void SetPlayerNickname(string str)
    {
        LocalPlayerNickname = str;
    }
    
    // 🚀 게임 시작 메서드 (가장 중요한 메서드!)
    // GameMode: Host, Client, Server 등 어떤 방식으로 게임을 시작할지
    // roomName: 게임 방 이름 (같은 방 이름끼리 연결됨)
    public void StartGame(GameMode mode, string roomName)
    {
        StartGameInternal(mode, roomName, false); // 기본적으로 씬 이동 수행
    }
    
    // 🛠️ 개발용 게임 시작 메서드 (씬 이동 건너뛰기 옵션)
    public void StartGame(GameMode mode, string roomName, bool skipSceneLoad)
    {
        StartGameInternal(mode, roomName, skipSceneLoad);
    }
    
    // 🔧 내부 게임 시작 메서드
    private async void StartGameInternal(GameMode mode, string roomName, bool skipSceneLoad)
    {
        // 🔔 네트워크 연결 시작 이벤트 알림
        OnStartedRunnerConnection?.Invoke();
        
        // 🎮 NetworkRunner가 없으면 새로 생성
        if (networkRunnerInstance == null)
        {
            networkRunnerInstance = Instantiate(networkRunnerPrefab);
        }
        
        // 📞 이 스크립트를 콜백 리스너로 등록 (네트워크 이벤트 받기 위함)
        networkRunnerInstance.AddCallbacks(this);

        // 🎯 ProvideInput = true: 이 클라이언트가 입력을 서버로 전송한다는 의미
        // (키보드, 마우스 입력을 네트워크로 보냄)
        networkRunnerInstance.ProvideInput = true;

       // 🎮 게임 시작 설정값들
       var startGameArgs = new StartGameArgs()
       {
           GameMode = mode,                    // 게임 모드 (Host/Client/Server)
           SessionName = roomName,             // 방 이름 (같은 이름끼리 연결)
           PlayerCount = 4,                    // 최대 플레이어 수
           SceneManager = networkRunnerInstance.GetComponent<INetworkSceneManager>(),  // 씬 관리자
    
       };

      // 🚀 실제 게임 시작! (비동기 처리)
      var result = await networkRunnerInstance.StartGame(startGameArgs);
      
      // 🏠 서버(Host)인 경우에만 씬 로딩 처리
      if (networkRunnerInstance.IsServer)
      {
          if (result.Ok)
          {
              // 🛠️ 개발 모드에서는 씬 이동 건너뛰기
              if (!skipSceneLoad)
              {
                  // ✅ 성공시 메인 게임 씬으로 이동
                  //const string SCENE_NAME = "MainGame";
                  const string SCENE_NAME = "Main";
                  networkRunnerInstance.LoadScene(SCENE_NAME);
              }
              else
              {
                  Debug.Log("🛠️ [DEV MODE] 씬 이동 건너뜀 - 현재 씬에서 플레이어 소환 가능");
              }
          }
          else
          {
              // ❌ 실패시 에러 로그
              Debug.LogError($"Failed to start: {result.ShutdownReason}");
          }
      }
    }

    // 🔍 AOI (Area of Interest) 관련 콜백들
    // AOI: 플레이어 주변의 관심 영역 (멀리 있는 객체는 동기화 안함으로 최적화)
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // 객체가 관심 영역에서 벗어났을 때
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // 객체가 관심 영역에 들어왔을 때
    }

    // 👥 플레이어 접속/퇴장 이벤트
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
       Debug.Log("OnPlayerJoined");
       // 🔔 플레이어 접속 성공 이벤트 알림
       OnPlayerJoinedSuccessfully?.Invoke();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("OnPlayerLeft");
    }

    // 🎮 입력 처리 콜백 (매 프레임마다 호출)
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // 실제 입력 처리는 LocalInputPoller.cs에서 담당
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        Debug.Log("OnInputMissing");
        // 입력 데이터가 누락되었을 때
    }

    // 🛑 네트워크 종료 콜백
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("OnShutdown");

        // 🏠 네트워크 종료시 로비 씬으로 돌아가기
        const string LOBBY_SCENE = "Lobby";
        SceneManager.LoadScene(LOBBY_SCENE);
    }

    // 🌐 서버 연결 상태 콜백들
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("OnConnectedToServer");
        // 서버에 연결 성공
    }

    public void OnConnectedToServer()
    {
        Debug.Log("OnConnectedToServer");
        // 중복 메서드 (Fusion 버전 호환성)
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log("OnDisconnectedFromServer");
        // 서버 연결 끊김
    }

    // 🤝 연결 요청 및 실패 처리
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        Debug.Log("OnConnectRequest");
        // 연결 요청 처리
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.Log("OnConnectFailed");
        // 연결 실패 처리
    }

    // 📨 메시지 및 세션 관련 콜백들
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        Debug.Log("OnUserSimulationMessage");
        // 사용자 정의 시뮬레이션 메시지
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log("OnSessionListUpdated");
        // 세션 목록 업데이트 (방 목록 갱신)
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        Debug.Log("OnCustomAuthenticationResponse");
        // 사용자 인증 응답
    }

    // 🔄 호스트 마이그레이션 (Host가 나갔을 때 다른 플레이어가 Host가 되는 기능)
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("OnHostMigration");
        // 호스트 변경 처리
    }

    // 📡 안정적인 데이터 전송 관련 콜백들
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
        // 안정적인 데이터 수신 (TCP 같은 보장된 전송)
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
        // 안정적인 데이터 전송 진행률
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        Debug.Log("OnReliableDataReceived");
        // 안정적인 데이터 수신 (오버로드 메서드)
    }

    // 🎬 씬 로딩 관련 콜백들
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("OnSceneLoadDone");
        // 씬 로딩 완료
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("OnSceneLoadStart");
        // 씬 로딩 시작
    }
}
