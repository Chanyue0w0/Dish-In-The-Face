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
	[SerializeField] private Sprite[] hotLevelSprites; // D~S ���� 0~4
	[SerializeField] private Image hotPointImage;
	[SerializeField] private Image hotPointFillBar;
	[SerializeField] private RoundManager roundManager;
	private float lastHotIncreaseTime;
	private int currentLevelIndex;
	

	private void Start()
	{
		currentLevelIndex = GetHotLevelIndex();
		currentMaxHotPoint = stageMaxHotPoint[currentLevelIndex];
		currentMinHotPoint = currentLevelIndex == 0 ? 0f : stageMaxHotPoint[currentLevelIndex - 1];
		UpdateHotUI();
	}

	private void Update()
	{
		// �Ӥ[�SĲ�o���סA�۰ʤU��
		if (Time.time - lastHotIncreaseTime > decayDelay && hotPoint > 0f)
		{
			hotPoint -= decreaseRate * Time.deltaTime;
			hotPoint = Mathf.Clamp(hotPoint, 0f, stageMaxHotPoint[^1]); // Clamp to max stage value
		}

		int newLevelIndex = GetHotLevelIndex();
		if (newLevelIndex != currentLevelIndex) // �i���B��s�϶�
		{
			currentLevelIndex = newLevelIndex;
			currentMinHotPoint = currentLevelIndex == 0 ? 0f : stageMaxHotPoint[currentLevelIndex - 1];
			currentMaxHotPoint = stageMaxHotPoint[currentLevelIndex];

			if (currentLevelIndex == stageMaxHotPoint.Length - 1) roundManager.globalLightManager.SetLightCycleLoopEnabled(true);
			else roundManager.globalLightManager.SetLightCycleLoopEnabled(false);
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
		// ��s�ϥ�
		if (hotLevelSprites != null && hotLevelSprites.Length > currentLevelIndex)
		{
			hotPointImage.sprite = hotLevelSprites[currentLevelIndex];
			hotPointFillBar.sprite = hotLevelSprites[currentLevelIndex];
		}
		// ��s fillAmount ���ӵ��Ű϶�
		float normalized = (hotPoint - currentMinHotPoint) / (currentMaxHotPoint - currentMinHotPoint);
		Debug.Log(normalized);
		hotPointFillBar.fillAmount = normalized;
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
