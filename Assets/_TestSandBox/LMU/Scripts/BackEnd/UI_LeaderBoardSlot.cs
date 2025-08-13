
using UnityEngine;
using UnityEngine.UI;

public class UI_LeaderBoardSlot : MonoBehaviour
{
	[Header("인스펙터 참조")]
	[SerializeField] private Text _rankText;
	[SerializeField] private Text _nickNameText;
	[SerializeField] private Text _sessionSecText;
	[SerializeField] private Text _stageText;
	[SerializeField] private Text _totalScoreText;

	/// <summary>
	/// 슬롯 데이터 표시
	/// </summary>
	public void SetSlot(int rank, string nickName, int sessionDurationSec, string stage, int totalScore)
	{
		if (_rankText != null)
		{
			_rankText.text = rank.ToString();
		}

		if (_nickNameText != null)
		{
			_nickNameText.text = nickName;
		}

		if (_sessionSecText != null)
		{
			_sessionSecText.text = sessionDurationSec.ToString();
		}

		if (_stageText != null)
		{
			_stageText.text = stage;
		}

		if (_totalScoreText != null)
		{
			_totalScoreText.text = totalScore.ToString();
		}
	}

	/// <summary>
	/// 슬롯 초기화
	/// </summary>
	public void ClearSlot()
	{
		if (_rankText != null)
		{
			_rankText.text = string.Empty;
		}

		if (_nickNameText != null)
		{
			_nickNameText.text = string.Empty;
		}

		if (_sessionSecText != null)
		{
			_sessionSecText.text = string.Empty;
		}

		if (_stageText != null)
		{
			_stageText.text = string.Empty;
		}

		if (_totalScoreText != null)
		{
			_totalScoreText.text = string.Empty;
		}
	}
}
