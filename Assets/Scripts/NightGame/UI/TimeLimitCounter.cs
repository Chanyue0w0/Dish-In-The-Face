using UnityEngine;
using TMPro; // TextMeshPro

public class TimeLimitCounter : MonoBehaviour
{
	[Header("Setting")]
	[SerializeField] private float roundTime = 60f;          // �w�]�^�X�ɶ� (��)
	[SerializeField] private float maxRoundTime = 300f;      // �̤j�i�]�w�ɶ� (�w�] 5 ����)
	[SerializeField] private GameObject gameOverPanel;       // Game Over ���O
	[SerializeField] private TextMeshProUGUI timerText;      // ��ܳѾl�ɶ� "m:ss"

	private float remainingTime; // �Ѿl�ɶ�
	private bool isCounting = false; // �O�_�b�p��
	private bool isPaused = false;   // �O�_�Ȱ���

	private void Start()
	{
		remainingTime = roundTime;
		if (gameOverPanel != null)
			gameOverPanel.SetActive(false);

		UpdateTimerText();
	}

	private void Update()
	{
		if (isCounting && !isPaused)
		{
			remainingTime -= Time.deltaTime;
			if (remainingTime <= 0f)
			{
				remainingTime = 0f;
				isCounting = false;
				TriggerGameOver();
			}

			UpdateTimerText();
		}
	}

	/// <summary>
	/// �}�l�˼�
	/// </summary>
	public void StartCountdown()
	{
		remainingTime = roundTime;
		isCounting = true;
		isPaused = false;
		if (gameOverPanel != null)
			gameOverPanel.SetActive(false);

		UpdateTimerText();
	}

	/// <summary>
	/// �Ȱ��˼�
	/// </summary>
	public void PauseCountdown()
	{
		if (isCounting)
			isPaused = true;
	}

	/// <summary>
	/// �~��˼�
	/// </summary>
	public void ResumeCountdown()
	{
		if (isCounting && isPaused)
			isPaused = false;
	}

	/// <summary>
	/// ���o�ѤU�ɶ��]��^
	/// </summary>
	public float GetRemainingTime()
	{
		return remainingTime;
	}

	/// <summary>
	/// �]�w�^�X�ɶ��]��^
	/// </summary>
	public void SetRoundTime(float time)
	{
		roundTime = Mathf.Clamp(time, 1f, maxRoundTime); // ���� 1 �� ~ �̤j�ɶ�
		remainingTime = roundTime;
		UpdateTimerText();
	}

	/// <summary>
	/// �]�w�̤j�ɶ��]��^
	/// </summary>
	public void SetMaxRoundTime(float time)
	{
		maxRoundTime = Mathf.Max(1f, time);
		// �p�G roundTime �W�L�s���W���A�N�۰ʽվ�
		roundTime = Mathf.Min(roundTime, maxRoundTime);
		remainingTime = roundTime;
		UpdateTimerText();
	}

	/// <summary>
	/// ��s UI ��� "m:ss"
	/// </summary>
	private void UpdateTimerText()
	{
		if (timerText != null)
		{
			int minutes = Mathf.FloorToInt(remainingTime / 60f);
			int seconds = Mathf.FloorToInt(remainingTime % 60f);
			timerText.text = $"{minutes}:{seconds:00}";
		}
	}

	/// <summary>
	/// �˼Ƶ���Ĳ�o Game Over
	/// </summary>
	private void TriggerGameOver()
	{
		Debug.Log("Time is up! Game Over!");
		if (gameOverPanel != null)
			gameOverPanel.SetActive(true);
		RoundManager.Instance.GameOver();
	}
}
