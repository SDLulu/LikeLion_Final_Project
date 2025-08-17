using System;
using BackEnd;
using UnityEngine;
using LMCore;

public enum E_FriendSlotForm
{
    Friend,     // 친구 목록
    Request,    // 보낸 친구 요청
    Response    // 받은 친구 요청
}

public class Friends : BaseManager<Friends>
{
    public class FriendData
    {
        public int ProfileImageID;
        public string NickName;
        public string GameStates;
        public string InDate;
    }

    /// <summary>
    /// 모든 친구 목록을 조회하는 함수
    /// </summary>
    public void GetFriendList(Action<FriendData[]> onSuccess = null, Action<string> onFail = null)
    {
        if (BackEndWorkFlow.IsFakeClient)
        {
            Debug.Log("페이크 클라이언트 모드에서는 친구 목록을 조회할 수 없습니다.");
            onFail?.Invoke("오프라인 모드에서는 친구 목록을 조회할 수 없습니다.");
            return;
        }

        Debug.Log("<color=yellow>친구 목록을 조회합니다...</color>");

        Backend.Friend.GetFriendList(callback =>
        {
            if (callback.IsSuccess())
            {
                try
                {
                    var friendDataList = new System.Collections.Generic.List<FriendData>();
                    
                    // JSON 응답에서 친구 목록 파싱
                    LitJson.JsonData json = callback.GetReturnValuetoJSON();
                    
                    if (json["rows"] != null)
                    {
                        for (int i = 0; i < json["rows"].Count; i++)
                        {
                            var row = json["rows"][i];
                            // DynamoDB 형태의 JSON 파싱
                            string nickname = GetDynamoDBStringValue(row, "nickname", "알 수 없음");
                            string inDate = GetDynamoDBStringValue(row, "inDate", "");
                            
                            var friendData = new FriendData
                            {
                                ProfileImageID = UnityEngine.Random.Range(0, 5), // 임시로 랜덤 프로필 이미지
                                NickName = nickname,
                                GameStates = DetermineGameState(row),
                                InDate = inDate
                            };
                            friendDataList.Add(friendData);
                        }
                    }

                    Debug.Log($"친구 목록 조회 성공 - 총 {friendDataList.Count}명의 친구</color>");
                    onSuccess?.Invoke(friendDataList.ToArray());
                }
                catch (Exception ex)
                {
                    Debug.LogError($"친구 목록 파싱 중 오류 발생: {ex.Message}");
                    onFail?.Invoke("친구 목록을 처리하는 중 오류가 발생했습니다.");
                }
            }
            else
            {
                string errorMessage = $"친구 목록 조회 실패: {callback.GetStatusCode()} - {callback.GetErrorMessage()}";
                Debug.LogError(errorMessage);
                onFail?.Invoke("친구 목록을 조회할 수 없습니다.");
            }
        });
    }

    /// <summary>
    /// 받은 친구 요청 목록을 조회하는 함수
    /// </summary>
    public void GetReceivedFriendRequests(Action<FriendData[]> onSuccess = null, Action<string> onFail = null)
    {
        if (BackEndWorkFlow.IsFakeClient)
        {
            Debug.Log("페이크 클라이언트 모드에서는 친구 요청을 조회할 수 없습니다.");
            onFail?.Invoke("오프라인 모드에서는 친구 요청을 조회할 수 없습니다.");
            return;
        }

        Backend.Friend.GetReceivedRequestList(callback =>
        {
            if (callback.IsSuccess())
            {
                try
                {
                    var requestDataList = new System.Collections.Generic.List<FriendData>();
                    
                    LitJson.JsonData json = callback.GetReturnValuetoJSON();
                    
                    if (json["rows"] != null)
                    {
                        for (int i = 0; i < json["rows"].Count; i++)
                        {
                            var row = json["rows"][i];
                            
                            // DynamoDB 형태의 JSON 파싱
                            string nickname = GetDynamoDBStringValue(row, "nickname", "알 수 없음");
                            string inDate = GetDynamoDBStringValue(row, "inDate", "");
                            
                            var requestData = new FriendData
                            {
                                ProfileImageID = UnityEngine.Random.Range(0, 5),
                                NickName = nickname,
                                GameStates = "요청 대기중",
                                InDate = inDate
                            };
                            requestDataList.Add(requestData);
                        }
                    }

                    Debug.Log($"받은 친구 요청 조회 성공! 총 {requestDataList.Count}개의 요청");
                    onSuccess?.Invoke(requestDataList.ToArray());
                }
                catch (Exception ex)
                {
                    Debug.LogError($"받은 친구 요청 파싱 중 오류 발생: {ex.Message}");
                    onFail?.Invoke("받은 친구 요청을 처리하는 중 오류가 발생했습니다.");
                }
            }
            else
            {
                string errorMessage = $"받은 친구 요청 조회 실패: {callback.GetStatusCode()} - {callback.GetErrorMessage()}";
                Debug.LogError(errorMessage);
                onFail?.Invoke("받은 친구 요청을 조회할 수 없습니다.");
            }
        });
    }

    /// <summary>
    /// 보낸 친구 요청 목록을 조회하는 함수
    /// </summary>
    public void GetSentFriendRequests(Action<FriendData[]> onSuccess = null, Action<string> onFail = null)
    {
        if (BackEndWorkFlow.IsFakeClient)
        {
            Debug.Log("페이크 클라이언트 모드에서는 친구 요청을 조회할 수 없습니다.");
            onFail?.Invoke("오프라인 모드에서는 친구 요청을 조회할 수 없습니다.");
            return;
        }

        Backend.Friend.GetSentRequestList(callback =>
        {
            if (callback.IsSuccess())
            {
                try
                {
                    var requestDataList = new System.Collections.Generic.List<FriendData>();
                    
                    LitJson.JsonData json = callback.GetReturnValuetoJSON();
                    
                    
                    if (json != null && json["rows"] != null)
                    {
                        for (int i = 0; i < json["rows"].Count; i++)
                        {
                            var row = json["rows"][i];
                            
                            // DynamoDB 형태의 JSON 파싱
                            string nickname = GetDynamoDBStringValue(row, "nickname", "알 수 없음");
                            string inDate = GetDynamoDBStringValue(row, "inDate", "");
                            
                            var requestData = new FriendData
                            {
                                ProfileImageID = UnityEngine.Random.Range(0, 5),
                                NickName = nickname,
                                GameStates = "요청 전송됨",
                                InDate = inDate
                            };
                            requestDataList.Add(requestData);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("JSON 응답에 rows 데이터가 없습니다.");
                    }

                    Debug.Log($"보낸 친구 요청 조회 성공! 총 {requestDataList.Count}개의 요청");
                    onSuccess?.Invoke(requestDataList.ToArray());
                }
                catch (Exception ex)
                {
                    Debug.LogError($"보낸 친구 요청 파싱 중 오류 발생: {ex.Message}");
                    onFail?.Invoke("보낸 친구 요청을 처리하는 중 오류가 발생했습니다.");
                }
            }
            else
            {
                string errorMessage = $"보낸 친구 요청 조회 실패: {callback.GetStatusCode()} - {callback.GetErrorMessage()}";
                Debug.LogError(errorMessage);
                onFail?.Invoke("보낸 친구 요청을 조회할 수 없습니다.");
            }
        });
    }

    /// <summary>
    /// 친구의 게임 상태를 결정하는 내부 함수
    /// </summary>
    private string DetermineGameState(LitJson.JsonData friendRow)
    {
        return "Offline";
    }

    /// <summary>
    /// DynamoDB 형태의 JSON에서 문자열 값을 안전하게 추출
    /// </summary>
    private string GetDynamoDBStringValue(LitJson.JsonData row, string fieldName, string defaultValue = "")
    {
        try
        {
            if (row != null && row[fieldName] != null && row[fieldName]["S"] != null)
            {
                return row[fieldName]["S"].ToString();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"DynamoDB 필드 '{fieldName}' 파싱 실패: {ex.Message}");
        }
        
        return defaultValue;
    }

    /// <summary>
    /// 닉네임으로 유저를 찾아서 친구신청을 보내는 함수
    /// </summary>
    public void SendFriendRequestByNickname(string targetNickname, Action onSuccess = null, Action<string> onFail = null)
    {
        if (string.IsNullOrEmpty(targetNickname))
        {
            Debug.LogError("닉네임이 비어있습니다.");
            onFail?.Invoke("닉네임이 비어있습니다.");
            return;
        }

        if (BackEndWorkFlow.IsFakeClient)
        {
            Debug.Log("페이크 클라이언트 모드에서는 친구신청을 할 수 없습니다.");
            onFail?.Invoke("오프라인 모드에서는 친구신청을 할 수 없습니다.");
            return;
        }

        Debug.Log($"<color=yellow>'{targetNickname}' 닉네임으로 유저를 검색합니다...</color>");

        // 1. 닉네임으로 유저의 inDate 조회
        Backend.Social.GetUserInfoByNickNameV2(targetNickname, callback =>
        {
            if (callback.IsSuccess())
            {
                try
                {
                    // JSON에서 inDate 추출
                    LitJson.JsonData json = callback.GetReturnValuetoJSON()["row"];
                    
                    if (json == null)
                    {
                        Debug.LogError("유저 정보를 찾을 수 없습니다.");
                        onFail?.Invoke("유저 정보를 찾을 수 없습니다.");
                        return;
                    }
                    
                    // DynamoDB 형태의 JSON 파싱
                    string targetInDate = GetDynamoDBStringValue(json, "inDate", "");
                    
                    if (string.IsNullOrEmpty(targetInDate))
                    {
                        Debug.LogError("유저 정보에서 inDate를 찾을 수 없습니다.");
                        onFail?.Invoke("유저 정보를 찾을 수 없습니다.");
                        return;
                    }

                    Debug.Log($"유저 '{targetNickname}'를 찾았습니다. 친구신청을 보냅니다...");

                    // 2. 찾은 유저에게 친구신청 보내기
                    SendFriendRequest(targetInDate, targetNickname, onSuccess, onFail);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"유저 정보 파싱 중 오류 발생: {ex.Message}");
                    onFail?.Invoke("유저 정보를 처리하는 중 오류가 발생했습니다.");
                }
            }
            else
            {
                string errorMessage = $"유저를 찾을 수 없습니다: {callback.GetStatusCode()} - {callback.GetErrorMessage()}";
                Debug.LogError(errorMessage);
                onFail?.Invoke($"'{targetNickname}' 닉네임을 가진 유저를 찾을 수 없습니다.");
            }
        });
    }

    /// <summary>
    /// inDate를 사용해서 실제 친구신청을 보내는 내부 함수
    /// </summary>
    private void SendFriendRequest(string targetInDate, string targetNickname, Action onSuccess, Action<string> onFail)
    {
        Backend.Friend.RequestFriend(targetInDate, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log($"<color=green>'{targetNickname}' 님에게 친구신청을 성공적으로 보냈습니다!</color>");
                onSuccess?.Invoke();
            }
            else
            {
                string errorMessage = $"친구신청 실패: {callback.GetStatusCode()} - {callback.GetErrorMessage()}";
                Debug.LogError(errorMessage);

                // 에러 코드에 따른 사용자 친화적 메시지
                string userMessage = GetFriendlyErrorMessage(callback.GetStatusCode());
                onFail?.Invoke(userMessage);
            }
        });
    }

    /// <summary>
    /// 에러 코드를 사용자 친화적 메시지로 변환
    /// </summary>
    private string GetFriendlyErrorMessage(string statusCode)
    {
        return statusCode switch
        {
            "409" => "이미 친구이거나 친구신청이 진행 중입니다.",
            "404" => "존재하지 않는 유저입니다.",
            "400" => "잘못된 요청입니다.",
            _ => "친구신청 중 오류가 발생했습니다."
        };
    }

    /// <summary>
    /// 받은 모든 친구 요청을 수락하는 함수
    /// </summary>
    public void AcceptAllFriendRequests(Action<int> onSuccess = null, Action<string> onFail = null)
    {
        if (BackEndWorkFlow.IsFakeClient)
        {
            Debug.Log("페이크 클라이언트 모드에서는 친구 요청을 수락할 수 없습니다.");
            onFail?.Invoke("오프라인 모드에서는 친구 요청을 수락할 수 없습니다.");
            return;
        }

        Debug.Log("<color=yellow>받은 친구 요청 목록을 조회하여 모두 수락합니다...</color>");

        GetReceivedFriendRequests(
            onSuccess: (requestData) =>
            {
                if (requestData.Length == 0)
                {
                    Debug.Log("수락할 친구 요청이 없습니다.");
                    onSuccess?.Invoke(0);
                    return;
                }

                Debug.Log($"총 {requestData.Length}개의 친구 요청을 수락 처리 시작");
                AcceptFriendRequestsSequentially(requestData, 0, 0, onSuccess, onFail);
            },
            onFail: onFail
        );
    }

    /// <summary>
    /// 친구 요청을 순차적으로 수락 - 재귀
    /// </summary>
    private void AcceptFriendRequestsSequentially(FriendData[] requests, int currentIndex, int successCount, Action<int> onSuccess, Action<string> onFail)
    {
        if (currentIndex >= requests.Length)
        {
            // 모든 요청 처리 완료
            Debug.Log($"<color=green>친구 요청 처리 완료! 총 {successCount}개 수락 성공</color>");
            onSuccess?.Invoke(successCount);
            return;
        }

        var request = requests[currentIndex];
        Debug.Log($"친구 요청 수락 중: {request.NickName} ({currentIndex + 1}/{requests.Length})");

        Backend.Friend.AcceptFriend(request.InDate, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log($"<color=green>'{request.NickName}' 님의 친구 요청 수락 성공!</color>");
                AcceptFriendRequestsSequentially(requests, currentIndex + 1, successCount + 1, onSuccess, onFail);
            }
            else
            {
                Debug.LogWarning($"'{request.NickName}' 님의 친구 요청 수락 실패: {callback.GetStatusCode()} - {callback.GetErrorMessage()}");
                AcceptFriendRequestsSequentially(requests, currentIndex + 1, successCount, onSuccess, onFail);
            }
        });
    }

	/// <summary>
	/// 닉네임으로 사용자 inDate를 조회
	/// </summary>
	private void ResolveUserInDateByNickname(string nickname, Action<string> onSuccess, Action<string> onFail)
	{
		if (string.IsNullOrEmpty(nickname))
		{
			onFail?.Invoke("닉네임이 비어있습니다.");
			return;
		}

		Backend.Social.GetUserInfoByNickNameV2(nickname, callback =>
		{
			if (callback.IsSuccess())
			{
				try
				{
					var json = callback.GetReturnValuetoJSON()["row"];
					string resolvedInDate = GetDynamoDBStringValue(json, "inDate", "");
					if (string.IsNullOrEmpty(resolvedInDate))
					{
						onFail?.Invoke("대상 inDate를 찾을 수 없습니다.");
						return;
					}
					onSuccess?.Invoke(resolvedInDate);
				}
				catch (System.Exception ex)
				{
					Debug.LogError($"inDate 조회 파싱 오류: {ex.Message}");
					onFail?.Invoke("대상 정보를 처리하는 중 오류가 발생했습니다.");
				}
			}
			else
			{
				onFail?.Invoke($"대상 유저 정보를 찾을 수 없습니다. 코드:{callback.GetStatusCode()}");
			}
		});
	}

	/// <summary>
	/// 현재 룸 ID를 얻습니다.
	/// </summary>
	private bool TryGetCurrentRoomId(out string roomId)
	{
		roomId = null;
		var lobbyM = LobbyManager.Inst;
		if (lobbyM == null)
		{
			return false;
		}
		if (lobbyM.NetRunner == null)
		{
			return false;
		}
		if (lobbyM.NetRunner.IsRunning == false)
		{
			return false;
		}
		roomId = lobbyM.NetRunner.SessionInfo?.Name;
		if (string.IsNullOrEmpty(roomId))
		{
			return false;
		}
		return true;
	}

	/// <summary>
	/// 초대 페이로드를 200바이트 이하가 되도록 축약 JSON으로 생성합니다.
	/// </summary>
	private bool TryBuildInvitePayloadJson(string roomId, string inviterName, out string jsonPayload, int maxBytes = 200)
	{
		var payload = new GameInvitePayload(roomId, inviterName);
		jsonPayload = payload.ToCompactJson(includeInviterName: true, inviterNameMaxLen: 20);
		int byteLen = System.Text.Encoding.UTF8.GetByteCount(jsonPayload);
		if (byteLen > maxBytes)
		{
			jsonPayload = payload.ToCompactJson(includeInviterName: false);
			byteLen = System.Text.Encoding.UTF8.GetByteCount(jsonPayload);
			if (byteLen > maxBytes)
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// 쪽지 전송 호출
	/// </summary>
	private void SendInviteMessage(string targetInDate, string jsonPayload, string friendNickname, Action onSuccess, Action<string> onFail)
	{
		Backend.Message.SendMessage(targetInDate, jsonPayload, callback =>
		{
			if (callback.IsSuccess())
			{
				Debug.Log($"<color=green>'{friendNickname}' 님에게 게임 초대를 성공적으로 보냈습니다!</color>");
				onSuccess?.Invoke();
			}
			else
			{
				string status = callback.GetStatusCode();
				string serverMsg = callback.GetErrorMessage();
				Debug.LogError($"게임 초대 전송 실패: {status} - {serverMsg}");
				if (status == "412")
				{
					onFail?.Invoke("초대 전송 조건을 만족하지 않습니다. (상대 유효성/친구 여부/차단 여부/본문 제한)");
				}
				else
				{
					onFail?.Invoke("게임 초대를 보내는 중 오류가 발생했습니다.");
				}
			}
		});
	}

	/// <summary>
	/// 닉네임으로 inDate를 검증 후 초대를 전송
	/// </summary>
	public void SendGameInviteByNickname(string friendNickname, Action onSuccess = null, Action<string> onFail = null)
	{
		if (string.IsNullOrEmpty(friendNickname))
		{
			onFail?.Invoke("닉네임이 비어있습니다.");
			return;
		}
		if (BackEndWorkFlow.IsFakeClient)
		{
			onFail?.Invoke("오프라인 모드에서는 게임 초대를 보낼 수 없습니다.");
			return;
		}
		if (TryGetCurrentRoomId(out string roomId) == false)
		{
			Debug.LogError("현재 게임 룸에 접속되어 있지 않습니다.");
			onFail?.Invoke("게임 룸에 접속된 상태에서만 초대할 수 있습니다.");
			return;
		}
		string inviterName = BackEndWorkFlow.NickName ?? "";
		ResolveUserInDateByNickname(friendNickname,
			onSuccess: targetInDate =>
			{
				if (TryBuildInvitePayloadJson(roomId, inviterName, out string jsonPayload) == false)
				{
					onFail?.Invoke("초대 본문이 너무 깁니다. 콘솔 설정을 늘리거나 내용을 줄여주세요.");
					return;
				}
				SendInviteMessage(targetInDate, jsonPayload, friendNickname, onSuccess, onFail);
			},
			onFail: err =>
			{
				Debug.LogError(err);
				onFail?.Invoke("대상 유저 정보를 찾을 수 없습니다.");
			});
	}

	/// <summary>
	/// inDate가 이미 있는 경우 바로 초대를 보냅니다.
	/// </summary>
	public void SendGameInviteWithInDate(string friendInDate, string friendNickname, Action onSuccess = null, Action<string> onFail = null)
	{
		if (string.IsNullOrEmpty(friendInDate))
		{
			onFail?.Invoke("친구 정보가 유효하지 않습니다.");
			return;
		}
		if (BackEndWorkFlow.IsFakeClient)
		{
			onFail?.Invoke("오프라인 모드에서는 게임 초대를 보낼 수 없습니다.");
			return;
		}
		if (TryGetCurrentRoomId(out string roomId) == false)
		{
			Debug.LogError("현재 게임 룸에 접속되어 있지 않습니다.");
			onFail?.Invoke("게임 룸에 접속된 상태에서만 초대할 수 있습니다.");
			return;
		}
		string inviterName = BackEndWorkFlow.NickName ?? "";
		if (TryBuildInvitePayloadJson(roomId, inviterName, out string jsonPayload) == false)
		{
			onFail?.Invoke("초대 본문이 너무 깁니다. 콘솔 설정을 늘리거나 내용을 줄여주세요.");
			return;
		}
		SendInviteMessage(friendInDate, jsonPayload, friendNickname, onSuccess, onFail);
	}

	/// <summary>
	/// [호환] 기존 시그니처. 내부적으로 WithInDate로 위임합니다.
	/// </summary>
	public void SendGameInvite(string friendInDate, string friendNickname, Action onSuccess = null, Action<string> onFail = null)
	{
		SendGameInviteWithInDate(friendInDate, friendNickname, onSuccess, onFail);
	}
}
