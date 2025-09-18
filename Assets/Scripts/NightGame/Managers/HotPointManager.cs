using UnityEngine;
using UnityEngine.UI;

public class HotPointManager : MonoBehaviour
{
	[SerializeField] private float hotPoint = 0f;

	[Header("-------- Setting ---------")]
	[SerializeField] private float decreaseRate = 0.1f;
	[SerializeField] private float decayDelay = 3f;
	[SerializeField] private float[] stageMaxHotPoint = { 2f, 4f, 6f, 8f, 10f };

	// 在最高熱度時，先維持的秒數（可調整）
	[SerializeField] private float maxHoldDuration = 10f;

	[SerializeField] private float currentMinHotPoint = 0f;
	[SerializeField] private float currentMaxHotPoint = 10f;

	[Header("-------- Reference ---------")]
	[SerializeField] private Sprite[] hotLevelSprites; // D~S 對應 0~4
	[SerializeField] private Image hotPointImage;
	[SerializeField] private Image hotPointFillBar;
	[SerializeField] private Slider hotPointSlider;

	private float lastHotIncreaseTime;
	private int currentLevelIndex;
	private float maxHotPointValue;

	// 最高熱度的保留到期時間（>Time.time 表示仍在保留期）
	private float maxHoldUntil = 0f;

	private void Start()
	{
		RoundManager.Instance.globalLightManager.SetLightGroupActive(0, false);

		currentLevelIndex = GetHotLevelIndex();
		currentMaxHotPoint = stageMaxHotPoint[currentLevelIndex];
		currentMinHotPoint = currentLevelIndex == 0 ? 0f : stageMaxHotPoint[currentLevelIndex - 1];

		maxHotPointValue = stageMaxHotPoint[stageMaxHotPoint.Length - 1];
		UpdateHotUI();
	}

	private void Update()
	{
		// 是否處在最高熱度
		bool atAbsoluteMax = hotPoint >= maxHotPointValue - 0.0001f;

		// 若剛好在最高熱度，且尚未設定保留期，設定保留到期時間（處理載入/初始化直接滿的情況）
		if (atAbsoluteMax && maxHoldUntil <= 0f)
			maxHoldUntil = Time.time + maxHoldDuration;

		// 是否可以開始衰減
		bool canDecay;
		if (atAbsoluteMax)
		{
			// 最高熱度：保留期過後才衰減
			canDecay = Time.time >= maxHoldUntil;
		}
		else
		{
			// 非最高熱度：沿用原本 decayDelay 規則
			canDecay = (Time.time - lastHotIncreaseTime > decayDelay);
			// 離開最高熱度就清空保留期
			maxHoldUntil = 0f;
		}

		// 衰減
		if (canDecay && hotPoint > 0f)
		{
			hotPoint -= decreaseRate * Time.deltaTime;
			hotPoint = Mathf.Clamp(hotPoint, 0f, maxHotPointValue);
		}

		// 等級切換檢查
		int newLevelIndex = GetHotLevelIndex();
		if (newLevelIndex != currentLevelIndex)
		{
			currentLevelIndex = newLevelIndex;
			currentMinHotPoint = currentLevelIndex == 0 ? 0f : stageMaxHotPoint[currentLevelIndex - 1];
			currentMaxHotPoint = stageMaxHotPoint[currentLevelIndex];

			if (currentLevelIndex == stageMaxHotPoint.Length - 2)
			{
				RoundManager.Instance.globalLightManager.SetLightCycleLoopEnabled(false);
				RoundManager.Instance.globalLightManager.SetLightGroupActive(0, true);
				RoundManager.Instance.globalLightManager.SetLightGroupActive(1, false);
			}
			else if (currentLevelIndex == stageMaxHotPoint.Length - 1)
			{
				RoundManager.Instance.globalLightManager.SetLightCycleLoopEnabled(true);
				RoundManager.Instance.globalLightManager.SetLightGroupActive(0, true);
				RoundManager.Instance.globalLightManager.SetLightGroupActive(1, true);
			}
			else
			{
				RoundManager.Instance.globalLightManager.SetLightCycleLoopEnabled(false);
				RoundManager.Instance.globalLightManager.SetLightGroupActive(0, false);
				RoundManager.Instance.globalLightManager.SetLightGroupActive(1, false);
			}
		}

		UpdateHotUI();
	}

	public void AddHotPoint(float value)
	{
		float before = hotPoint;

		hotPoint += value;
		hotPoint = Mathf.Clamp(hotPoint, 0f, stageMaxHotPoint[^1]);
		lastHotIncreaseTime = Time.time;

		// 若這次加成後達到最高熱度，刷新/設定保留期
		if (before < stageMaxHotPoint[^1] && hotPoint >= stageMaxHotPoint[^1] - 0.0001f)
		{
			maxHoldUntil = Time.time + maxHoldDuration;
		}
	}

	private void UpdateHotUI()
	{
		// 更新圖示
		if (hotLevelSprites != null && hotLevelSprites.Length > currentLevelIndex)
		{
			hotPointImage.sprite = hotLevelSprites[currentLevelIndex];
			hotPointFillBar.sprite = hotLevelSprites[currentLevelIndex];
		}

		// Slider 0~1：數值越高，滑條越接近 0（沿用你的設計）
		hotPointSlider.value = (maxHotPointValue - hotPoint) / maxHotPointValue;
	}

	public int GetMoneyMultiplier()
	{
		return currentLevelIndex + 1;
	}

	private int GetHotLevelIndex()
	{
		for (int i = stageMaxHotPoint.Length - 1; i >= 1; i--)
		{
			if (hotPoint >= stageMaxHotPoint[i - 1])
				return i;
		}
		return 0;
	}
}