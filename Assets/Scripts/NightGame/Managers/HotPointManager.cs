using UnityEngine;
using UnityEngine.UI;

public class HotPointManager : MonoBehaviour
{
	[SerializeField] private float hotPoint = 0f;

	[Header("-------- Setting ---------")]
	[SerializeField] private float decreaseRate = 0.1f;
	[SerializeField] private float decayDelay = 3f;
	[SerializeField] private float[] stageMaxHotPoint = { 2f, 4f, 6f, 8f, 10f };

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
		// 太久沒觸發熱度，自動下降
		if (Time.time - lastHotIncreaseTime > decayDelay && hotPoint > 0f)
		{
			hotPoint -= decreaseRate * Time.deltaTime;
			hotPoint = Mathf.Clamp(hotPoint, 0f, stageMaxHotPoint[^1]); // Clamp to max stage value
		}

		int newLevelIndex = GetHotLevelIndex();
		if (newLevelIndex != currentLevelIndex) // 進階、更新區間
		{
			currentLevelIndex = newLevelIndex;
			currentMinHotPoint = currentLevelIndex == 0 ? 0f : stageMaxHotPoint[currentLevelIndex - 1];
			currentMaxHotPoint = stageMaxHotPoint[currentLevelIndex];

			if (currentLevelIndex == stageMaxHotPoint.Length - 2) RoundManager.Instance.globalLightManager.SetLightGroupActive(0, true);
			else if (currentLevelIndex == stageMaxHotPoint.Length - 1) RoundManager.Instance.globalLightManager.SetLightCycleLoopEnabled(true);
			else
			{
				RoundManager.Instance.globalLightManager.SetLightCycleLoopEnabled(false);
				RoundManager.Instance.globalLightManager.SetLightGroupActive(0, false);
			}
		}

		UpdateHotUI();
	}

	public void AddHotPoint(float value)
	{
		hotPoint += value;
		hotPoint = Mathf.Clamp(hotPoint, 0f, stageMaxHotPoint[^1]);
		lastHotIncreaseTime = Time.time;
	}

	private void UpdateHotUI()
	{
		// 更新圖示
		if (hotLevelSprites != null && hotLevelSprites.Length > currentLevelIndex)
		{
			hotPointImage.sprite = hotLevelSprites[currentLevelIndex];
			hotPointFillBar.sprite = hotLevelSprites[currentLevelIndex];
		}

		//// 更新 fillAmount 基於該等級區間
		//float normalized = (hotPoint - currentMinHotPoint) / (currentMaxHotPoint - currentMinHotPoint);
		//hotPointFillBar.fillAmount = normalized;

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
			if (hotPoint >= stageMaxHotPoint[i-1])
				return i;
		}
		return 0;
	}
}
