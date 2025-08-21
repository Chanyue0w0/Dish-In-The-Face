using UnityEngine;
using TMPro; // TextMeshPro

public class TimeLimitCounter : MonoBehaviour
{
	[Header("Setting")]
	[SerializeField] private float roundTime = 60f;          // 預設回合時間 (秒)
	[SerializeField] private float maxRoundTime = 300f;      // 最大可設定時間 (預設 5 分鐘)
	[SerializeField] private GameObject gameOverPanel;       // Game Over 面板
	[SerializeField] private TextMeshProUGUI timerText;      // 顯示剩餘時間 "m:ss"

	private float remainingTime; // 剩餘時間
	private bool isCounting = false; // 是否在計時
	private bool isPaused = false;   // 是否暫停中

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
	/// 開始倒數
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
	/// 暫停倒數
	/// </summary>
	public void PauseCountdown()
	{
		if (isCounting)
			isPaused = true;
	}

	/// <summary>
	/// 繼續倒數
	/// </summary>
	public void ResumeCountdown()
	{
		if (isCounting && isPaused)
			isPaused = false;
	}

	/// <summary>
	/// 取得剩下時間（秒）
	/// </summary>
	public float GetRemainingTime()
	{
		return remainingTime;
	}

	/// <summary>
	/// 設定回合時間（秒）
	/// </summary>
	public void SetRoundTime(float time)
	{
		roundTime = Mathf.Clamp(time, 1f, maxRoundTime); // 介於 1 秒 ~ 最大時間
		remainingTime = roundTime;
		UpdateTimerText();
	}

	/// <summary>
	/// 設定最大時間（秒）
	/// </summary>
	public void SetMaxRoundTime(float time)
	{
		maxRoundTime = Mathf.Max(1f, time);
		// 如果 roundTime 超過新的上限，就自動調整
		roundTime = Mathf.Min(roundTime, maxRoundTime);
		remainingTime = roundTime;
		UpdateTimerText();
	}

	/// <summary>
	/// 更新 UI 顯示 "m:ss"
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
	/// 倒數結束觸發 Game Over
	/// </summary>
	private void TriggerGameOver()
	{
		Debug.Log("Time is up! Game Over!");
		if (gameOverPanel != null)
			gameOverPanel.SetActive(true);
	}
}
