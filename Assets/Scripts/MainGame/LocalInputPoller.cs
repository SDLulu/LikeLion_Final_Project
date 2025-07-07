using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

// 🎮 로컬 플레이어의 입력을 수집하여 네트워크로 전송하는 핵심 시스템
// 📡 NetworkBehaviour: 네트워크 오브젝트로 동작
// 📞 INetworkRunnerCallbacks: Fusion의 다양한 네트워크 이벤트를 처리하는 인터페이스
public class LocalInputPoller : NetworkBehaviour, INetworkRunnerCallbacks
{
    // 🎯 입력을 수집할 대상 플레이어 컨트롤러
    [SerializeField] private PlayerController player;

    // 🎬 네트워크 오브젝트가 생성될 때 한 번 호출
    public override void Spawned()
    {
        // ✅ 로컬 플레이어인지 확인 (자신의 캐릭터인지 체크)
        if (Runner.LocalPlayer == Object.InputAuthority)
        {
            // 📞 로컬 플레이어인 경우에만 네트워크 콜백 등록
            // 다른 플레이어의 입력은 수집할 필요가 없음!
            Runner.AddCallbacks(this);
        }
    }

    // 🎮 가장 중요한 메서드! Fusion이 입력을 요청할 때마다 호출됨
    // ⚡ 매 네트워크 틱마다 호출되어 로컬 입력을 수집
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // ✅ Runner가 실행 중인지 안전성 확인
        if (runner != null && runner.IsRunning)
        {
            // 🔄 PlayerController에서 현재 입력 상태를 가져옴
            // (키보드, 마우스, 조이스틱 등의 입력을 PlayerData 구조체로 변환)
            var data = player.GetPlayerNetworkInput();
            
            // 📤 수집된 입력 데이터를 네트워크 입력으로 설정
            // 이 데이터가 서버와 다른 클라이언트들에게 전송됨
            input.Set(data);
        }
    }

    // 👁️ 오브젝트가 관심 영역(AOI)을 벗어날 때 호출
    // AOI = Area of Interest (플레이어가 볼 수 있는 범위)
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // 🚫 현재 프로젝트에서는 AOI 기능을 사용하지 않음
    }

    // 👁️ 오브젝트가 관심 영역(AOI)에 진입할 때 호출
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
       // 🚫 현재 프로젝트에서는 AOI 기능을 사용하지 않음
    }

    // 🚪 새로운 플레이어가 게임에 참가했을 때 호출
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // 📝 현재는 별도 처리 없음 (다른 스크립트에서 처리할 수 있음)
    }

    // 🚪 플레이어가 게임을 떠났을 때 호출
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // 📝 현재는 별도 처리 없음 (다른 스크립트에서 처리할 수 있음)
    }

    // ⚠️ 플레이어의 입력이 누락되었을 때 호출
    // 네트워크 지연이나 패킷 손실 등으로 입력이 전달되지 않았을 때
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // 🔧 현재는 별도 처리 없음 (기본 동작 사용)
    }

    // 🔌 네트워크 연결이 종료될 때 호출
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        // 🧹 현재는 별도 정리 작업 없음
    }

    // 🌐 서버에 연결되었을 때 호출
    public void OnConnectedToServer(NetworkRunner runner)
    {
        // 📝 현재는 별도 처리 없음
    }

    // ❌ 서버와의 연결이 끊어졌을 때 호출 (이유 포함)
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        // 📝 현재는 별도 처리 없음  
    }

    // ❌ 서버와의 연결이 끊어졌을 때 호출 (기본)
    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        // 📝 현재는 별도 처리 없음
    }

    // 🤝 클라이언트가 서버에 연결을 요청할 때 호출
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // 🔐 현재는 별도 인증 처리 없음
    }

    // ❌ 서버 연결이 실패했을 때 호출
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        // 📝 현재는 별도 처리 없음
    }

    // 📨 사용자 정의 시뮬레이션 메시지를 받았을 때 호출
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        // 📝 현재는 커스텀 메시지 사용 안함
    }

    // 📋 사용 가능한 세션 목록이 업데이트되었을 때 호출
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // 📝 현재는 세션 브라우징 기능 사용 안함
    }

    // 🔐 커스텀 인증 응답을 받았을 때 호출
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        // 📝 현재는 커스텀 인증 사용 안함
    }

    // 🔄 호스트 마이그레이션이 발생했을 때 호출
    // (현재 호스트가 떠나서 다른 플레이어가 호스트가 될 때)
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        // 🔧 현재는 호스트 마이그레이션 처리 안함
    }

    // 📥 신뢰성 있는 데이터를 받았을 때 호출 (키 포함)
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        // 📝 현재는 신뢰성 데이터 사용 안함
    }

    // 📊 신뢰성 있는 데이터 전송 진행률 업데이트
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
       // 📝 현재는 신뢰성 데이터 사용 안함
    }

    // 📥 신뢰성 있는 데이터를 받았을 때 호출 (기본)
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        // 📝 현재는 신뢰성 데이터 사용 안함
    }

    // 🎬 씬 로딩이 완료되었을 때 호출
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // 📝 현재는 별도 처리 없음
    }

    // 🎬 씬 로딩이 시작될 때 호출
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        // 📝 현재는 별도 처리 없음
    }
}