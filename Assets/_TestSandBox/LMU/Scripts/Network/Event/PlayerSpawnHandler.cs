using Fusion;
using UnityEngine;

public class PlayerSpawnHandler : MonoBehaviour
{
    [Header("프리팹 참조")]
    [SerializeField] private GameObject playerMPrefab;
    [SerializeField] private GameObject gameStatesPrefab;

    [Header("디버그용")]
    [SerializeField] private GameMode localGameMode;
    [SerializeField] private PlayerManager hostPlayerManage;
    [SerializeField] private GameStates gameStates;

    public async void HandlePlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"플레이어 {player} 입장");

        if (runner.IsServer && runner.GameMode == GameMode.Host && hostPlayerManage == null) 
        {
            localGameMode = runner.GameMode;
            await OnHostPlayerJoinAsync(runner, player);
        }
        else 
        {
            localGameMode = runner.GameMode;
            await OnClientPlayerJoinAsync(runner, player);
        }
    }

    /// <summary>
    /// 호스트 입장 처리
    /// </summary>
    private async Awaitable OnHostPlayerJoinAsync(NetworkRunner runner, PlayerRef player)
    {
        // 호스트 입장시 네트워크 관리 컴포넌트 스폰
        var id = NetworkPrefabId.FromRaw(NetObjProvider.PLAYER);
        await runner.SpawnAsync(id, Vector3.zero, Quaternion.identity, player,
            onCompleted: (info) =>
            {
                var gameManagerObj = runner.Spawn(playerMPrefab, Vector3.zero, Quaternion.identity, player);
                hostPlayerManage = gameManagerObj.GetComponent<PlayerManager>();

                var gameStatesObj = runner.Spawn(gameStatesPrefab, Vector3.zero, Quaternion.identity, player);
                gameStates = gameStatesObj.GetComponent<GameStates>();

                runner.SetPlayerObject(player, info.Object);
            });
        
        // PlayerManager 네트워크 등록까지 대기
        await hostPlayerManage.IsPollingSpawned();
        hostPlayerManage.AddPlayer(player);
    }

    /// <summary>
    /// 클라이언트 입장처리
    /// </summary>
    private async Awaitable OnClientPlayerJoinAsync(NetworkRunner runner, PlayerRef player)
    {
        var id = NetworkPrefabId.FromRaw(NetObjProvider.PLAYER);
        var spawnedPlayer = await runner.SpawnAsync(id, Vector3.zero, Quaternion.identity, player);
        runner.SetPlayerObject(player, spawnedPlayer);

        // PlayerManager 네트워크 등록까지 대기
        await hostPlayerManage.IsPollingSpawned();
        hostPlayerManage.AddPlayer(player);
    }



    /// <summary>
    /// 게임이 진행 중인지 확인
    /// </summary>
    private bool IsGameInProgress()
    {
        return gameStates != null &&
               gameStates.StateMachine != null &&
               gameStates.StateMachine.ActiveState != null &&
               !(gameStates.StateMachine.ActiveState is LobbyState);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"플레이어 {player} 퇴장");

        if (runner.IsServer == false)
            return;

        var playerObj = runner.GetPlayerObject(player);

        if (PlayerManager.HasInstance)
            PlayerManager.Inst.TryRemovePlayer(player);

        if (playerObj == null)
        {
            Debug.LogError($"플레이어 {player}의 오브젝트가 존재하지 않습니다.");
            return;
        }

        // 네트워크 객체 제거
        runner.Despawn(playerObj);
    }


    public async void OnLateJoin(NetworkRunner runner, PlayerRef player, NetworkObject spawnedPlayer)
    {
        await OnLateJoinPlayerAsync(runner, player, spawnedPlayer);
    }

    /// <summary>
    /// Late Join 플레이어 처리
    /// </summary>
    private async Awaitable OnLateJoinPlayerAsync(NetworkRunner runner, PlayerRef player, NetworkObject spawnedPlayer)
    {
        await Awaitable.NextFrameAsync();

        var gameStates = GameStates.Inst;
        if (gameStates?.StateMachine?.ActiveState == null)
        {
            Debug.LogWarning("GameStates를 찾을 수 없어 Late Join 처리를 건너뜁니다.");
            return;
        }

        var currentState = gameStates.StateMachine.ActiveState;

        // 게임 씬으로 플레이어 이동
        if (IsGameSceneState(currentState))
        {
            MovePlayerToGameScene(runner, spawnedPlayer);
        }

        // 플레이어 상태 설정
        SetPlayerStateForLateJoin(player, spawnedPlayer, currentState);

        Debug.Log($"Late Join 처리 완료: 플레이어 {player}");
    }

    /// <summary>
    /// 게임 씬 상태인지 확인
    /// </summary>
    private bool IsGameSceneState(object currentState)
    {
        return currentState is GameStageWaitingState ||
               currentState is GameStagePlayingState ||
               currentState is GameStageTransitionState;
    }

    /// <summary>
    /// 플레이어를 게임 씬으로 이동
    /// </summary>
    private void MovePlayerToGameScene(NetworkRunner runner, NetworkObject spawnedPlayer)
    {
        var gameSceneObj = GameObject.Find("GameScene");
        if (gameSceneObj != null)
        {
            runner.MoveGameObjectToSameScene(spawnedPlayer.gameObject, gameSceneObj);

            // 플레이어 위치 설정
            var teleporter = spawnedPlayer.GetComponent<PlayerStageController>();
            if (teleporter != null)
            {
                teleporter.SetPosition(new Vector2(0, 0));
            }
        }
    }

    /// <summary>
    /// Late Join 플레이어 상태 설정
    /// </summary>
    private void SetPlayerStateForLateJoin(PlayerRef player, NetworkObject spawnedPlayer, object currentState)
    {
        var playerData = spawnedPlayer.GetComponent<PlayerData>();
        if (playerData != null)
        {
            // Late Join 플레이어는 자동으로 준비 상태로 설정
            playerData.SetReadyState(true);

            // 게임이 이미 시작되었으면 플레이어 상태를 활성화
            if (currentState is GameStagePlayingState)
            {
                Debug.Log($"Late Join 플레이어 {player}를 게임 플레이 상태로 설정합니다.");
                // 필요시 추가 게임 플레이 상태 설정
            }
        }
    }
}
