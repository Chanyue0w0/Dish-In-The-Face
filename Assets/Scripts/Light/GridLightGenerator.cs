using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 在編輯模式下也會執行（可立即在編輯器中生成）
[ExecuteAlways]
public class GridLightGenerator : MonoBehaviour
{
	// ------ 設定區塊 ------

	[Header("Target Sprite")]
	[SerializeField] private SpriteRenderer targetSprite; // 目標 Sprite（用於取得範圍）

	[Header("Grid Setting")]
	[SerializeField] private int columns = 5;     // 橫向幾列光源
	[SerializeField] private int rows = 5;        // 垂直幾列光源

	[Header("Light Scale")]
	[SerializeField] private Vector2 manualScale = new Vector2(1f, 1f); // 手動設定大小

	[Header("Light Color (Alternating)")]
	[SerializeField] private bool useCustomColors = true; // 是否使用自訂顏色
	[SerializeField] private List<Color> lightColors = new List<Color> { Color.red, Color.green, Color.blue }; // 可自訂顏色列表

	[Header("Light Prefab (Optional)")]
	[SerializeField] private GameObject lightPrefab; // 可選用的 Light2D 預製物件

	[Header("Default Light Settings (used only if no prefab)")]
	[SerializeField] private float intensity = 1f;
	[SerializeField] private float falloffIntensity = 0f;  // 越低越柔
	[SerializeField] private float alpha = 1f;

	private List<Light2D> generatedLights = new List<Light2D>(); // 儲存已生成的光源

	// ------ 生成光源主功能 ------
	[ContextMenu("Generate Grid Lights")]
	public void GenerateLights()
	{
		if (targetSprite == null)
		{
			Debug.LogError("請指定 SpriteRenderer");
			return;
		}

		ClearChildren();
		generatedLights.Clear();


		// 取得 Sorting Layer 與 Order（來源是 targetSprite）
		int sortingLayerID = targetSprite.sortingLayerID;
		int sortingOrder = targetSprite.sortingOrder;

		// 取得 Sprite 邊界與尺寸
		Bounds bounds = targetSprite.bounds;
		Vector3 bottomLeft = bounds.min;
		Vector3 size = bounds.size;

		float cellWidth = size.x / columns;
		float cellHeight = size.y / rows;

		for (int y = 0; y < rows; y++)
		{
			for (int x = 0; x < columns; x++)
			{
				Vector3 pos = bottomLeft + new Vector3((x + 0.5f) * cellWidth, (y + 0.5f) * cellHeight, 0f);

				GameObject lightGO;
				Light2D light2D;

				// 有指定 prefab 時
				if (lightPrefab != null)
				{
					lightGO = Instantiate(lightPrefab, pos, Quaternion.identity, transform);
					lightGO.name = $"Light_{x}_{y}";
					light2D = lightGO.GetComponent<Light2D>() ?? lightGO.AddComponent<Light2D>();

					var source = lightPrefab.GetComponent<Light2D>();
					if (source != null)
						CopyLight2DSettings(source, light2D);
				}
				else // 沒指定 prefab 時使用預設光源
				{
					lightGO = new GameObject($"Light_{x}_{y}", typeof(Light2D));
					lightGO.transform.SetParent(transform);
					light2D = lightGO.GetComponent<Light2D>();
					light2D.lightType = Light2D.LightType.Point;
					light2D.intensity = intensity;
					light2D.falloffIntensity = falloffIntensity;
				}

				lightGO.transform.position = pos;

				lightGO.transform.localScale = new Vector3(manualScale.x, manualScale.y, 1f);

				// 顏色設定
				if (useCustomColors && lightColors.Count > 0)
				{
					int colorIndex = (x + y) % lightColors.Count;
					Color baseColor = lightColors[colorIndex];
					baseColor.a = Mathf.Clamp01(alpha);
					light2D.color = baseColor;
				}


				// 套用 sorting layer 與 order
				light2D.lightOrder = sortingOrder;

				generatedLights.Add(light2D);
			}
		}
	}

	// ------ 清除子物件（重新生成前使用）------
	private void ClearChildren()
	{
		List<Transform> toDestroy = new List<Transform>();
		foreach (Transform child in transform)
			toDestroy.Add(child);

		foreach (Transform t in toDestroy)
		{
#if UNITY_EDITOR
			if (Application.isEditor)
				DestroyImmediate(t.gameObject);
			else
				Destroy(t.gameObject);
#else
            Destroy(t.gameObject);
#endif
		}
	}

	// ------ 複製 Light2D 設定 ------ 
	private void CopyLight2DSettings(Light2D source, Light2D target)
	{
		if (source == null || target == null) return;

		target.lightType = source.lightType;
		target.intensity = intensity;
		target.falloffIntensity = falloffIntensity;
		target.pointLightInnerRadius = source.pointLightInnerRadius;
		target.pointLightOuterRadius = source.pointLightOuterRadius;
		target.shapeLightFalloffSize = source.shapeLightFalloffSize;

		if (source.lightType == Light2D.LightType.Freeform)
			target.SetShapePath(source.shapePath);

		target.lightOrder = source.lightOrder;
		target.volumeIntensity = source.volumeIntensity;
		target.lightCookieSprite = source.lightCookieSprite;
	}
}
