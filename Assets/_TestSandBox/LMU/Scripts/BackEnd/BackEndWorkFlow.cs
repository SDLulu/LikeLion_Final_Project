using System.Threading.Tasks;
using BackEnd;
using UnityEngine;

public class BackEndWorkFlow : MonoBehaviour
{
    [ContextMenu("경로보기")]
    public void ShowPath()
    {
        Debug.Log(Application.persistentDataPath);
    }

    private void Awake()
    {
        bool retInit = InitBackend();
        if (retInit == false)
        {
            Debug.LogError("백엔드 초기화 실패");
            return;
        }


        bool retLogin = TryGuestLoginAsync();
        if (retLogin == false)
        {
            bool retSignUp = TrySignUpAsync("test", "test");
            if (retSignUp == false)
            {
                Debug.LogError("회원가입 실패");
                return;
            }
        }
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

    public bool TryGuestLoginAsync()
    {
        BackendReturnObject loginRet = Backend.BMember.GuestLogin();

        if (loginRet.IsSuccess())
        {
            Debug.Log("게스트 로그인 성공 : " + loginRet.GetReturnValue());
            return true;
        }
        else
        {
            Debug.LogError("게스트 로그인 실패 : " + loginRet.StatusCode + "-" + loginRet.GetErrorCode());
            Debug.LogError("오류 메시지 : " + loginRet.GetErrorMessage());
            return false;
        }
    }

    public bool TrySignUpAsync(string id, string password)
    {
        BackendReturnObject signUpRet = Backend.BMember.CustomSignUp(id, password);

        if (signUpRet.IsSuccess())
        {
            Debug.Log("회원가입 성공 : " + signUpRet.GetReturnValue());
            return true;
        }
        else
        {
            Debug.LogError("회원가입 실패 : " + signUpRet.StatusCode + "-" + signUpRet.GetErrorCode());
            Debug.LogError("오류 메시지 : " + signUpRet.GetErrorMessage());
            return false;
        }
    }
}
