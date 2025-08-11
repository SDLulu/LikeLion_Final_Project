using System;
using BackEnd;
using UnityEngine;




public class UserInfo : MonoBehaviour
{
    public UserInfoEvent OnUserInfoEvent = new();
    private static UserData _data = new();
    public static UserData   Data => _data;

    public void GetUserInfoFromBackend()
    {
        Backend.BMember.GetUserInfo(callback =>
        {
            if (callback.IsSuccess())
            {
                try 
                {
                    LitJson.JsonData json = callback.GetReturnValuetoJSON()["row"];
                    _data.ID = json["ID"].ToString();
                    _data.Nickname = json["Nickname"].ToString();
                    _data.Level = json["Level"].ToString();
                    _data.Exp = json["Exp"].ToString();
                    _data.Gold = json["Gold"].ToString();
                }
                catch (Exception e)
                {
                    _data.Reset();
                    Debug.LogError(e);
                }

                OnUserInfoEvent.Invoke();
            }
            else
            {
                _data.Reset();
                Debug.LogError(callback.GetErrorCode());
            }
        });
    }

    [System.Serializable]
    public class UserInfoEvent : UnityEngine.Events.UnityEvent 
    {

    }

    public class UserData
{
    public string ID;
    public string Nickname;
    public string Level;
    public string Exp;
    public string Gold;

    public void Reset()
    {
        ID = default;
        Nickname = default;
        Level = default;
        Exp = default;
        Gold = default;
    }
}

}