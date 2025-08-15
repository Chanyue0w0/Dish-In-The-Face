using UnityEngine;
using UnityEngine.UI;

public class HotPointManager : MonoBehaviour
{
	[SerializeField] private float hotPoint = 0f;

	[Header("-------- Setting ---------")]
	[SerializeField] private float decreaseRate = 0.1f;
	[SerializeField] private float decayDelay = 3f;
	[SerializeField] private float[] stageMaxHotPoint = { 2f, 4f, 6f, 8f, 10f };

	// �b�̰����׮ɡA����������ơ]�i�վ�^
	[SerializeField] private float maxHoldDuration = 10f;

	[SerializeField] private float currentMinHotPoint = 0f;
	[SerializeField] private float currentMaxHotPoint = 10f;

	[Header("-------- Reference ---------")]
	[SerializeField] private Sprite[] hotLevelSprites; // D~S ���� 0~4
	[SerializeField] private Image hotPointImage;
	[SerializeField] private Image hotPointFillBar;
	[SerializeField] private Slider hotPointSlider;

	private float lastHotIncreaseTime;
	private int currentLevelIndex;
	private float maxHotPointValue;

	// �̰����ת��O�d����ɶ��]>Time.time ��ܤ��b�O�d���^
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
		// �O�_�B�b�̰�����
		bool atAbsoluteMax = hotPoint >= maxHotPointValue - 0.0001f;

		// �Y��n�b�̰����סA�B�|���]�w�O�d���A�]�w�O�d����ɶ��]�B�z���J/��l�ƪ����������p�^
		if (atAbsoluteMax && maxHoldUntil <= 0f)
			maxHoldUntil = Time.time + maxHoldDuration;

		// �O�_�i�H�}�l�I��
		bool canDecay;
		if (atAbsoluteMax)
		{
			// �̰����סG�O�d���L��~�I��
			canDecay = Time.time >= maxHoldUntil;
		}
		else
		{
			// �D�̰����סG�u�έ쥻 decayDelay �W�h
			canDecay = (Time.time - lastHotIncreaseTime > decayDelay);
			// ���}�̰����״N�M�ūO�d��
			maxHoldUntil = 0f;
		}

		// �I��
		if (canDecay && hotPoint > 0f)
		{
			hotPoint -= decreaseRate * Time.deltaTime;
			hotPoint = Mathf.Clamp(hotPoint, 0f, maxHotPointValue);
		}

		// ���Ť����ˬd
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

		// �Y�o���[����F��̰����סA��s/�]�w�O�d��
		if (before < stageMaxHotPoint[^1] && hotPoint >= stageMaxHotPoint[^1] - 0.0001f)
		{
			maxHoldUntil = Time.time + maxHoldDuration;
		}
	}

	private void UpdateHotUI()
	{
		// ��s�ϥ�
		if (hotLevelSprites != null && hotLevelSprites.Length > currentLevelIndex)
		{
			hotPointImage.sprite = hotLevelSprites[currentLevelIndex];
			hotPointFillBar.sprite = hotLevelSprites[currentLevelIndex];
		}

		// Slider 0~1�G�ƭȶV���A�Ʊ��V���� 0�]�u�ΧA���]�p�^
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
