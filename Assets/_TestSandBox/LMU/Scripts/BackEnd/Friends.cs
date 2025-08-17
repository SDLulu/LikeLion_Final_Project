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

                    Debug.Log($"<color=green>친구 목록 조회 성공! 총 {friendDataList.Count}명의 친구</color>");
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

                    Debug.Log($"<color=green>받은 친구 요청 조회 성공! 총 {requestDataList.Count}개의 요청</color>");
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

                    Debug.Log($"<color=green>보낸 친구 요청 조회 성공! 총 {requestDataList.Count}개의 요청</color>");
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

                    Debug.Log($"<color=green>유저 '{targetNickname}'를 찾았습니다. 친구신청을 보냅니다...</color>");

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

        // 1. 먼저 받은 친구 요청 목록을 조회
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
                
                // 2. 각 요청을 순차적으로 수락
                AcceptFriendRequestsSequentially(requestData, 0, 0, onSuccess, onFail);
            },
            onFail: onFail
        );
    }

    /// <summary>
    /// 친구 요청을 순차적으로 수락하는 내부 함수 (재귀 호출)
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

        // Backend.Friend.AcceptFriend API 호출 (inDate 사용)
        Backend.Friend.AcceptFriend(request.InDate, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log($"<color=green>'{request.NickName}' 님의 친구 요청 수락 성공!</color>");
                
                // 다음 요청 처리 (성공 카운트 증가)
                AcceptFriendRequestsSequentially(requests, currentIndex + 1, successCount + 1, onSuccess, onFail);
            }
            else
            {
                Debug.LogWarning($"'{request.NickName}' 님의 친구 요청 수락 실패: {callback.GetStatusCode()} - {callback.GetErrorMessage()}");
                
                // 실패해도 다음 요청 계속 처리 (성공 카운트 증가하지 않음)
                AcceptFriendRequestsSequentially(requests, currentIndex + 1, successCount, onSuccess, onFail);
            }
        });
    }
}
