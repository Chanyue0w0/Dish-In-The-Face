using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;

public class ObjectInspectorTool : EditorWindow
{
	private Vector2 ScrollPosition;

	private List<GameObject> TrackedObjects = new();
	private Dictionary<GameObject, bool> ObjectFoldouts = new();
	private Dictionary<Component, bool> ComponentFoldouts = new();
	private Dictionary<GameObject, List<Component>> ObjectTrackedComponents = new();

	private bool ShowUnsupportedFieldTypes = false;
	private bool ShowPrivateFieldsWithoutSerializeField = false;

	[MenuItem("Tools/Advanced Object Inspector")]
	public static void ShowWindow()
	{
		GetWindow<ObjectInspectorTool>("Object Inspector");
	}

	void OnGUI()
	{
		EditorGUILayout.LabelField("場景物件欄位總覽工具", EditorStyles.boldLabel);
		EditorGUILayout.Space();

		// 設定選項按鈕
		ShowUnsupportedFieldTypes = GUILayout.Toggle(ShowUnsupportedFieldTypes, "顯示不支援類型欄位");
		ShowPrivateFieldsWithoutSerializeField = GUILayout.Toggle(ShowPrivateFieldsWithoutSerializeField, "顯示沒有 [SerializeField] 的 private 欄位");

		EditorGUILayout.Space();

		// 按鈕：加入目前選取物件的所有元件
		if (GUILayout.Button("加入選取物件的所有元件"))
		{
			GameObject selected = Selection.activeGameObject;
			if (selected != null)
			{
				if (!TrackedObjects.Contains(selected))
				{
					TrackedObjects.Add(selected);
					ObjectFoldouts[selected] = true;
					ObjectTrackedComponents[selected] = new List<Component>();
				}

				Component[] components = selected.GetComponents<Component>();
				foreach (var comp in components)
				{
					if (!ObjectTrackedComponents[selected].Contains(comp))
						ObjectTrackedComponents[selected].Add(comp);
				}
			}
		}

		// 按鈕：手動挑選元件
		if (GUILayout.Button("選擇要加入的元件"))
		{
			GameObject selected = Selection.activeGameObject;
			if (selected != null)
			{
				ShowComponentPicker(selected);
			}
		}

		EditorGUILayout.Space();
		ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);

		// 顯示每個追蹤的 GameObject 與元件欄位
		for (int i = 0; i < TrackedObjects.Count; i++)
		{
			GameObject go = TrackedObjects[i];
			if (go == null) continue;

			EditorGUILayout.BeginHorizontal();
			bool shouldSkip = false;

			ObjectFoldouts.TryGetValue(go, out bool goOpen);
			goOpen = EditorGUILayout.Foldout(goOpen, $"物件：{go.name}", true);
			ObjectFoldouts[go] = goOpen;

			// 移除該 GameObject
			if (GUILayout.Button("X", GUILayout.Width(20)))
			{
				TrackedObjects.RemoveAt(i);
				ObjectFoldouts.Remove(go);
				ObjectTrackedComponents.Remove(go);
				i--;
				shouldSkip = true;
			}
			EditorGUILayout.EndHorizontal();
			if (shouldSkip) continue;

			if (goOpen)
			{
				EditorGUI.indentLevel++;
				if (!ObjectTrackedComponents.ContainsKey(go)) continue;

				var compList = ObjectTrackedComponents[go];
				for (int j = 0; j < compList.Count; j++)
				{
					Component comp = compList[j];
					if (comp == null) continue;

					ComponentFoldouts.TryGetValue(comp, out bool compOpen);

					EditorGUILayout.BeginHorizontal();
					bool skipComponent = false;

					string label = GetHeaderOrTypeName(comp);
					compOpen = EditorGUILayout.Foldout(compOpen, $"Component：{label}", true);
					ComponentFoldouts[comp] = compOpen;

					if (GUILayout.Button("X", GUILayout.Width(20)))
					{
						compList.RemoveAt(j);
						ComponentFoldouts.Remove(comp);
						j--;
						skipComponent = true;
					}
					EditorGUILayout.EndHorizontal();
					if (skipComponent) continue;

					if (compOpen)
					{
						EditorGUI.indentLevel++;
						DrawAllFields(comp);
						EditorGUI.indentLevel--;
					}
				}
				EditorGUI.indentLevel--;
			}
		}

		EditorGUILayout.EndScrollView();
		EditorGUILayout.Space();

		if (GUILayout.Button("清空追蹤清單"))
		{
			TrackedObjects.Clear();
			ObjectFoldouts.Clear();
			ComponentFoldouts.Clear();
			ObjectTrackedComponents.Clear();
		}
	}

	// 顯示元件選擇選單（已修正 Lambda Closure 問題）
	private void ShowComponentPicker(GameObject go)
	{
		GenericMenu menu = new GenericMenu();
		Component[] comps = go.GetComponents<Component>();

		foreach (var comp in comps)
		{
			if (comp == null) continue;

			// 捕捉區域變數，避免 Lambda 閉包錯誤
			GameObject targetGO = go;
			Component targetComp = comp;

			menu.AddItem(new GUIContent(comp.GetType().Name), false, () =>
			{
				// 加入 GameObject（如果尚未存在）
				if (!TrackedObjects.Contains(targetGO))
				{
					TrackedObjects.Add(targetGO);
					ObjectFoldouts[targetGO] = true;
				}

				// 確保 Dictionary 已初始化
				if (!ObjectTrackedComponents.ContainsKey(targetGO))
				{
					ObjectTrackedComponents[targetGO] = new List<Component>();
				}

				// 加入元件
				if (!ObjectTrackedComponents[targetGO].Contains(targetComp))
				{
					ObjectTrackedComponents[targetGO].Add(targetComp);
				}
			});
		}

		menu.ShowAsContext();
	}

	// 顯示元件的所有欄位
	void DrawAllFields(Component component)
	{
		BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		var fields = component.GetType().GetFields(flags);

		string currentHeader = null;

		foreach (var field in fields)
		{
			if (field.IsDefined(typeof(HideInInspector), true)) continue;

			bool isPublic = field.IsPublic;
			bool isPrivate = !isPublic;
			bool isSerialized = isPublic || field.IsDefined(typeof(SerializeField), false);

			if (!isSerialized && !ShowPrivateFieldsWithoutSerializeField)
				continue;

			// 顯示 Header（支援多層 attribute）
			var headerAttrs = field.GetCustomAttributes(typeof(HeaderAttribute), false);
			if (headerAttrs.Length > 0)
			{
				string newHeader = ((HeaderAttribute)headerAttrs[0]).header;
				if (newHeader != currentHeader)
				{
					currentHeader = newHeader;
					EditorGUILayout.Space(4);
					EditorGUILayout.LabelField(currentHeader, EditorStyles.boldLabel);
				}
			}

			string displayName = FormatFieldName(field.Name);
			object fieldValue = field.GetValue(component);
			object newValue = DrawField(displayName, fieldValue, field.FieldType);

			if (newValue != null && !Equals(fieldValue, newValue))
			{
				Undo.RecordObject(component, "Change Field Value");
				field.SetValue(component, newValue);
				EditorUtility.SetDirty(component);
			}
		}
	}

	// 自訂欄位顯示排版（加大 label 與 value 的間距）
	object DrawField(string label, object value, Type type)
	{
		try
		{
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField(label, GUILayout.Width(160));
			object result = null;

			if (type == typeof(int))
				result = EditorGUILayout.IntField((int)value);
			else if (type == typeof(float))
				result = EditorGUILayout.FloatField((float)value);
			else if (type == typeof(bool))
				result = EditorGUILayout.Toggle((bool)value);
			else if (type == typeof(string))
				result = EditorGUILayout.TextField((string)value);
			else if (type == typeof(Vector3))
				result = EditorGUILayout.Vector3Field("", (Vector3)value);
			else if (type == typeof(Vector2))
				result = EditorGUILayout.Vector2Field("", (Vector2)value);
			else if (type == typeof(Color))
				result = EditorGUILayout.ColorField((Color)value);
			else
			{
				if (ShowUnsupportedFieldTypes)
					EditorGUILayout.LabelField($"不支援的類型：{type.Name}");
			}

			EditorGUILayout.EndHorizontal();
			return result;
		}
		catch
		{
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField(label, "錯誤：無法顯示");
			return null;
		}
	}

	// 取得元件類型或 Header 名稱
	private string GetHeaderOrTypeName(Component component)
	{
		var headerAttr = component.GetType().GetCustomAttribute<HeaderAttribute>();
		if (headerAttr != null && !string.IsNullOrWhiteSpace(headerAttr.header))
			return headerAttr.header;
		else
			return component.GetType().Name;
	}

	// 將變數名稱轉為「每個單字大寫＋空格」
	private string FormatFieldName(string rawName)
	{
		string spaced = Regex.Replace(rawName, "(\\B[A-Z])", " $1").Replace("_", " ").Trim();
		return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(spaced.ToLower());
	}
}
