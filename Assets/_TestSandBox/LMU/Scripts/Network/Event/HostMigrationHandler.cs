using System;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

  public static class ConnectionTokens 
  {
    public static byte[] NewToken() => Guid.NewGuid().ToByteArray();

    public static int HashToken(byte[] token) => new Guid(token).GetHashCode();

    public static string TokenToString(byte[] token) => new Guid(token).ToString();
  }

/// <summary>
/// 호스트 마이그레이션 처리를 담당하는 핸들러
/// 호스트가 나갔을 때 새로운 호스트로 전환하는 과정을 관리
/// </summary>
public class HostMigrationHandler : MonoBehaviour
{
    private byte[] connectionToken;

    private void Awake()
    {
        connectionToken = ConnectionTokens.NewToken();
    }

    public async void HandleHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("호스트가 나갔습니다 - HostMigrationHandler");

        // 기존 러너 종료
        await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

        // 씬 리로드
        await ReloadCurrentScene();

        // 새 러너로 재시작
        await StartNewRunnerWithMigration(hostMigrationToken);
    }

    /// <summary>
    /// 현재 씬 리로드
    /// </summary>
    private async Awaitable ReloadCurrentScene()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        var loadSceneMode = LoadSceneMode.Additive;
        await SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
        await Awaitable.NextFrameAsync();
    }

    /// <summary>
    /// 새 러너로 마이그레이션 재시작
    /// </summary>
    private async Awaitable StartNewRunnerWithMigration(HostMigrationToken hostMigrationToken)
    {
        await Awaitable.NextFrameAsync();
        if (!LobbyManager.HasInstance)
        {
            Debug.LogError("LobbyManager를 찾을 수 없습니다.");
            return;
        }

        // // 새 러너 설정
        // var newRunner = LobbyManager.Inst.SetForcingRunner(
        //     hostMigrationToken.GameMode.ToString(),
        //     FindAnyObjectByType<NetCallbacks>()
        // );

        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        // 새 러너로 게임 재시작
        // await StartGameWithMigration(newRunner, hostMigrationToken, sceneRef);
    }

    /// <summary>
    /// 마이그레이션으로 게임 시작
    /// </summary>
    private async Awaitable StartGameWithMigration(NetworkRunner newRunner,
                                                  HostMigrationToken hostMigrationToken,
                                                  SceneRef sceneRef)
    {
        var startGameArgs = new StartGameArgs()
        {
            GameMode = hostMigrationToken.GameMode,
            SessionName = "TestRoom",
            PlayerCount = 4,
            SceneManager = LevelManager.Inst,
            ObjectProvider = NetObjProvider.Inst,
            ConnectionToken = connectionToken,
            HostMigrationToken = hostMigrationToken,
            HostMigrationResume = HostMigrationResume,
            Scene = sceneRef,
        };

        await newRunner.StartGame(startGameArgs);
        await Awaitable.NextFrameAsync();
    }

    /// <summary>
    /// 호스트 재연결 콜백
    /// </summary>
    private void HostMigrationResume(NetworkRunner runner)
    {
        foreach (var resume in runner.GetResumeSnapshotNetworkObjects())
        {
            // 필요시 객체 복원 로직 추가
        }
        Debug.Log("호스트 재연결 완료");
    }
}

