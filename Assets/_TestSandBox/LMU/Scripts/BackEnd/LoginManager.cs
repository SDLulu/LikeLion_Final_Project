using System;
using System.Threading.Tasks;
using BackEnd;
using LMCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 뒤끝(The Backend) SDK를 사용한 자동 게스트 로그인 및 닉네임 생성/등록을 담당합니다.
/// - 게임 시작 시 기기 기반 게스트 로그인 시도
/// - 로그인 성공 시 닉네임 존재 여부 확인 후 분기
/// - 닉네임 미보유 시 패널 활성화 및 중복 검사/등록 처리
/// </summary>
public class LoginManager : BaseManager<LoginManager>
{
    [Header("닉네임 패널 (초기 비활성)")]
    public GameObject nicknamePanel;

    [Header("닉네임 입력 필드")]
    public InputField nicknameInput;

    [Header("제출 버튼")]
    public Button submitButton;

    [Header("상태 메시지 텍스트")]
    public Text statusText;

    private const string LobbySceneName = "LobbyScene";

    public async Task TryGuestLoginAsync()
    {
        BackendReturnObject loginRet = await Task.Run(() =>
        {
            var ret = Backend.BMember.GuestLogin();
            return ret;
        });

        if (loginRet.IsSuccess() == false)
        {
            Debug.LogError("게스트 로그인 실패 : " + loginRet.GetErrorCode());
            return;
        }
    }

    private async Task TryGuestLoginFlowAsync()
    {
        SetStatus("로그인 중입니다...\n잠시만 기다려주세요.");

        BackendReturnObject loginBro = await Task.Run(() => Backend.BMember.GuestLogin());

        if (loginBro.IsSuccess() == false)
        {
            SetStatus("서버에 연결할 수 없습니다. 인터넷 연결을 확인해주세요.");
            return;
        }

        // 2. 닉네임 정보 확인
        string nickname = TryExtractNicknameFromLogin(loginBro);

        if (string.IsNullOrWhiteSpace(nickname))
        {
            nickname = await TryGetNicknameFallbackAsync();
        }

        if (string.IsNullOrWhiteSpace(nickname) == false)
        {
            // 3. 닉네임이 존재하면 즉시 로비로 이동
            LoadLobby();
            return;
        }

        // 4. 닉네임이 없다면 닉네임 생성 유도
        ShowNicknamePanel();
        SetStatus("닉네임을 입력한 뒤 확인 버튼을 눌러주세요.");
    }

    /// <summary>
    /// 닉네임 제출 버튼 온클릭. 중복 검사 → 업데이트 → 성공 시 로비 로드
    /// </summary>
    public async void OnClickSubmitNickname()
    {
        if (nicknameInput == null)
        {
            SetStatus("닉네임 입력 필드가 설정되지 않았습니다.");
            return;
        }

        string desiredNickname = nicknameInput.text;

        if (string.IsNullOrWhiteSpace(desiredNickname))
        {
            SetStatus("닉네임을 입력해주세요.");
            return;
        }

        desiredNickname = desiredNickname.Trim();

        if (submitButton != null)
        {
            submitButton.interactable = false;
        }

        try
        {
            SetStatus("닉네임 중복을 확인 중입니다...");
            BackendReturnObject checkBro = await Task.Run(() => Backend.BMember.CheckNicknameDuplication(desiredNickname));

            // CheckNicknameDuplication 성공 == 사용 가능한 닉네임으로 간주
            if (checkBro.IsSuccess() == false)
            {
                if (IsDuplicateNicknameResponse(checkBro))
                {
                    SetStatus("이미 사용 중인 닉네임입니다.");
                    return;
                }

                SetStatus(BuildGenericErrorMessage("닉네임 중복 검사 실패", checkBro));
                return;
            }

            SetStatus("닉네임을 등록 중입니다...");
            BackendReturnObject updateBro = await Task.Run(() => Backend.BMember.UpdateNickname(desiredNickname));

            if (updateBro.IsSuccess() == false)
            {
                SetStatus(BuildGenericErrorMessage("닉네임 등록 실패", updateBro));
                return;
            }

            HideNicknamePanel();
            LoadLobby();
        }
        catch (Exception e)
        {
            SetStatus($"알 수 없는 오류가 발생했습니다: {e.Message}");
        }
        finally
        {
            if (submitButton != null)
            {
                submitButton.interactable = true;
            }
        }
    }

    // -------------- 내부 유틸 --------------

    private void ShowNicknamePanel()
    {
        if (nicknamePanel != null)
        {
            nicknamePanel.SetActive(true);
        }
    }

    private void HideNicknamePanel()
    {
        if (nicknamePanel != null)
        {
            nicknamePanel.SetActive(false);
        }
    }

    private void LoadLobby()
    {
        SceneManager.LoadScene(LobbySceneName);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private string TryExtractNicknameFromLogin(BackendReturnObject loginBro)
    {
        try
        {
            // 뒤끝 SDK: 로그인 반환값 JSON에서 닉네임 추출 시도
            var json = loginBro.GetReturnValuetoJSON();

            if (json != null)
            {
                // 키가 없을 수 있으므로 직접 접근 시 예외를 대비
                string nick = null;

                try
                {
                    nick = json["nickname"].ToString();
                }
                catch
                {
                    nick = null;
                }

                if (string.IsNullOrWhiteSpace(nick) == false)
                {
                    return nick;
                }
            }
        }
        catch
        {
            // 무시하고 폴백으로 진행
        }

        return null;
    }

    private async Task<string> TryGetNicknameFallbackAsync()
    {
        try
        {
            var infoBro = await Task.Run(() => Backend.BMember.GetUserInfo());

            if (infoBro.IsSuccess())
            {
                try
                {
                    var json = infoBro.GetReturnValuetoJSON();
                    string nick = null;

                    try
                    {
                        nick = json["row"]["nickname"].ToString();
                    }
                    catch
                    {
                        nick = null;
                    }

                    if (string.IsNullOrWhiteSpace(nick) == false)
                    {
                        return nick;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private bool IsDuplicateNicknameResponse(BackendReturnObject bro)
    {
        // 뒤끝의 중복 응답은 상태 코드/메시지로 구분되는 경우가 많음. 가능한 패턴을 포괄적으로 확인
        string status = bro.GetStatusCode();
        string message = bro.GetMessage();

        bool is409 = status == "409";
        bool mentionsDuplicate = false;

        if (string.IsNullOrEmpty(message) == false)
        {
            string lower = message.ToLowerInvariant();
            mentionsDuplicate = lower.Contains("duplicate") || lower.Contains("duplicated") || lower.Contains("already");
        }

        if (is409)
        {
            return true;
        }

        if (mentionsDuplicate)
        {
            return true;
        }

        return false;
    }

    private string BuildGenericErrorMessage(string title, BackendReturnObject bro)
    {
        string code = bro.GetStatusCode();
        string err = bro.GetErrorCode();
        string msg = bro.GetMessage();

        return $"{title}\n사유: {msg} (Status: {code}, Error: {err})";
    }
}


