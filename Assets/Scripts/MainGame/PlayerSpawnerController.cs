using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

// 🎮 플레이어 스폰 관리자
// IPlayerJoined, IPlayerLeft: Fusion의 플레이어 이벤트 콜백 인터페이스
// 플레이어가 접속/퇴장할 때 자동으로 호출되는 메서드들을 정의
public class PlayerSpawnerController : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    // 🎯 NetworkPrefabRef: 네트워크 프리팹 참조
    // 일반 GameObject와 달리 네트워크로 동기화되는 프리팹을 참조
    // Unity Inspector에서 드래그 앤 드롭으로 할당 가능
    [SerializeField] private NetworkPrefabRef playerNetworkPrefab = NetworkPrefabRef.Empty;
    
    // 📍 플레이어 스폰 위치들
    // 여러 위치 중 하나를 선택해서 플레이어 생성
    [SerializeField] private Transform[] spawnPoints;
    
    // 🗂️ 현재 생성된 플레이어들을 관리하는 딕셔너리
    // PlayerRef: Fusion의 플레이어 식별자 (고유 ID)
    // NetworkObject: 실제 생성된 네트워크 객체
    private Dictionary<PlayerRef, NetworkObject> currentSpawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
    
    private void Awake()
    {
        if (GlobalManagers.Instance != null)
        {
            GlobalManagers.Instance.PlayerSpawnerController = this;
        }
    }
    
    // 📝 플레이어 딕셔너리에 추가
    // 다른 스크립트에서 플레이어 생성 완료를 알릴 때 사용
    public void AddToEntry(PlayerRef player, NetworkObject obj)
    {
        // TryAdd: 이미 존재하면 추가하지 않음 (안전한 추가)
        currentSpawnedPlayers.TryAdd(player, obj);
    }

    // 🎲 랜덤 스폰 위치 반환
    // nondeterministic 주석: 비결정적 (클라이언트마다 다를 수 있음)
    // 네트워크 게임에서는 결정적 로직이 중요하지만, 이 메서드는 예외적으로 랜덤 사용
    public Vector2 GetRandomSpawnPoint()
    {
        // Random.Range: Unity의 랜덤 함수 (각 클라이언트마다 다른 값)
        var index = Random.Range(0, spawnPoints.Length - 1);
        return spawnPoints[index].position;
    }

    // 🎯 플레이어 생성 (서버에서만 실행)
    private void SpawnPlayer(PlayerRef playerRef)
    {
        // 🔐 서버 권한 체크: 오직 Host/Server에서만 네트워크 객체 생성 가능
        if (Runner.IsServer)
        {
            // 🔢 PlayerRef.AsIndex: 플레이어 참조를 인덱스 번호로 변환
            // 예: 첫 번째 플레이어 = 0, 두 번째 플레이어 = 1
            var index = playerRef.AsIndex % spawnPoints.Length;
            var spawnPoint = spawnPoints[index].transform.position;
            
            // 🌐 Runner.Spawn: 네트워크 객체 생성 (모든 클라이언트에서 동기화)
            // 매개변수: 프리팹, 위치, 회전, 소유자
            var playerObject = Runner.Spawn(playerNetworkPrefab, spawnPoint, Quaternion.identity, playerRef);
            
            // 🎮 Runner.SetPlayerObject: 플레이어 참조와 객체 연결
            // 이 연결을 통해 나중에 플레이어의 객체를 쉽게 찾을 수 있음
            Runner.SetPlayerObject(playerRef, playerObject);
        }
    }

    // 🗑️ 플레이어 제거 (서버에서만 실행)
    private void DespawnPlayer(PlayerRef playerRef)
    {
        // 🔐 서버 권한 체크: 오직 Host/Server에서만 네트워크 객체 제거 가능
        if (Runner.IsServer)
        {
            // 🔍 딕셔너리에서 플레이어 객체 찾기
            if (currentSpawnedPlayers.TryGetValue(playerRef, out var playerNetworkObject))
            {
                // 🌐 Runner.Despawn: 네트워크 객체 제거 (모든 클라이언트에서 동기화)
                Runner.Despawn(playerNetworkObject);
            }
            
            // 🔄 플레이어 객체 연결 해제
            Runner.SetPlayerObject(playerRef, null);
        }
    }
    
    // 🎉 플레이어 접속 콜백 (IPlayerJoined 인터페이스)
    // 누군가 게임에 접속하면 Fusion이 자동으로 호출
    public void PlayerJoined(PlayerRef player)
    {
        SpawnPlayer(player);
    }

    // 👋 플레이어 퇴장 콜백 (IPlayerLeft 인터페이스)
    // 누군가 게임에서 나가면 Fusion이 자동으로 호출
    public void PlayerLeft(PlayerRef player)
    {
        DespawnPlayer(player);
    }
}

// 📚 Fusion 네트워크 객체 관리 시스템 개념 정리:
//
// 🎯 PlayerRef란?
// - Fusion의 플레이어 고유 식별자 (Player ID)
// - 네트워크 세션에서 각 플레이어를 구별하는 데 사용
// - AsIndex 속성으로 0, 1, 2... 형태의 인덱스 제공
//
// 🌐 NetworkPrefabRef vs GameObject:
// - GameObject: 로컬에서만 존재하는 일반 오브젝트
// - NetworkPrefabRef: 네트워크로 동기화되는 프리팹 참조
// - NetworkPrefabRef는 모든 클라이언트에서 동일한 프리팹을 가리킴
//
// 🏗️ Runner.Spawn vs Instantiate:
// - Instantiate: 로컬에서만 생성 (다른 클라이언트에서 안 보임)
// - Runner.Spawn: 네트워크 생성 (모든 클라이언트에서 동기화)
// - Spawn된 객체는 모든 클라이언트에서 자동으로 생성됨
//
// 🔐 서버 권한 관리:
// - 네트워크 객체 생성/제거는 오직 서버에서만 가능
// - 클라이언트는 요청만 할 수 있고, 서버가 실제 처리
// - 치트 방지와 일관성 유지를 위한 설계
//
// 🎮 플레이어 관리 흐름:
// 1. 플레이어 접속 → IPlayerJoined.PlayerJoined() 자동 호출
// 2. 서버에서 Runner.Spawn()으로 플레이어 객체 생성
// 3. 모든 클라이언트에서 플레이어 객체 자동 생성
// 4. 플레이어 퇴장 → IPlayerLeft.PlayerLeft() 자동 호출
// 5. 서버에서 Runner.Despawn()으로 플레이어 객체 제거
//
// 🔗 콜백 인터페이스:
// - IPlayerJoined: 플레이어 접속 시 호출
// - IPlayerLeft: 플레이어 퇴장 시 호출
// - NetworkBehaviour에서 이 인터페이스들을 구현하면 자동 호출됨
// - 콜백 메서드 이름은 정확히 일치해야 함!
//
// 📊 성능 최적화 포인트:
// - Dictionary 사용으로 O(1) 플레이어 검색
// - TryAdd/TryGetValue로 안전한 딕셔너리 접근
// - 서버에서만 스폰 처리하여 중복 생성 방지
// - AsIndex 활용으로 결정적 스폰 위치 선택
