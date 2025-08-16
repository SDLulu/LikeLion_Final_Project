
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
	[SerializeField] private Image _rankImage;

	private void Awake()
	{
		SetColorByRank(4);
	}

	public void UpdateSlot(int rank, string nickName, int sessionDurationSec, string stage, int totalScore)
	{
		SetText(_rankText, rank.ToString());
		SetText(_nickNameText, nickName);
		SetText(_sessionSecText, sessionDurationSec.ToString());
		SetText(_stageText, stage);
		SetText(_totalScoreText, totalScore.ToString());
		
		// 랭킹에 따라 색상 자동 설정
		SetColorByRank(rank);
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
	
	private void SetColorByRank(int rank)
	{
		Color rankColor;
		
		if (rank == 1)
			rankColor = new Color(0.4f, 0.9f, 1.0f, 1.0f);
		else if (rank == 2)
			rankColor = new Color(0.6f, 0.7f, 1.0f, 1.0f);
		else if (rank == 3)
			rankColor = new Color(0.3f, 0.5f, 0.8f, 1.0f);
		else
			rankColor = new Color(0.7f, 0.8f, 0.9f, 1.0f);
		
		_rankImage.color = rankColor;
	}
}
