using Fusion;
using Fusion.Sockets;
using LMCore;
using UnityEngine;

public class ConnectionHandler : MonoBehaviour
{
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        Debug.Log($"OnConnectRequest 연결요청 - {request.RemoteAddress}");
        CheckGameFlowControl(runner, request);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"OnDisconnectedFromServer - 호스트가 연결이 끊겼어요. - {reason}");
    }

    public async void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"OnShutdown - 호스트가 연결이 끊겼어요. - {reason}");

        if (reason == ShutdownReason.DisconnectedByPluginLogic)
        {
            await ShutDown_AtHostQuit();
        }
        else if (reason == ShutdownReason.ServerInRoom)
        {
            await ShotDown_AtServerInRoom();
        }
    }

    public async Awaitable ShutDown_AtHostQuit()
    {
        try
        {
            await Fader.Inst.FadeOutAsync(Color.black, 1.0f);

            // 타이틀씬을 제외한 모든 씬을 UnLoad
            var scenes = LocalSceneManager.Inst.GetAllLoadedScenes();
            foreach (var scene in scenes)
            {
                string lobbyName = GlobalSetting.Inst.LobbyScenePath;
                string gameName = GlobalSetting.Inst.GameScenePath;
                if (scene.name == lobbyName)
                {
                    _ = LocalSceneManager.Inst.UnloadSceneAsync(scene.name);
                }
                else if (scene.name == gameName)
                {
                    _ = LocalSceneManager.Inst.UnloadSceneAsync(scene.name);
                }
            }
            LobbyUI_Manager.Inst.ActiveTitlePanel();
            await Fader.Inst.FadeInAsync(Color.black, 1.0f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ShutDownAtHostQuit 오류: {e.Message}");
        }
    }


    public async Awaitable ShotDown_AtServerInRoom()
    {
        try
        {
            await LobbyUI_Manager.Inst.UIEnterOnline.FallbackRun();
            await Awaitable.NextFrameAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ShotDown_AtServerInRoom 오류: {e.Message}");
        }
    }


    private void CheckGameFlowControl(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request)
    {
        if (runner == null || runner.IsServer == false || PlayerManager.HasInstance == false)
        {
            request.Refuse();
            return;
        }

        bool isGameBlocked = false;
        bool inGame = PlayerManager.Inst.IsInGame;
        bool loadingGame = PlayerManager.Inst.IsGameSceneLoading;

        if (inGame || loadingGame)
        {
            isGameBlocked = true;
        }

        if (isGameBlocked)
        {
            Debug.Log("진행 중인 게임 세션입니다. 새로운 접속을 거부합니다.");
            request.Refuse();
            return;
        }

        request.Accept();
    }
}