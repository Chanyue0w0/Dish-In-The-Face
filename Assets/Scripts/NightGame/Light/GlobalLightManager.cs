using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class GlobalLightManager : MonoBehaviour
{
	[Header("Loop Setting")]
	[SerializeField] private float cycleDuration = 6f; // 整體循環秒數
	[SerializeField] private bool enableRGBLoop = true;
	[SerializeField] private Color[] colorCycleList = new Color[] { Color.red, Color.green, Color.blue };

	private Light2D globalLight;
	private float timer = 0f;
	private Color originalColor;
	private bool hasStoredOriginal = false;

	private void Awake()
	{
		globalLight = GetComponent<Light2D>();
		originalColor = globalLight.color;
		hasStoredOriginal = true;
	}

	private void Update()
	{
		if (enableRGBLoop && colorCycleList.Length >= 2)
			UpdateLightCycleLoop();
	}

	/// 顏色漸變循環
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

	/// 開關顏色循環，關閉時回到初始顏色
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
}
