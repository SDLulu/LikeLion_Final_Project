using BackEnd;
using LMCore;
using UnityEngine;

public class BackEndWorkFlow : MonoBehaviour
{
    [ContextMenu("로컬 뒤끝 정보 삭제")]
    private void DeleteLocalBackend()
    {
        Debug.Log("로컬 뒤끝 정보 삭제");
        Backend.BMember.DeleteGuestInfo();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            DeleteLocalBackend();
        }
    }

    public void CompleteCreateNickName()
    {
        _createNickNameTCS?.TrySetResult(true);
    }

    public static FakeClient.Data FakeNickNameData { get; private set; }
    public static bool IsFakeClient { get; private set; } = false;
    public static string NickName {get; private set;} = "백앤드는 아직 테스트중";

    private AwaitableCompletionSource<bool> _createNickNameTCS;
    public async Awaitable LoginGuest()
    {
        // 첫화면 페이드
        Fader.Inst.ActiveBGImage(true, Color.black);
        await Awaitable.WaitForSecondsAsync(0.5f);
        await Fader.Inst.FadeInAsync(seconds: 0.5f);

        // 로딩 표시 후 백엔드 초기화
        await Fader.Inst.ShowLoadingAsync(onCancel: Quit);
        InitBackend();

        var loginTCS = new AwaitableCompletionSource<bool>();
        _createNickNameTCS = new AwaitableCompletionSource<bool>();

        await Awaitable.WaitForSecondsAsync(1.5f);
        Backend.BMember.GuestLogin(async (callback) =>
        {
            try
            {
                // 최초 회원가입
                if (callback.IsSuccess() && callback.GetStatusCode() == "201")
                {
                    Debug.Log("최초 회원가입후 로그인");
                    await Fader.Inst.HideLoadingAsync();
                    LobbyUI_Manager.Inst.UIBackEnd.ShowNickNamePanel(value: true);
                    loginTCS.TrySetResult(true);
                    return;
                }
                else if (callback.IsSuccess() && callback.GetStatusCode() == "200")
                {
                    Debug.Log("이미 회원가입된 게스트 로그인");
                    await Fader.Inst.HideLoadingAsync();
                    loginTCS.TrySetResult(true);
                    this._createNickNameTCS.TrySetResult(true);
                    return;
                }
                // 성공했으나 예상외의 경우
                else if (callback.IsSuccess())
                {
                    Debug.Log("게스트 로그인 성공?");
                    Debug.Log(callback.GetStatusCode());
                    await Fader.Inst.HideLoadingAsync();
                    loginTCS.TrySetResult(true);
                    this._createNickNameTCS.TrySetResult(true);
                    return;
                }
                else
                {
                    Debug.LogError($"게스트 로그인 실패 : {callback.GetStatusCode()} - {callback.GetErrorMessage()}");
                    await Fader.Inst.HideLoadingAsync();
                    loginTCS.TrySetResult(false);
                    this._createNickNameTCS.TrySetResult(false);
                    FakeNickNameData = DataManager.Inst.GetRandomFakeClientData();
                    IsFakeClient = true;
                    return;
                }
            }
            catch (System.Exception)
            {
                Debug.LogError($"[예외] 게스트 로그인 실패 : {callback.GetStatusCode()} - {callback.GetErrorMessage()}");
                await Fader.Inst.HideLoadingAsync();
                Quit();
                loginTCS.TrySetResult(false);
                this._createNickNameTCS.TrySetResult(false);
                return;
            }
        });

        // 게스트 로그인 완료까지 대기
        await loginTCS.Awaitable;
        await _createNickNameTCS.Awaitable;
    }


    public void SetNickName(string nickName)
    {
        NickName = nickName;
    }


    public bool InitBackend()
    {
        BackendReturnObject ret = Backend.Initialize();

        if (ret.IsSuccess())
        {
            Debug.Log("초기화 성공 : " + ret.GetStatusCode());
            return true;
        }
        else
        {
            Debug.LogError("초기화 실패 : " + ret.GetStatusCode());
            Debug.LogError("오류 메시지 : " + ret.GetErrorMessage());
            return false;
        }
    }


    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
