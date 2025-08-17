using System;
using BackEnd;
using LMCore;
using UnityEngine;

public class BackEndWorkFlow : BaseManager<BackEndWorkFlow>
{
    // 리더보드 식별 UUID, 공개용이라 여기 적어도 상관없음 
    public string LeaderboardUUID { get; private set; } = "0198b525-b443-762b-9392-e9298dc577e3";
    
    // 유저 데이터가 기록되는 테이블 이름
    public string TABLE_NAME { get; private set; } = "PlayerSession2";
    public static FakeClient.Data FakeNickNameData { get; private set; }
    public static bool IsFakeClient { get; private set; } = false;
    public static string NickName { get; private set; } = "백앤드는 아직 테스트중";
    private AwaitableCompletionSource<bool> _createNickNameTCS;

    public async void CompleteCreateNickName()
    {
        await Awaitable.WaitForSecondsAsync(0.75f);
        _createNickNameTCS?.TrySetResult(true);
    }

    public void SetNickName()
    {
        string nickName = UI_CreateNickName.InputFieldStr;
        Debug.Log($"<color=yellow>닉네임설정 : {nickName}</color>");
        NickName = nickName;
        UpdateBackendNickName(nickName,
            onSuccess: () =>
            {
                Debug.Log("닉네임 업데이트 성공");
                CompleteCreateNickName();
            },
            onFail: () =>
            {
            });
    }


    /// <summary>
    /// 게스트 로그인을 시도하는 함수, 로컬 장치의 고유식별자가 id로 사용
    /// </summary>
    public async Awaitable LoginGuest()
    {
        if (GlobalSetting.Inst.IsEnableBackend == false)
        {
            IsFakeClient = true;
            Debug.Log("백앤드 비활성화");
            return;
        }

        // 첫화면 페이드
        Fader.Inst.ActiveBGImage(true, Color.black);
        await Awaitable.WaitForSecondsAsync(0.5f);
        await Fader.Inst.FadeInAsync(seconds: 0.5f);

        // 로딩 표시 후 백엔드 초기화
        await Fader.Inst.ShowLoadingAsync(onCancel: () => Quit());
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
                // 로그인후 닉네임을 로드
                else if (callback.IsSuccess() && callback.GetStatusCode() == "200")
                {
                    await Fader.Inst.HideLoadingAsync();
                    await LoadNickname();
                    Debug.Log($"이미 회원가입된 게스트 로그인 - {NickName}");
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
        _createNickNameTCS = null;
    }


    /// <summary>
    /// 뒤끝 초기화 함수
    /// </summary>
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

    /// <summary>
    /// 뒤끝 닉네임 업데이트 함수
    /// </summary>
    public void UpdateBackendNickName(string nickName, Action onSuccess = null, Action onFail = null)
    {
        Backend.BMember.UpdateNickname(nickName, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("닉네임 업데이트 성공");
                onSuccess?.Invoke();
            }
            else
            {
                Debug.LogError("닉네임 업데이트 실패");
                onFail?.Invoke();
            }
        });
    }

    public async Awaitable<bool> LoadNickname()
    {
        var loadNicknameTCS = new AwaitableCompletionSource<bool>();
        Backend.BMember.GetUserInfo(callback =>
        {
            if (callback.IsSuccess())
            {
                LitJson.JsonData json = callback.GetReturnValuetoJSON()["row"];
                string nickname = json["nickname"].ToString();
                NickName = nickname;
                loadNicknameTCS.TrySetResult(true);
            }
            else
            {
                Debug.LogError($"유저 정보 로드 실패 : {callback.GetStatusCode()} - {callback.GetErrorMessage()}");
                loadNicknameTCS.TrySetResult(false);
            }
        });

        bool result = await loadNicknameTCS.Awaitable;
        return result;
    }

    
    /// <summary>
    /// 회원탈퇴 함수
    /// </summary>
    [ContextMenu("로컬 뒤끝 정보 삭제")]
    public void DeleteLocalBackend()
    {
        bool ret = InitBackend();
        if (ret == false)
            return;

        Backend.BMember.WithdrawAccount(callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("회원 탈퇴 성공! 모든 데이터가 삭제되었습니다.");
                Debug.Log("로컬 뒤끝 정보 삭제");
                Backend.BMember.DeleteGuestInfo();
                Quit();
            }
            else
            {
                Debug.LogError($"회원 탈퇴 실패: {callback.GetStatusCode()} - {callback.GetErrorMessage()}");
            }
        });
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
