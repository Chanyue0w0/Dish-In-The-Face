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
		GUILayout.Label("�妸���� SpriteRenderer �� Material", EditorStyles.boldLabel);

		EditorGUILayout.Space(10);

		selectedMaterial = (Material)EditorGUILayout.ObjectField("��ܷs�� Material", selectedMaterial, typeof(Material), false);

		EditorGUILayout.HelpBox(
			"���u��|�N��e�������Ҧ� SpriteRenderer ������������襤�� Material�C\n" +
			"�`�N�G�o�u�v�T������������A���|��� prefab �귽�C",
			MessageType.Info);

		EditorGUILayout.Space(10);

		if (selectedMaterial == null)
		{
			EditorGUILayout.HelpBox("�п�ܤ@�Ӧ��Ī� Material", MessageType.Warning);
			GUI.enabled = false;
		}

		if (GUILayout.Button("�������"))
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

		Debug.Log($" ���������G�@ {count} �� SpriteRenderer ������w��s�� {selectedMaterial.name}");
		EditorUtility.DisplayDialog("��������", $"�@ {count} �� SpriteRenderer ������w��s�� {selectedMaterial.name}", "OK");
	}
}