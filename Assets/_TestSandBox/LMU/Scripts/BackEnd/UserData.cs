using System;
using System.Collections.Generic;
using BackEnd;
using UnityEngine;

public class PlayerSessionRecord
{
	public string NickName = Backend.UserNickName;
	public string OwnerIndate = Backend.UserInDate;
	public string InDate;
	public int SessionDurationSec;
	public string Stage;
	public int ItemScore;
	public int KillScore;
	public DateTime LastUpdate;

	public PlayerSessionRecord()
	{
	}

	public PlayerSessionRecord(LitJson.JsonData json)
	{
		NickName = json["NickName"].ToString();
		OwnerIndate = json["OwnerIndate"].ToString();
		InDate = json["InDate"].ToString();
		SessionDurationSec = int.Parse(json["SessionDurationSec"].ToString());
		Stage = json["Stage"].ToString();
		ItemScore = int.Parse(json["ItemScore"].ToString());
		KillScore = int.Parse(json["KillScore"].ToString());
		LastUpdate = DateTime.Parse(json["LastUpdate"].ToString());
	}

	/// <summary>
	/// Backend 저장을 위한 Param으로 변환
	/// </summary>
	public Param ToParam()
	{
		Param param = new Param();

		param.Add("NickName", NickName);
		param.Add("OwnerIndate", OwnerIndate);
		param.Add("SessionDurationSec", SessionDurationSec);
		param.Add("Stage", Stage);
		param.Add("ItemScore", ItemScore);
		param.Add("KillScore", KillScore);
		param.Add("LastUpdate", DateTime.UtcNow);

		return param;
	}

	public override string ToString()
	{
		return $"NickName : {NickName}\n" +
			$"OwnerIndate : {OwnerIndate}\n" +
			$"SessionDurationSec : {SessionDurationSec}\n" +
			$"Stage : {Stage}\n" +
			$"ItemScore : {ItemScore}\n" +
			$"KillScore : {KillScore}\n" +
			$"LastUpdate : {LastUpdate}";
	}
}

public static class UserData
{
	/// <summary>
	/// 동기 Insert. 성공 시 inDate 문자열을 반환합니다. 실패 시 null 반환.
	/// </summary>
	public static string InsertSession(string tableName, PlayerSessionRecord record)
	{
		if (string.IsNullOrEmpty(tableName))
		{
			return null;
		}

		Param param = record.ToParam();
		BackendReturnObject bro = Backend.GameData.Insert(tableName, param);

		if (bro.IsSuccess())
		{
			string newIndate = bro.GetInDate();
			return newIndate;
		}
		else
		{
			Debug.LogError("세션 데이터 삽입 실패 : " + bro);
			return null;
		}
	}

	/// <summary>
	/// 비동기 Insert. 콜백으로 Backend 응답을 전달합니다.
	/// </summary>
	public static void InsertSessionAsync(string tableName, PlayerSessionRecord record, Action<BackendReturnObject> onCompleted)
	{
		if (string.IsNullOrEmpty(tableName))
		{
			return;
		}

		Param param = record.ToParam();
		Backend.GameData.Insert(tableName, param, callback =>
		{
			if (callback.IsSuccess())
			{
				Debug.Log("세션 데이터 삽입 성공 : " + callback.GetInDate());
			}
			else
			{
				Debug.LogError("세션 데이터 삽입 실패 : " + callback);
			}

			if (onCompleted != null)
			{
				onCompleted(callback);
			}
		});
	}

	/// <summary>
	/// 사용 예시. 필요 시 호출해서 동작 확인.
	/// </summary>
	public static void InsertSessionTest()
	{
		PlayerSessionRecord data = new PlayerSessionRecord();
		data.SessionDurationSec = 1234;
		data.Stage = "Stage-3";
		data.ItemScore = 2500;
		data.KillScore = 47;
		data.LastUpdate = DateTime.UtcNow;

		string inDate = InsertSession("PlayerSession", data);
		if (string.IsNullOrEmpty(inDate) == false)
		{
			Debug.Log("내 세션 inDate : " + inDate);
		}
	}

	/// <summary>
	/// inDate로 나의 단일 세션 데이터를 동기 조회합니다. 실패 또는 미존재 시 null 반환.
	/// </summary>
	public static PlayerSessionRecord GetSessionByInDate(string tableName, string inDate)
	{
		if (string.IsNullOrEmpty(tableName))
		{
			return null;
		}

		if (string.IsNullOrEmpty(inDate))
		{
			return null;
		}

		BackendReturnObject bro = Backend.GameData.GetMyData(tableName, inDate);
		if (bro.IsSuccess() == false)
		{
			Debug.LogError(bro.ToString());
			return null;
		}

		LitJson.JsonData row = bro.GetFlattenJSON()["row"];
		if (row == null)
		{
			Debug.Log("데이터가 존재하지 않습니다");
			return null;
		}

		PlayerSessionRecord record = new PlayerSessionRecord(row);
		return record;
	}

	/// <summary>
	/// inDate로 나의 단일 세션 데이터를 비동기 조회합니다.
	/// </summary>
	public static void GetSessionByInDateAsync(string tableName, string inDate, Action<bool, PlayerSessionRecord, BackendReturnObject> onCompleted)
	{
		if (string.IsNullOrEmpty(tableName))
		{
			if (onCompleted != null)
			{
				onCompleted(false, null, null);
			}
			return;
		}

		if (string.IsNullOrEmpty(inDate))
		{
			if (onCompleted != null)
			{
				onCompleted(false, null, null);
			}
			return;
		}

		Backend.GameData.GetMyData(tableName, inDate, callback =>
		{
			if (callback.IsSuccess() == false)
			{
				Debug.LogError(callback.ToString());
				if (onCompleted != null)
				{
					onCompleted(false, null, callback);
				}
				return;
			}

			LitJson.JsonData row = callback.GetFlattenJSON()["row"];
			if (row == null)
			{
				Debug.Log("데이터가 존재하지 않습니다");
				if (onCompleted != null)
				{
					onCompleted(false, null, callback);
				}
				return;
			}

			PlayerSessionRecord record = new PlayerSessionRecord(row);
			if (onCompleted != null)
			{
				onCompleted(true, record, callback);
			}
		});
	}

	private static Param BuildUpdateParam(PlayerSessionRecord record)
	{
		Param param = new Param();
		param.Add("SessionDurationSec", record.SessionDurationSec);
		param.Add("Stage", record.Stage);
		param.Add("ItemScore", record.ItemScore);
		param.Add("KillScore", record.KillScore);
		param.Add("LastUpdate", DateTime.UtcNow);
		return param;
	}

	/// <summary>
	/// 내 소유 데이터의 특정 inDate 레코드를 동기 업데이트합니다. 성공 여부 반환.
	/// </summary>
	public static bool UpdateSession(string tableName, string inDate, PlayerSessionRecord record)
	{
		if (string.IsNullOrEmpty(tableName))
		{
			return false;
		}

		if (string.IsNullOrEmpty(inDate))
		{
			return false;
		}

		Param param = BuildUpdateParam(record);
		BackendReturnObject bro = Backend.GameData.UpdateV2(tableName, inDate, Backend.UserInDate, param);
		if (bro.IsSuccess() == false)
		{
			Debug.LogError("세션 데이터 업데이트 실패 : " + bro);
			return false;
		}

		return true;
	}

	/// <summary>
	/// 내 소유 데이터의 특정 inDate 레코드를 비동기 업데이트합니다.
	/// </summary>
	public static void UpdateSessionAsync(string tableName, string inDate, PlayerSessionRecord record, Action<BackendReturnObject> onCompleted)
	{
		if (string.IsNullOrEmpty(tableName))
		{
			if (onCompleted != null)
			{
				onCompleted(null);
			}
			return;
		}

		if (string.IsNullOrEmpty(inDate))
		{
			if (onCompleted != null)
			{
				onCompleted(null);
			}
			return;
		}

		Param param = BuildUpdateParam(record);
		Backend.GameData.UpdateV2(tableName, inDate, Backend.UserInDate, param, callback =>
		{
			if (callback.IsSuccess() == false)
			{
				Debug.LogError("세션 데이터 업데이트 실패 : " + callback);
			}

			if (onCompleted != null)
			{
				onCompleted(callback);
			}
		});
	}

	/// <summary>
	/// 특정 소유자(ownerInDate)의 레코드를 동기 업데이트합니다. 성공 여부 반환.
	/// </summary>
	public static bool UpdateSessionOfOwner(string tableName, string inDate, string ownerInDate, PlayerSessionRecord record)
	{
		if (string.IsNullOrEmpty(tableName))
		{
			return false;
		}

		if (string.IsNullOrEmpty(inDate))
		{
			return false;
		}

		if (string.IsNullOrEmpty(ownerInDate))
		{
			return false;
		}

		Param param = BuildUpdateParam(record);
		BackendReturnObject bro = Backend.GameData.UpdateV2(tableName, inDate, ownerInDate, param);
		if (bro.IsSuccess() == false)
		{
			Debug.LogError("세션 데이터 업데이트 실패 : " + bro);
			return false;
		}

		return true;
	}

	/// <summary>
	/// 특정 소유자(ownerInDate)의 레코드를 비동기 업데이트합니다.
	/// </summary>
	public static void UpdateSessionOfOwnerAsync(string tableName, string inDate, string ownerInDate, PlayerSessionRecord record, Action<BackendReturnObject> onCompleted)
	{
		if (string.IsNullOrEmpty(tableName))
		{
			if (onCompleted != null)
			{
				onCompleted(null);
			}
			return;
		}

		if (string.IsNullOrEmpty(inDate))
		{
			if (onCompleted != null)
			{
				onCompleted(null);
			}
			return;
		}

		if (string.IsNullOrEmpty(ownerInDate))
		{
			if (onCompleted != null)
			{
				onCompleted(null);
			}
			return;
		}

		Param param = BuildUpdateParam(record);
		Backend.GameData.UpdateV2(tableName, inDate, ownerInDate, param, callback =>
		{
			if (callback.IsSuccess() == false)
			{
				Debug.LogError("세션 데이터 업데이트 실패 : " + callback);
			}

			if (onCompleted != null)
			{
				onCompleted(callback);
			}
		});
	}
}


