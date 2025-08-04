using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ReigonLightControll : MonoBehaviour
{
	public enum LightMode
	{
		None,
		Constant,
		Breathing,
		Flicker,
		ColorCycle,
		Wave
	}

	[Header("Target Settings")]
	[SerializeField] private LightMode mode = LightMode.None;
	[SerializeField] private float baseIntensity = 1f;

	[Header("Rotation Settings")]
	[SerializeField] private bool isRotationEnabled = true;
	[SerializeField] private bool isClockwise = true;
	[SerializeField] private float rotationSpeed = 30f;

	[Header("Breathing Settings")]
	[SerializeField] private float breathSpeed = 2f;
	[SerializeField] private float breathRange = 0.5f;

	[Header("Flicker Settings")]
	[SerializeField] private float flickerMin = 0.5f;
	[SerializeField] private float flickerMax = 1.5f;
	[SerializeField] private float flickerSpeed = 0.05f;

	[Header("Color Cycle Settings")]
	[SerializeField] private List<Color> colorList = new List<Color> { Color.red, Color.green, Color.blue };
	[SerializeField] private float colorCycleSpeed = 1f;

	[Header("Wave Settings")]
	[SerializeField] private float waveSpeed = 2f;
	[SerializeField] private float waveAmplitude = 0.3f;

	private List<Light2D> childLights = new List<Light2D>();
	private List<Color> originalLightColors = new List<Color>();
	private float timer;
	private int currentColorIndex = 0;

	void Start()
	{
		// 找出此物件底下所有 Light2D
		childLights.Clear();
		childLights.AddRange(GetComponentsInChildren<Light2D>());

		originalLightColors.Clear();
		foreach (var light in childLights)
			originalLightColors.Add(light.color);
	}

	void Update()
	{
		// 旋轉
		if (isRotationEnabled)
		{
			float angle = rotationSpeed * Time.deltaTime;
			angle *= isClockwise ? -1f : 1f;
			transform.Rotate(Vector3.forward, angle);
		}

		// 如果沒有 Light2D 就不執行效果
		if (childLights.Count == 0) return;

		switch (mode)
		{
			case LightMode.Constant:
				ApplyIntensity(baseIntensity);
				break;

			case LightMode.Breathing:
				ApplyIntensity(baseIntensity + Mathf.Sin(Time.time * breathSpeed) * breathRange);
				break;

			case LightMode.Flicker:
				timer += Time.deltaTime;
				if (timer >= flickerSpeed)
				{
					ApplyIntensity(Random.Range(flickerMin, flickerMax));
					timer = 0f;
				}
				break;

			case LightMode.ColorCycle:
				ApplyColorCycle();
				break;

			case LightMode.Wave:
				float wave = Mathf.Sin(Time.time * waveSpeed) * waveAmplitude;
				ApplyIntensity(baseIntensity + wave);
				break;

			case LightMode.None:
			default:
				break;
		}
	}

	// 套用亮度到所有子 Light2D
	private void ApplyIntensity(float intensity)
	{
		foreach (var light in childLights)
			light.intensity = Mathf.Max(0f, intensity);
	}

	// 色彩循環模式（基於原本顏色變化）
	private void ApplyColorCycle()
	{
		if (colorList.Count == 0) return;

		// 計算漸變進度
		float t = (Mathf.Sin(Time.time * colorCycleSpeed) + 1f) / 2f;

		// 當前顏色與下一個顏色索引
		Color fromColor = colorList[currentColorIndex];
		Color toColor = colorList[(currentColorIndex + 1) % colorList.Count];

		// 平滑漸變
		Color cycleColor = Color.Lerp(fromColor, toColor, t);

		// 套用到每個 Light2D（基於原本的色系做乘算）
		for (int i = 0; i < childLights.Count; i++)
		{
			Light2D light = childLights[i];
			Color original = originalLightColors[i];
			light.color = new Color(
				original.r * cycleColor.r,
				original.g * cycleColor.g,
				original.b * cycleColor.b,
				original.a // 不改變透明度
			);
		}

		// 切換顏色段
		if (Time.time - timer > (1f / colorCycleSpeed))
		{
			currentColorIndex = (currentColorIndex + 1) % colorList.Count;
			timer = Time.time;
		}
	}


	// 外部設定模式
	public void SetMode(LightMode newMode) => mode = newMode;

	// 外部控制旋轉啟用
	public void SetRotationEnabled(bool enabled) => isRotationEnabled = enabled;

	// 外部控制旋轉速度
	public void SetRotationSpeed(float speed) => rotationSpeed = speed;

	// 外部控制旋轉方向
	public void SetRotationDirection(bool clockwise) => isClockwise = clockwise;
}
