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
		EditorGUILayout.LabelField("������������`���u��", EditorStyles.boldLabel);
		EditorGUILayout.Space();

		// �]�w�ﶵ���s
		ShowUnsupportedFieldTypes = GUILayout.Toggle(ShowUnsupportedFieldTypes, "��ܤ��䴩�������");
		ShowPrivateFieldsWithoutSerializeField = GUILayout.Toggle(ShowPrivateFieldsWithoutSerializeField, "��ܨS�� [SerializeField] �� private ���");

		EditorGUILayout.Space();

		// ���s�G�[�J�ثe������󪺩Ҧ�����
		if (GUILayout.Button("�[�J������󪺩Ҧ�����"))
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

		// ���s�G��ʬD�露��
		if (GUILayout.Button("��ܭn�[�J������"))
		{
			GameObject selected = Selection.activeGameObject;
			if (selected != null)
			{
				ShowComponentPicker(selected);
			}
		}

		EditorGUILayout.Space();
		ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);

		// ��ܨC�Ӱl�ܪ� GameObject �P�������
		for (int i = 0; i < TrackedObjects.Count; i++)
		{
			GameObject go = TrackedObjects[i];
			if (go == null) continue;

			EditorGUILayout.BeginHorizontal();
			bool shouldSkip = false;

			ObjectFoldouts.TryGetValue(go, out bool goOpen);
			goOpen = EditorGUILayout.Foldout(goOpen, $"����G{go.name}", true);
			ObjectFoldouts[go] = goOpen;

			// ������ GameObject
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
					compOpen = EditorGUILayout.Foldout(compOpen, $"Component�G{label}", true);
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

		if (GUILayout.Button("�M�Űl�ܲM��"))
		{
			TrackedObjects.Clear();
			ObjectFoldouts.Clear();
			ComponentFoldouts.Clear();
			ObjectTrackedComponents.Clear();
		}
	}

	// ��ܤ����ܿ��]�w�ץ� Lambda Closure ���D�^
	private void ShowComponentPicker(GameObject go)
	{
		GenericMenu menu = new GenericMenu();
		Component[] comps = go.GetComponents<Component>();

		foreach (var comp in comps)
		{
			if (comp == null) continue;

			// �����ϰ��ܼơA�קK Lambda ���]���~
			GameObject targetGO = go;
			Component targetComp = comp;

			menu.AddItem(new GUIContent(comp.GetType().Name), false, () =>
			{
				// �[�J GameObject�]�p�G�|���s�b�^
				if (!TrackedObjects.Contains(targetGO))
				{
					TrackedObjects.Add(targetGO);
					ObjectFoldouts[targetGO] = true;
				}

				// �T�O Dictionary �w��l��
				if (!ObjectTrackedComponents.ContainsKey(targetGO))
				{
					ObjectTrackedComponents[targetGO] = new List<Component>();
				}

				// �[�J����
				if (!ObjectTrackedComponents[targetGO].Contains(targetComp))
				{
					ObjectTrackedComponents[targetGO].Add(targetComp);
				}
			});
		}

		menu.ShowAsContext();
	}

	// ��ܤ��󪺩Ҧ����
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

			// ��� Header�]�䴩�h�h attribute�^
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

	// �ۭq�����ܱƪ��]�[�j label �P value �����Z�^
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
					EditorGUILayout.LabelField($"���䴩�������G{type.Name}");
			}

			EditorGUILayout.EndHorizontal();
			return result;
		}
		catch
		{
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField(label, "���~�G�L�k���");
			return null;
		}
	}

	// ���o���������� Header �W��
	private string GetHeaderOrTypeName(Component component)
	{
		var headerAttr = component.GetType().GetCustomAttribute<HeaderAttribute>();
		if (headerAttr != null && !string.IsNullOrWhiteSpace(headerAttr.header))
			return headerAttr.header;
		else
			return component.GetType().Name;
	}

	// �N�ܼƦW���ର�u�C�ӳ�r�j�g�ϪŮ�v
	private string FormatFieldName(string rawName)
	{
		string spaced = Regex.Replace(rawName, "(\\B[A-Z])", " $1").Replace("_", " ").Trim();
		return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(spaced.ToLower());
	}
}
