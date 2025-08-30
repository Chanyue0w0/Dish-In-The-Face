using UnityEngine;
using System;
using UnityEngine.Events;

public class BeGrabByPlayer : MonoBehaviour
{
    [SerializeField] private bool isCanBeGrabByPlayer = false;
    [SerializeField] private bool isOnBeGrabing = false;

    // 讓你可在 Inspector 綁事件（可選）
    [Header("Events (optional)")]
    [SerializeField] private UnityEvent onGrabbed;   // SetIsOnBeGrabing(true)
    [SerializeField] private UnityEvent onReleased;  // SetIsOnBeGrabing(false)

    // 讓你在程式碼動態註冊要做的事（Action）
    private event Action grabbedActions;   // true 時呼叫
    private event Action releasedActions;  // false 時呼叫

    // ---- Get/Set ----
    public bool GetIsCanBeGrabByPlayer() => isCanBeGrabByPlayer;
    public void SetIsCanBeGrabByPlayer(bool value) => isCanBeGrabByPlayer = value;

    
    // 範例
    // var grabbable = GetComponent<BeGrabByPlayer>();
    // grabbable.RegisterOnBeGrabbingAction(true,  () => Debug.Log("被抓起時做的事"));
    // grabbable.RegisterOnBeGrabbingAction(false, () => Debug.Log("被放下/丟出時做的事"));
    
    /// <summary>
    /// 設定「目前是否被玩家抓著」。會依 value 觸發對應的事件/Action。
    /// </summary>
    public void SetIsOnBeGrabing(bool value)
    {
        if (isOnBeGrabing == value) return;
        isOnBeGrabing = value;

        if (value)
        {
            // true：被抓起
            onGrabbed?.Invoke();
            grabbedActions?.Invoke();
        }
        else
        {
            // false：被放下/丟出
            onReleased?.Invoke();
            releasedActions?.Invoke();
        }
    }

    /// <summary> 取得目前是否處於「被抓著」狀態。 </summary>
    public bool GetIsOnBeGrabing() => isOnBeGrabing;

    // 為了相容舊名稱，保留同名方法，但正確回傳「是否被抓著」
    public bool GetIsCanBeGrabing() => isOnBeGrabing;

    // ---- 新增：註冊/移除 Action（依 true/false 分流）----

    /// <summary>
    /// 註冊在 SetIsOnBeGrabing 被呼叫時要執行的 Action。
    /// whenValueIsTrue = true → 註冊到「被抓起」；false → 註冊到「被放下/丟出」。
    /// </summary>
    public void RegisterOnBeGrabbingAction(bool whenValueIsTrue, Action action)
    {
        if (action == null) return;
        if (whenValueIsTrue) grabbedActions += action;
        else                 releasedActions += action;
    }

    /// <summary> 取消先前註冊的 Action。 </summary>
    public void UnregisterOnBeGrabbingAction(bool whenValueIsTrue, Action action)
    {
        if (action == null) return;
        if (whenValueIsTrue) grabbedActions -= action;
        else                 releasedActions -= action;
    }

    /// <summary> 清空已註冊的 Action。 </summary>
    public void ClearOnBeGrabbingActions(bool forTrueGroup)
    {
        if (forTrueGroup) grabbedActions = null;
        else              releasedActions = null;
    }
}
