using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class GlobalLightManager : MonoBehaviour
{
	[System.Serializable]
	public class LightGroupEntry
	{
		public GameObject groupObject;
		public bool isActive = true;
	}

	[Header("Loop Setting")]
	[SerializeField] private float cycleDuration = 6f; // ����`�����
	[SerializeField] private bool enableRGBLoop = true;
	[SerializeField] private Color[] colorCycleList = new Color[] { Color.red, Color.green, Color.blue };

	[Header("Other Light group Object")]
	[SerializeField] private List<LightGroupEntry> lightGroups = new List<LightGroupEntry>();

	private Light2D globalLight;
	private float timer = 0f;
	private Color originalColor;
	private bool hasStoredOriginal = false;

	private void Awake()
	{
		globalLight = GetComponent<Light2D>();

		if (!hasStoredOriginal && globalLight != null)
		{
			originalColor = globalLight.color;
			hasStoredOriginal = true;
		}

		// �ھ� Inspector �]�w�}�� Light Group
		foreach (var entry in lightGroups)
		{
			if (entry.groupObject != null)
				entry.groupObject.SetActive(entry.isActive);
		}
	}

	private void Update()
	{
		if (enableRGBLoop && colorCycleList.Length >= 2)
			UpdateLightCycleLoop();
	}

	private void UpdateLightCycleLoop()
	{
		timer += Time.deltaTime;

		float segmentDuration = cycleDuration / colorCycleList.Length;
		float totalTime = timer % cycleDuration;

		int currentIndex = Mathf.FloorToInt(totalTime / segmentDuration);
		int nextIndex = (currentIndex + 1) % colorCycleList.Length;

		float t = (totalTime % segmentDuration) / segmentDuration;

		Color color = Color.Lerp(colorCycleList[currentIndex], colorCycleList[nextIndex], t);
		globalLight.color = color;
	}

	/// �~������O�_�ҥ��C��`��
	public void SetLightCycleLoopEnabled(bool enabled)
	{
		if (!hasStoredOriginal && globalLight != null)
		{
			originalColor = globalLight.color;
			hasStoredOriginal = true;
		}

		enableRGBLoop = enabled;

		if (!enabled && globalLight != null)
		{
			globalLight.color = originalColor;
		}
	}

	/// �~��������w LightGroup �}���]�̷� index�^
	public void SetLightGroupActive(int index, bool isActive)
	{
		if (index < 0 || index >= lightGroups.Count)
		{
			Debug.LogWarning($"SetLightGroupActive�G���� {index} �W�X�d��");
			return;
		}

		if (lightGroups[index].groupObject != null)
		{
			lightGroups[index].groupObject.SetActive(isActive);
			lightGroups[index].isActive = isActive; // �P�B���
		}
	}
}
