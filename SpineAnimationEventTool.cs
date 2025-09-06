// Assets/Editor/SpineAnimationEventTool.cs

using System.Collections.Generic;
using System.Linq;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

public class SpineAnimationEventTool : EditorWindow
{
    private AnimationClip clip;
    private SkeletonAnimation skeletonAnimation;     // 優先：直接拖場景/Prefab上的 SkeletonAnimation
    private SkeletonDataAsset skeletonDataAsset;     // 或者：直接拖 SkeletonDataAsset
    private string targetFunction = "PlaySpineOnce"; // 你打算用哪個事件方法帶 string 參數
    private string filter = "";
    private Vector2 scroll;

    // Spine 動畫名列表 & 快速索引
    private List<string> spineAnimNames = new List<string>();
    private HashSet<string> animNameSet = new HashSet<string>();

    [MenuItem("Tools/Spine/Animation Event Tool")]
    public static void Open()
    {
        GetWindow<SpineAnimationEventTool>("Spine Anim Event Tool");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Spine Animation Event Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        // 來源選擇
        clip = (AnimationClip)EditorGUILayout.ObjectField(new GUIContent("Animation Clip", "要編輯事件的 Clip"), clip, typeof(AnimationClip), false);
        skeletonAnimation = (SkeletonAnimation)EditorGUILayout.ObjectField(new GUIContent("SkeletonAnimation (可選)"), skeletonAnimation, typeof(SkeletonAnimation), true);
        skeletonDataAsset = (SkeletonDataAsset)EditorGUILayout.ObjectField(new GUIContent("SkeletonDataAsset (可選)"), skeletonDataAsset, typeof(SkeletonDataAsset), false);

        targetFunction = EditorGUILayout.TextField(new GUIContent("Event Function Name", "事件要呼叫的方法（帶 string 參數）"), targetFunction);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("載入 Spine 動畫清單"))
        {
            LoadSpineAnimations();
        }

        using (new EditorGUI.DisabledScope(spineAnimNames.Count == 0))
        {
            EditorGUILayout.LabelField($"已載入 {spineAnimNames.Count} 個 Spine 動畫", EditorStyles.miniLabel);
            filter = EditorGUILayout.TextField(new GUIContent("名稱過濾 (可空白)"), filter);
        }

        EditorGUILayout.Space(8);

        if (!clip)
        {
            EditorGUILayout.HelpBox("請先指定 Animation Clip。", MessageType.Info);
            return;
        }
        if (spineAnimNames.Count == 0)
        {
            EditorGUILayout.HelpBox("請先載入 Spine 動畫清單（SkeletonAnimation 或 SkeletonDataAsset 需其一提供）。", MessageType.Warning);
        }

        DrawEventEditor();
        EditorGUILayout.Space(8);
        DrawAddEventSection();
    }

    private void LoadSpineAnimations()
    {
        spineAnimNames.Clear();
        animNameSet.Clear();

        SkeletonData data = null;

        if (skeletonAnimation && skeletonAnimation.Skeleton != null)
        {
            // 來自場景/Prefab 實例
            data = skeletonAnimation.Skeleton.Data;
        }
        else if (skeletonDataAsset)
        {
            data = skeletonDataAsset.GetSkeletonData(true);
        }

        if (data == null)
        {
            EditorUtility.DisplayDialog("載入失敗", "無法從 SkeletonAnimation 或 SkeletonDataAsset 取得 SkeletonData。", "OK");
            return;
        }

        foreach (var anim in data.Animations)
        {
            if (anim == null) continue;
            spineAnimNames.Add(anim.Name);
        }

        spineAnimNames = spineAnimNames.Distinct().OrderBy(n => n).ToList();
        animNameSet = new HashSet<string>(spineAnimNames);
        Repaint();
    }

    private void DrawEventEditor()
    {
        var events = AnimationUtility.GetAnimationEvents(clip);
        if (events == null) events = new AnimationEvent[0];

        EditorGUILayout.LabelField("現有事件", EditorStyles.boldLabel);
        if (events.Length == 0)
        {
            EditorGUILayout.HelpBox("此 Clip 目前沒有事件。", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < events.Length; i++)
        {
            var e = events[i];
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Event {i}", GUILayout.Width(70));
            float newTime = EditorGUILayout.FloatField("Time (sec)", e.time);
            if (!Mathf.Approximately(newTime, e.time))
            {
                e.time = Mathf.Max(0f, newTime);
                events[i] = e;
            }
            if (GUILayout.Button("刪除此事件", GUILayout.Width(100)))
            {
                var list = events.ToList();
                list.RemoveAt(i);
                ApplyEvents(list.ToArray(), "移除事件");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            // 方法名稱
            string newFunc = EditorGUILayout.TextField("Function", e.functionName);
            if (newFunc != e.functionName)
            {
                e.functionName = newFunc;
                events[i] = e;
            }

            // 參數顯示：如果 functionName == 目標函數，就用下拉；否則照原樣可編輯
            if (!string.IsNullOrEmpty(targetFunction) && e.functionName == targetFunction && spineAnimNames.Count > 0)
            {
                // 目前的字串參數
                string current = e.stringParameter;

                // 過濾後的候選
                var candidates = string.IsNullOrEmpty(filter)
                    ? spineAnimNames
                    : spineAnimNames.Where(n => n.ToLower().Contains(filter.ToLower())).ToList();

                int idx = Mathf.Max(0, candidates.IndexOf(current));
                if (idx < 0) idx = 0;

                int newIdx = EditorGUILayout.Popup("Spine Animation", idx, candidates.ToArray());
                string chosen = (candidates.Count > 0) ? candidates[Mathf.Clamp(newIdx, 0, candidates.Count - 1)] : "";

                if (chosen != current)
                {
                    e.stringParameter = chosen;
                    events[i] = e;
                }

                // 額外顯示完整清單索引（只讀）
                EditorGUILayout.LabelField("目前字串參數", e.stringParameter);
            }
            else
            {
                // 非目標函數或尚未載入清單 → 照原始字串參數顯示
                string sp = EditorGUILayout.TextField("stringParameter", e.stringParameter);
                if (sp != e.stringParameter)
                {
                    e.stringParameter = sp;
                    events[i] = e;
                }
            }

            // 其他原生參數仍可調
            float fp = EditorGUILayout.FloatField("floatParameter", e.floatParameter);
            if (!Mathf.Approximately(fp, e.floatParameter))
            {
                e.floatParameter = fp;
                events[i] = e;
            }

            int ip = EditorGUILayout.IntField("intParameter", e.intParameter);
            if (ip != e.intParameter)
            {
                e.intParameter = ip;
                events[i] = e;
            }

            var obj = (Object)EditorGUILayout.ObjectField("objectReferenceParameter", e.objectReferenceParameter, typeof(Object), true);
            if (obj != e.objectReferenceParameter)
            {
                e.objectReferenceParameter = obj;
                events[i] = e;
            }

            // 套用此事件變更
            if (GUILayout.Button("套用此事件變更"))
            {
                events[i] = e;
                ApplyEvents(events, "修改事件");
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);
        if (GUILayout.Button("套用全部變更"))
        {
            ApplyEvents(events, "修改全部事件");
        }
    }

    private void DrawAddEventSection()
    {
        EditorGUILayout.LabelField("新增事件", EditorStyles.boldLabel);

        // 預設新增一個「使用目標函數」的事件
        using (new EditorGUI.DisabledScope(spineAnimNames.Count == 0 || string.IsNullOrEmpty(targetFunction)))
        {
            EditorGUILayout.HelpBox("新增一個事件至目前時間的尾端（可再手動調整時間）。", MessageType.None);
            if (GUILayout.Button($"新增事件（函數：{targetFunction}，用下拉選單選 Spine Clip）"))
            {
                var newEvent = new AnimationEvent
                {
                    time = GetClipLengthSafe(clip), // 先加在尾端
                    functionName = targetFunction,
                    stringParameter = spineAnimNames.Count > 0 ? spineAnimNames[0] : ""
                };

                var list = AnimationUtility.GetAnimationEvents(clip).ToList();
                list.Add(newEvent);
                ApplyEvents(list.ToArray(), "新增事件");
            }
        }

        // 也允許新增「自定義函數名」的事件（手動填字串）
        if (GUILayout.Button("新增一般事件（手動輸入函數名/參數）"))
        {
            var newEvent = new AnimationEvent
            {
                time = GetClipLengthSafe(clip),
                functionName = "FunctionName",
                stringParameter = ""
            };

            var list = AnimationUtility.GetAnimationEvents(clip).ToList();
            list.Add(newEvent);
            ApplyEvents(list.ToArray(), "新增一般事件");
        }
    }

    private void ApplyEvents(AnimationEvent[] events, string undoLabel)
    {
        if (!clip) return;
        Undo.RecordObject(clip, undoLabel);
        AnimationUtility.SetAnimationEvents(clip, events);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        Repaint();
    }

    private static float GetClipLengthSafe(AnimationClip c)
    {
        if (!c) return 0f;
        // 對非 legacy clip：length 可能是 0，如果沒曲線；保底用 0
        return Mathf.Max(0f, c.length);
    }
}
