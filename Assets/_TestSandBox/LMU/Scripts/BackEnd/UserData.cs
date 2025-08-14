using System;
using System.Collections.Generic;
using BackEnd;
using UnityEngine;

public class PlayerSessionRecord
{
	public string NickName = Backend.UserNickName;
	public int SessionDurationSec;
	public string Stage;
	public int ItemScore;
	public int KillScore;

	public int TotalScore => ItemScore + KillScore;

	public PlayerSessionRecord()
	{
	}

	public PlayerSessionRecord(LitJson.JsonData json)
	{
		NickName = json["NickName"].ToString();
		SessionDurationSec = int.Parse(json["SessionDurationSec"].ToString());
		Stage = json["Stage"].ToString();
		ItemScore = int.Parse(json["ItemScore"].ToString());
		KillScore = int.Parse(json["KillScore"].ToString());
	}

	/// <summary>
	/// Backend 저장을 위한 Param으로 변환
	/// </summary>
	public Param ToParam()
	{
		Param param = new Param();

		param.Add("NickName", NickName);
		param.Add("SessionDurationSec", SessionDurationSec);
		param.Add("Stage", Stage);
		param.Add("ItemScore", ItemScore);
		param.Add("KillScore", KillScore);
		param.Add("TotalScore", TotalScore);

		return param;
	}

	public override string ToString()
	{
		return $"NickName : {NickName}\n" +
			$"SessionDurationSec : {SessionDurationSec}\n" +
			$"Stage : {Stage}\n" +
			$"ItemScore : {ItemScore}\n" +
			$"KillScore : {KillScore}\n" +
			$"TotalScore : {TotalScore}";
	}
}

public static class UserData
{
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
				Debug.Log("<color=#00FF00>세션 데이터 삽입 성공 : " + callback.GetInDate() + "</color>");
			}
			else
			{
				Debug.LogError("세션 데이터 삽입 실패 : " + callback);
			}

			onCompleted?.Invoke(callback);
		});
	}

	/// <summary>
	/// inDate로 나의 단일 세션 데이터를 동기 조회합니다. 실패 또는 미존재 시 null 반환.
	/// </summary>
	public static PlayerSessionRecord GetSessionByInDate(string tableName, string inDate)
	{
		if (string.IsNullOrEmpty(tableName))
			return null;

		if (string.IsNullOrEmpty(inDate))
			return null;

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
			onCompleted?.Invoke(false, null, null);
			return;
		}

		if (string.IsNullOrEmpty(inDate))
		{
			onCompleted?.Invoke(false, null, null);
			return;
		}

		Backend.GameData.GetMyData(tableName, inDate, callback =>
		{
			if (callback.IsSuccess() == false)
			{
				Debug.LogError(callback.ToString());
				onCompleted?.Invoke(false, null, callback);
				return;
			}

			LitJson.JsonData row = callback.GetFlattenJSON()["row"];
			if (row == null)
			{
				Debug.Log("데이터가 존재하지 않습니다");
				onCompleted?.Invoke(false, null, callback);
				return;
			}

			PlayerSessionRecord record = new PlayerSessionRecord(row);
			onCompleted?.Invoke(true, record, callback);
		});
	}

	private static Param BuildUpdateParam(PlayerSessionRecord record)
	{
		Param param = new Param();
		param.Add("SessionDurationSec", record.SessionDurationSec);
		param.Add("Stage", record.Stage);
		param.Add("ItemScore", record.ItemScore);
		param.Add("KillScore", record.KillScore);
		param.Add("TotalScore", record.TotalScore);
		return param;
	}

	/// <summary>
	/// 내 소유 데이터의 특정 inDate 레코드를 비동기 업데이트합니다.
	/// </summary>
	public static void UpdateSessionAsync(string tableName, string inDate, PlayerSessionRecord record, Action<BackendReturnObject> onCompleted)
	{
		if (string.IsNullOrEmpty(tableName))
		{
			onCompleted?.Invoke(null);
			return;
		}

		if (string.IsNullOrEmpty(inDate))
		{
			onCompleted?.Invoke(null);
			return;
		}

		Param param = BuildUpdateParam(record);
		Backend.GameData.UpdateV2(tableName, inDate, Backend.UserInDate, param, callback =>
		{
			if (callback.IsSuccess() == false)
			{
				Debug.LogError("세션 데이터 업데이트 실패 : " + callback);
			}
			else
			{
				Debug.Log("<color=#00FF00>세션 데이터 업데이트 성공 : " + inDate + "</color>");
			}

			onCompleted?.Invoke(callback);
		});
	}

	/// <summary>
	/// 특정 소유자(ownerInDate)의 레코드를 비동기 업데이트합니다.
	/// </summary>
	public static void UpdateSessionOfOwnerAsync(string tableName, string inDate, string ownerInDate, PlayerSessionRecord record, Action<BackendReturnObject> onCompleted)
	{
		if (string.IsNullOrEmpty(tableName))
		{
			onCompleted?.Invoke(null);
			return;
		}

		if (string.IsNullOrEmpty(inDate))
		{
			onCompleted?.Invoke(null);
			return;
		}

		if (string.IsNullOrEmpty(ownerInDate))
		{
			onCompleted?.Invoke(null);
			return;
		}

		Param param = BuildUpdateParam(record);
		Backend.GameData.UpdateV2(tableName, inDate, ownerInDate, param, callback =>
		{
			if (callback.IsSuccess() == false)
			{
				Debug.LogError("세션 데이터 업데이트 실패 : " + callback);
			}

			onCompleted?.Invoke(callback);
		});
	}
}


