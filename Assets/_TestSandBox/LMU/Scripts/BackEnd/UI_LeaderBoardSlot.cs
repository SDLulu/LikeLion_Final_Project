
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_LeaderBoardSlot : MonoBehaviour
{
	[Header("인스펙터 참조")]
	[SerializeField] private TMP_Text _rankText;
	[SerializeField] private TMP_Text _nickNameText;
	[SerializeField] private TMP_Text _sessionSecText;
	[SerializeField] private TMP_Text _stageText;
	[SerializeField] private TMP_Text _totalScoreText;

	/// <summary>
	/// 슬롯 데이터 표시
	/// </summary>
	public void UpdateSlot(int rank, string nickName, int sessionDurationSec, string stage, int totalScore)
	{
		SetText(_rankText, rank.ToString());
		SetText(_nickNameText, nickName);
		SetText(_sessionSecText, sessionDurationSec.ToString());
		SetText(_stageText, stage);
		SetText(_totalScoreText, totalScore.ToString());
	}

	public void ClearSlot()
	{
		SetText(_rankText, string.Empty);
		SetText(_nickNameText, string.Empty);
		SetText(_sessionSecText, string.Empty);
		SetText(_stageText, string.Empty);
		SetText(_totalScoreText, string.Empty);
	}

	private void SetText(TMP_Text text, string value)
	{
		if (text != null)
			text.text = value;
	}
}
