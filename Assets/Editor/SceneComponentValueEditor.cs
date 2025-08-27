using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using UnityEngine.SceneManagement;

public class SceneComponentValueEditor : EditorWindow
{
	private const string SAVE_PATH = "Assets/Editor/Data/ComponentList.json";

	private Vector2 ScrollPosition;

	private List<GameObject> TrackedObjects = new();
	private Dictionary<GameObject, bool> ObjectFoldouts = new();
	private Dictionary<Component, bool> ComponentFoldouts = new();
	private Dictionary<GameObject, List<Component>> ObjectTrackedComponents = new();

	private bool ShowUnsupportedFieldTypes = false;
	private bool ShowPrivateFieldsWithoutSerializeField = false;

	private Dictionary<string, List<TrackedComponentInfo>> SceneSavedObjects = new();

	[InitializeOnLoadMethod]
	private static void InitSceneChangeListener()
	{
		EditorSceneManager.activeSceneChangedInEditMode += (oldScene, newScene) =>
		{
			if (HasOpenInstances<SceneComponentValueEditor>())
			{
				var window = GetWindow<SceneComponentValueEditor>();
				window.LoadTrackedObjectsFromJson();
			}
		};
	}

	[MenuItem("Tools/Scene Component Value Editor")]
	public static void ShowWindow()
	{
		GetWindow<SceneComponentValueEditor>("Scene Component Editor");
	}

	private void OnEnable()
	{
		LoadTrackedObjectsFromJson();
	}

	void OnGUI()
	{
		EditorGUILayout.LabelField("場景組件值追蹤工具", EditorStyles.boldLabel);
		EditorGUILayout.Space();

		ShowUnsupportedFieldTypes = GUILayout.Toggle(ShowUnsupportedFieldTypes, "顯示不支援的類型");
		ShowPrivateFieldsWithoutSerializeField = GUILayout.Toggle(ShowPrivateFieldsWithoutSerializeField, "顯示沒有 [SerializeField] 的 private 欄位");

		EditorGUILayout.Space();

		if (GUILayout.Button("加入選擇物件的所有組件"))
		{
			GameObject selected = Selection.activeGameObject;
			if (selected != null)
			{
				AddObjectWithAllComponents(selected);
			}
		}

		if (GUILayout.Button("選擇要加入的組件"))
		{
			GameObject selected = Selection.activeGameObject;
			if (selected != null)
			{
				ShowComponentPicker(selected);
			}
		}

		if (GUILayout.Button("重新載入當前場景已儲存清單"))
		{
			LoadTrackedObjectsFromJson();
		}

		EditorGUILayout.Space();
		ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);

		for (int i = 0; i < TrackedObjects.Count; i++)
		{
			GameObject go = TrackedObjects[i];
			if (go == null) continue;

			bool shouldSkip = false;

			EditorGUILayout.BeginHorizontal();
			ObjectFoldouts.TryGetValue(go, out bool goOpen);
			goOpen = EditorGUILayout.Foldout(goOpen, $"物件：{go.name}", true);
			ObjectFoldouts[go] = goOpen;

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

					string label = comp.GetType().Name;
					compOpen = EditorGUILayout.Foldout(compOpen, $"組件：{label}", true);
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

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("清空追蹤清單"))
		{
			TrackedObjects.Clear();
			ObjectFoldouts.Clear();
			ComponentFoldouts.Clear();
			ObjectTrackedComponents.Clear();
		}
		if (GUILayout.Button("儲存當前清單"))
		{
			SaveTrackedObjectsToJson();
		}
		EditorGUILayout.EndHorizontal();
	}

	void AddObjectWithAllComponents(GameObject go)
	{
		if (!TrackedObjects.Contains(go))
		{
			TrackedObjects.Add(go);
			ObjectFoldouts[go] = true;
			ObjectTrackedComponents[go] = new List<Component>();
		}

		Component[] components = go.GetComponents<Component>();
		foreach (var comp in components)
		{
			if (!ObjectTrackedComponents[go].Contains(comp))
				ObjectTrackedComponents[go].Add(comp);
		}
	}

	void ShowComponentPicker(GameObject go)
	{
		GenericMenu menu = new GenericMenu();
		Component[] comps = go.GetComponents<Component>();

		foreach (var comp in comps)
		{
			if (comp == null) continue;

			GameObject targetGO = go;
			Component targetComp = comp;

			menu.AddItem(new GUIContent(comp.GetType().Name), false, () =>
			{
				if (!TrackedObjects.Contains(targetGO))
				{
					TrackedObjects.Add(targetGO);
					ObjectTrackedComponents[targetGO] = new List<Component>();
				}

				if (!ObjectTrackedComponents[targetGO].Contains(targetComp))
				{
					ObjectTrackedComponents[targetGO].Add(targetComp);
				}
			});
		}

		menu.ShowAsContext();
	}

	void DrawAllFields(Component component)
	{
		BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		var fields = component.GetType().GetFields(flags);
		string lastHeader = null;

		foreach (var field in fields)
		{
			if (field.IsDefined(typeof(HideInInspector), true)) continue;

			bool isPublic = field.IsPublic;
			bool isPrivate = !isPublic;
			if (isPrivate && !field.IsDefined(typeof(SerializeField), false))
			{
				if (!ShowPrivateFieldsWithoutSerializeField)
					continue;
			}

			var headerAttrs = field.GetCustomAttributes(typeof(HeaderAttribute), true);
			if (headerAttrs.Length > 0)
			{
				var headerAttr = (HeaderAttribute)headerAttrs[0];
				if (headerAttr.header != lastHeader)
				{
					EditorGUILayout.Space(8);
					EditorGUILayout.LabelField(headerAttr.header, EditorStyles.boldLabel);
					lastHeader = headerAttr.header;
				}
			}

			var spaceAttr = field.GetCustomAttribute<SpaceAttribute>();
			if (spaceAttr != null)
			{
				EditorGUILayout.Space(spaceAttr.height);
			}

			try
			{
				object val = field.GetValue(component);
				object newVal = DrawField(FormatFieldName(field.Name), val, field.FieldType);

				if (newVal == null && !ShowUnsupportedFieldTypes)
					continue;

				if (newVal != null && !Equals(val, newVal))
				{
					Undo.RecordObject(component, "Change Field Value");
					field.SetValue(component, newVal);
					EditorUtility.SetDirty(component);
				}
			}
			catch (Exception e)
			{
				EditorGUILayout.LabelField(field.Name, $"錯誤：{e.Message}");
			}
		}
	}

	object DrawField(string label, object value, Type type)
	{
		try
		{
			if (type == typeof(int))
				return EditorGUILayout.IntField(label, (int)value);
			else if (type == typeof(float))
				return EditorGUILayout.FloatField(label, (float)value);
			else if (type == typeof(bool))
				return EditorGUILayout.Toggle(label, (bool)value);
			else if (type == typeof(string))
				return EditorGUILayout.TextField(label, (string)value);
			else if (type == typeof(Vector3))
				return EditorGUILayout.Vector3Field(label, (Vector3)value);
			else if (type == typeof(Vector2))
				return EditorGUILayout.Vector2Field(label, (Vector2)value);
			else if (type == typeof(Color))
				return EditorGUILayout.ColorField(label, (Color)value);
			else
			{
				if (ShowUnsupportedFieldTypes)
					EditorGUILayout.LabelField(label, $"不支援的類型：{type.Name}");
				return null;
			}
		}
		catch
		{
			EditorGUILayout.LabelField(label, "錯誤：無法讀取");
			return null;
		}
	}

	private string FormatFieldName(string rawName)
	{
		string spaced = Regex.Replace(rawName, "(\\B[A-Z])", " $1").Replace("_", " ").Trim();
		return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(spaced.ToLower());
	}

	string GetGameObjectPath(GameObject obj)
	{
		string path = obj.name;
		Transform current = obj.transform;
		while (current.parent != null)
		{
			current = current.parent;
			path = current.name + "/" + path;
		}
		return path;
	}

	void SaveTrackedObjectsToJson()
	{
		string sceneName = SceneManager.GetActiveScene().name;
		List<TrackedComponentInfo> dataList = new();

		foreach (var go in TrackedObjects)
		{
			if (!ObjectTrackedComponents.ContainsKey(go)) continue;
			foreach (var comp in ObjectTrackedComponents[go])
			{
				if (comp == null) continue;
				dataList.Add(new TrackedComponentInfo
				{
					path = GetGameObjectPath(go),
					type = comp.GetType().AssemblyQualifiedName
				});
			}
		}

		if (!Directory.Exists("Assets/Editor/Data"))
			Directory.CreateDirectory("Assets/Editor/Data");

		Dictionary<string, List<TrackedComponentInfo>> dataToSave = new();
		if (File.Exists(SAVE_PATH))
		{
			string json = File.ReadAllText(SAVE_PATH);
			dataToSave = JsonUtility.FromJson<SerializableSceneComponents>(json).ToDictionary();
		}

		dataToSave[sceneName] = dataList;
		string finalJson = JsonUtility.ToJson(new SerializableSceneComponents(dataToSave), true);
		File.WriteAllText(SAVE_PATH, finalJson);
		AssetDatabase.Refresh();
	}

	void LoadTrackedObjectsFromJson()
	{
		string sceneName = SceneManager.GetActiveScene().name;
		if (!File.Exists(SAVE_PATH)) return;

		string json = File.ReadAllText(SAVE_PATH);
		SceneSavedObjects = JsonUtility.FromJson<SerializableSceneComponents>(json).ToDictionary();
		if (!SceneSavedObjects.ContainsKey(sceneName)) return;

		TrackedObjects.Clear();
		ObjectFoldouts.Clear();
		ComponentFoldouts.Clear();
		ObjectTrackedComponents.Clear();

		foreach (var info in SceneSavedObjects[sceneName])
		{
			foreach (var go in GameObject.FindObjectsOfType<GameObject>())
			{
				if (GetGameObjectPath(go) == info.path)
				{
					Type t = Type.GetType(info.type);
					if (t == null) continue;
					Component comp = go.GetComponent(t);
					if (comp != null)
					{
						if (!TrackedObjects.Contains(go))
						{
							TrackedObjects.Add(go);
							ObjectTrackedComponents[go] = new List<Component>();
						}
						if (!ObjectTrackedComponents[go].Contains(comp))
						{
							ObjectTrackedComponents[go].Add(comp);
						}
					}
				}
			}
		}
	}

	[Serializable]
	public class TrackedComponentInfo
	{
		public string path;
		public string type;
	}

	[Serializable]
	public class SerializableSceneComponents
	{
		public List<SceneComponentEntry> scenes = new();

		public SerializableSceneComponents() { }

		public SerializableSceneComponents(Dictionary<string, List<TrackedComponentInfo>> dict)
		{
			foreach (var kv in dict)
				scenes.Add(new SceneComponentEntry { sceneName = kv.Key, components = kv.Value });
		}

		public Dictionary<string, List<TrackedComponentInfo>> ToDictionary()
		{
			Dictionary<string, List<TrackedComponentInfo>> result = new();
			foreach (var entry in scenes)
				result[entry.sceneName] = entry.components;
			return result;
		}
	}

	[Serializable]
	public class SceneComponentEntry
	{
		public string sceneName;
		public List<TrackedComponentInfo> components;
	}
}
