using UnityEngine;
using UnityEditor;

public class ReplaceSpriteMaterial : EditorWindow
{
	private Material selectedMaterial;

	[MenuItem("Tools/Replace All SpriteRenderer Materials")]
	public static void ShowWindow()
	{
		GetWindow<ReplaceSpriteMaterial>("Replace SpriteRenderer Materials");
	}

	private void OnGUI()
	{
		GUILayout.Label("批次替換 SpriteRenderer 的 Material", EditorStyles.boldLabel);

		EditorGUILayout.Space(10);

		selectedMaterial = (Material)EditorGUILayout.ObjectField("選擇新的 Material", selectedMaterial, typeof(Material), false);

		EditorGUILayout.HelpBox(
			"此工具會將當前場景中所有 SpriteRenderer 的材質替換為選中的 Material。\n" +
			"注意：這只影響場景內的物件，不會改到 prefab 資源。",
			MessageType.Info);

		EditorGUILayout.Space(10);

		if (selectedMaterial == null)
		{
			EditorGUILayout.HelpBox("請選擇一個有效的 Material", MessageType.Warning);
			GUI.enabled = false;
		}

		if (GUILayout.Button("執行替換"))
		{
			ReplaceAllSpriteRendererMaterials();
		}

		GUI.enabled = true;
	}

	private void ReplaceAllSpriteRendererMaterials()
	{
		int count = 0;

		SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>();
		foreach (SpriteRenderer sr in renderers)
		{
			Undo.RecordObject(sr, "Replace SpriteRenderer Material");
			sr.sharedMaterial = selectedMaterial;
			count++;
		}

		Debug.Log($" 替換完成：共 {count} 個 SpriteRenderer 的材質已更新為 {selectedMaterial.name}");
		EditorUtility.DisplayDialog("替換完成", $"共 {count} 個 SpriteRenderer 的材質已更新為 {selectedMaterial.name}", "OK");
	}
}