using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// �b�s��Ҧ��U�]�|����]�i�ߧY�b�s�边���ͦ��^
[ExecuteAlways]
public class GridLightGenerator : MonoBehaviour
{
	// ------ �]�w�϶� ------

	[Header("Target Sprite")]
	[SerializeField] private SpriteRenderer targetSprite; // �ؼ� Sprite�]�Ω���o�d��^

	[Header("Grid Setting")]
	[SerializeField] private int columns = 5;     // ��V�X�C����
	[SerializeField] private int rows = 5;        // �����X�C����
	[SerializeField] private float paddingX = 0.1f; // X�b���Z�Y��
	[SerializeField] private float paddingY = 0.1f; // Y�b���Z�Y��

	[Header("Light Scale")]
	[SerializeField] private bool useAutoScale = true; // �O�_�۰ʨ̮�l�վ�j�p
	[SerializeField] private Vector2 manualScale = new Vector2(1f, 1f); // ��ʳ]�w�j�p

	[Header("Light Color (Alternating)")]
	[SerializeField] private List<Color> lightColors = new List<Color> { Color.red, Color.green, Color.blue }; // �i�ۭq�C��C��
	[SerializeField] private bool useCustomColors = true; // �O�_�ϥΦۭq�C��

	[Header("Light Prefab (Optional)")]
	[SerializeField] private GameObject lightPrefab; // �i��Ϊ� Light2D �w�s����

	[Header("Default Light Settings (used only if no prefab)")]
	[SerializeField] private float intensity = 1f;
	[SerializeField] private float falloffIntensity = 0f;  // �V�C�V�X
	[SerializeField] private float alpha = 1f;

	private List<Light2D> generatedLights = new List<Light2D>(); // �x�s�w�ͦ�������

	// ------ �ͦ������D�\�� ------
	[ContextMenu("Generate Grid Lights")]
	public void GenerateLights()
	{
		if (targetSprite == null)
		{
			Debug.LogError("�Ы��w SpriteRenderer");
			return;
		}

		ClearChildren();
		generatedLights.Clear();

		// ���o Sprite ��ɻP�ؤo
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

				// �����w prefab ��
				if (lightPrefab != null)
				{
					lightGO = Instantiate(lightPrefab, pos, Quaternion.identity, transform);
					light2D = lightGO.GetComponent<Light2D>() ?? lightGO.AddComponent<Light2D>();

					var source = lightPrefab.GetComponent<Light2D>();
					if (source != null)
						CopyLight2DSettings(source, light2D);
				}
				else // �S���w prefab �ɨϥιw�]����
				{
					lightGO = new GameObject($"Light_{x}_{y}", typeof(Light2D));
					lightGO.transform.SetParent(transform);
					light2D = lightGO.GetComponent<Light2D>();
					light2D.lightType = Light2D.LightType.Point;
					light2D.intensity = intensity;
					light2D.falloffIntensity = falloffIntensity;
				}

				lightGO.transform.position = pos;

				// �Y�񱱨�
				if (useAutoScale)
				{
					float scaleX = cellWidth - paddingX;
					float scaleY = cellHeight - paddingY;
					lightGO.transform.localScale = new Vector3(scaleX, scaleY, 1f);
				}
				else
				{
					lightGO.transform.localScale = new Vector3(manualScale.x, manualScale.y, 1f);
				}

				// �C��]�w
				if (useCustomColors && lightColors.Count > 0)
				{
					int colorIndex = (x + y) % lightColors.Count;
					Color baseColor = lightColors[colorIndex];
					baseColor.a = Mathf.Clamp01(alpha);
					light2D.color = baseColor;
				}

				generatedLights.Add(light2D);
			}
		}
	}

	// ------ �M���l����]���s�ͦ��e�ϥΡ^------
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

	// ------ �ƻs Light2D �]�w ------ 
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
