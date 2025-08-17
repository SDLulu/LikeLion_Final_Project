using System;
using BackEnd;
using UnityEngine;
using LMCore;
using Fusion;

public class GameInviteManager : BaseManager<GameInviteManager>
{
	[Header("디버그용")]
	[SerializeField] private bool _isNotificationConnected = false;
	[SerializeField] private float _messageCheckInterval = 3.0f;
	
	public bool IsNotificationConnected => _isNotificationConnected;
	
	// 게임 초대 이벤트
	public event Action<GameInvitePayload> OnGameInviteReceived;

	// 재표시 방지를 위한 처리된 메시지 캐시
	private readonly System.Collections.Generic.HashSet<string> _processedMessageInDates = new System.Collections.Generic.HashSet<string>();
	
	private void OnDestroy()
	{
		OnGameInviteReceived = null;
	}
	
	/// <summary>
	/// 뒤끝 메시지 확인 시작
	/// </summary>
	public void ConnectNotification()
	{
		if (BackEndWorkFlow.IsFakeClient)
			return;
		
		if (_isNotificationConnected)
		{
			Debug.Log("이미 메시지 확인이 활성화되어 있습니다.");
			return;
		}
		
		_isNotificationConnected = true;
		
		_ = CheckMessagesRoutine();
		Debug.Log($"뒤끝 메시지 확인 시작 완료 - {_messageCheckInterval}초 마다 확인");
	}
	
	/// <summary>
	/// 주기적 메시지 수신 확인
	/// </summary>
	private async Awaitable CheckMessagesRoutine()
	{
		while (_isNotificationConnected)
		{
			await Awaitable.WaitForSecondsAsync(_messageCheckInterval);
			
			if (_isNotificationConnected == false)
				break;
				
			CheckForNewMessages();
		}
	}
	
	/// <summary>
	/// 새로운 메시지 확인 (받은 메시지 목록 우선, 불가 시 보낸 메시지로 폴백)
	/// </summary>
	private void CheckForNewMessages()
	{
		FetchReceivedMessageList(callback =>
		{
			if (callback.IsSuccess())
			{
				try
				{
					var json = callback.FlattenRows();
					if (json != null && json.Count > 0)
					{
						for (int i = 0; i < json.Count; i++)
						{
							var messageRow = json[i];
							bool isRead = messageRow["isRead"].ToString() == "true";
							string inDate = messageRow["inDate"].ToString();
							
							// 이미 처리한 메시지는 스킵
							if (_processedMessageInDates.Contains(inDate))
							{
								continue;
							}
							
							if (isRead == false)
							{
								// 단건 조회로 읽음 처리 및 상세 내용 확보
								FetchReceivedMessage(inDate, readCallback =>
								{
									if (readCallback.IsSuccess())
									{
										var row = readCallback.GetFlattenJSON()["row"];
										string content = row["content"].ToString();
										string senderNickname = row["senderNickname"].ToString();
										Debug.Log($"<color=cyan>새 메시지 발견: {senderNickname} - {content}</color>");
										
										// 재표시 방지를 위해 즉시 처리 캐시에 등록
										_processedMessageInDates.Add(inDate);
										
										// 문서에 따라 받은/보낸 쪽지 삭제 시도 (있으면 사용)
										TryDeleteMessage(inDate);
										
										ProcessGameInvite(content);
									}
								});
							}
						}
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"메시지 처리 중 오류 발생: {ex.Message}");
				}
			}
			else
			{
				Debug.LogWarning($"메시지 목록 조회 실패: {callback.GetStatusCode()} - {callback.GetErrorMessage()}");
			}
		});
	}

	private void FetchReceivedMessageList(Action<BackendReturnObject> onResult)
	{
		var msgObj = Backend.Message;
		var msgType = msgObj.GetType();

		var method = msgType.GetMethod("GetReceivedMessageList", System.Type.EmptyTypes);
		if (method != null)
		{
			var broObj = method.Invoke(msgObj, null);
			onResult?.Invoke(broObj as BackendReturnObject);
			return;
		}

		var sentBro = Backend.Message.GetSentMessageList();
		onResult?.Invoke(sentBro);
	}

	private void FetchReceivedMessage(string inDate, Action<BackendReturnObject> onResult)
	{
		var msgObj = Backend.Message;
		var msgType = msgObj.GetType();

		var method = msgType.GetMethod("GetReceivedMessage", new System.Type[] { typeof(string) });
		if (method != null)
		{
			var broObj = method.Invoke(msgObj, new object[] { inDate });
			onResult?.Invoke(broObj as BackendReturnObject);
			return;
		}

		var sentBro = Backend.Message.GetSentMessage(inDate);
		onResult?.Invoke(sentBro);
	}
	
	/// <summary>
	/// 게임 초대 메시지 처리
	/// </summary>
	private void ProcessGameInvite(string jsonPayload)
	{
		var invitePayload = GameInvitePayload.FromJson(jsonPayload);
		
		if (invitePayload == null)
		{
			Debug.LogWarning("유효하지 않은 게임 초대 형식입니다.");
			return;
		}
		
		if (invitePayload.IsValid() == false)
		{
			Debug.LogWarning("만료된 게임 초대입니다.");
			return;
		}
		
		Debug.Log($"<color=green>유효한 게임 초대를 받았습니다! 초대자: {invitePayload.InviterName}, 룸: {invitePayload.RoomID}</color>");
		
		OnGameInviteReceived?.Invoke(invitePayload);
	}
	
	public async void AcceptGameInvite(GameInvitePayload invitePayload)
	{
		if (invitePayload == null)
		{
			Debug.LogError("초대 정보가 유효하지 않습니다.");
			return;
		}
		
		if (invitePayload.IsValid() == false)
		{
			Debug.LogError("만료된 초대입니다.");
			return;
		}
		
		Debug.Log($"<color=yellow>게임 초대를 수락합니다. 룸: {invitePayload.RoomID}</color>");
		
		try
		{
			var lobbyManager = LobbyManager.Inst;
			if (lobbyManager?.NetRunner != null && lobbyManager.NetRunner.IsRunning)
			{
				Debug.Log("현재 게임을 종료하고 새로운 룸으로 이동합니다...");
				await lobbyManager.LeaveGame(isShowWideFade: false);
			}
			
			await lobbyManager.JoinOrCreateLobby(
				isSoloPlay: false,
				mode: GameMode.AutoHostOrClient,
				roomName: invitePayload.RoomID,
				OnEnterLobby: () =>
				{
					Debug.Log($"<color=green>초대받은 룸에 성공적으로 참가했습니다! 룸: {invitePayload.RoomID}</color>");
				},
				OnCancel: () =>
				{
					Debug.LogError("룸 참가가 취소되었습니다.");
				}
			);
		}
		catch (Exception ex)
		{
			Debug.LogError($"게임 초대 수락 중 오류 발생: {ex.Message}");
		}
	}
	
	public void DeclineGameInvite(GameInvitePayload invitePayload)
	{
		if (invitePayload == null)
			return;
	}

	private void TryDeleteMessage(string inDate)
	{
		try
		{
			var msgObj = Backend.Message;
			var msgType = msgObj.GetType();
			// 받은 쪽지 삭제 우선
			var delRecv = msgType.GetMethod("DeleteReceivedMessage", new System.Type[] { typeof(string) });
			if (delRecv != null)
			{
				var broObj = delRecv.Invoke(msgObj, new object[] { inDate }) as BackendReturnObject;
				return;
			}
			// 보낸 쪽지 삭제 폴백
			var delSent = msgType.GetMethod("DeleteSentMessage", new System.Type[] { typeof(string) });
			if (delSent != null)
			{
				var broObj = delSent.Invoke(msgObj, new object[] { inDate }) as BackendReturnObject;
				return;
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning($"쪽지 삭제 시도 실패: {ex.Message}");
		}
	}
}
