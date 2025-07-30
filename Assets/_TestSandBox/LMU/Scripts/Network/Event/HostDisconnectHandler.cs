using Fusion;
using Fusion.Sockets;
using LMCore;
using UnityEngine;

public class HostDisconnectHandler : MonoBehaviour
{
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"OnDisconnectedFromServer - 호스트가 연결이 끊겼어요. - {reason}");
    }

    public async void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"OnShutdown - 호스트가 연결이 끊겼어요. - {reason}");

        if (reason == ShutdownReason.DisconnectedByPluginLogic)
        {
            await Fader.Inst.FadeOutAsync(Color.black, 1.0f);
            
            // 타이틀씬을 제외한 모든 씬을 UnLoad
            var scenes = LocalSceneManager.Inst.GetAllLoadedScenes();
            foreach (var scene in scenes)
            {
                if (scene.name == "DevLobby")
                {
                    _ = LocalSceneManager.Inst.UnloadSceneAsync(scene.name);
                }
                else if (scene.name == "DevGame")
                {
                    _ = LocalSceneManager.Inst.UnloadSceneAsync(scene.name);
                }
            }
            LobbyUI_Manager.Inst.ActiveTitleUI();
            await Fader.Inst.FadeInAsync(Color.black, 1.0f);
        }
    }





} 