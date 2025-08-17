using System;
using UnityEngine;

[System.Serializable]
public class GameInvitePayload
{
    public string RoomID;
    public string InviterName;
    public string Timestamp;
    
    public GameInvitePayload(string roomID, string inviterName)
    {
        this.RoomID = roomID;
        this.InviterName = inviterName;
        this.Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }
    
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    
    public static GameInvitePayload FromJson(string json)
    {
        try
        {
            return JsonUtility.FromJson<GameInvitePayload>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"GameInvitePayload 파싱 실패: {ex.Message}");
            return null;
        }
    }
    
    public bool IsValid()
    {
        try
        {
            DateTime inviteTime = DateTime.Parse(Timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);
            TimeSpan elapsed = DateTime.UtcNow - inviteTime;
            return elapsed.TotalMinutes <= 5.0;
        }
        catch (Exception ex)
        {
            Debug.LogError($"초대 시간 검증 실패: {ex.Message}");
            return false;
        }
    }
}
