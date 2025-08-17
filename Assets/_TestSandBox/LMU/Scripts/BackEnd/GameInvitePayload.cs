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
		RoomID = roomID;
		InviterName = inviterName;
		Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
	}
	
	public string ToJson()
	{
		return JsonUtility.ToJson(this);
	}
	
	/// <summary>
	/// 최대 200자 제약을 위한 축약 JSON (r, n, t)
	/// </summary>
	public string ToCompactJson(bool includeInviterName = true, int inviterNameMaxLen = 20)
	{
		long epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		string n = InviterName;
		if (includeInviterName)
		{
			if (string.IsNullOrEmpty(n) == false && n.Length > inviterNameMaxLen)
			{
				n = n.Substring(0, inviterNameMaxLen);
			}
			return "{\"r\":\"" + RoomID + "\",\"n\":\"" + (n ?? string.Empty) + "\",\"t\":" + epoch.ToString() + "}";
		}
		else
		{
			return "{\"r\":\"" + RoomID + "\",\"t\":" + epoch.ToString() + "}";
		}
	}
	
	public static GameInvitePayload FromJson(string json)
	{
		try
		{
			var obj = JsonUtility.FromJson<GameInvitePayload>(json);
			if (obj != null && string.IsNullOrEmpty(obj.RoomID) == false)
			{
				return obj;
			}
		}
		catch (Exception)
		{
			// ignore and try compact parse
		}
		
		try
		{
			var j = LitJson.JsonMapper.ToObject(json);
			string room = null;
			string inviter = null;
			string timestamp = null;
			
			try { if (j != null) room = j["r"].ToString(); } catch { }
			try { if (j != null) inviter = j["n"].ToString(); } catch { }
			try
			{
				if (j != null)
				{
					long epoch = 0;
					try { epoch = (long)j["t"]; }
					catch
					{
						try { epoch = long.Parse(j["t"].ToString()); } catch { }
					}
					if (epoch > 0)
					{
						timestamp = DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
					}
				}
			}
			catch { }
			
			if (string.IsNullOrEmpty(room) == false)
			{
				var payload = new GameInvitePayload(room, inviter ?? string.Empty);
				if (string.IsNullOrEmpty(timestamp) == false)
				{
					payload.Timestamp = timestamp;
				}
				return payload;
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("GameInvitePayload 축약 파싱 실패: " + ex.Message);
		}
		
		return null;
	}
	
	public bool IsValid()
	{
		try
		{
			DateTime inviteTime = DateTime.Parse(Timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);
			TimeSpan elapsed = DateTime.UtcNow - inviteTime;
			return elapsed.TotalMinutes <= 5.0f;
		}
		catch (Exception ex)
		{
			Debug.LogError("초대 시간 검증 실패: " + ex.Message);
			return false;
		}
	}
}
