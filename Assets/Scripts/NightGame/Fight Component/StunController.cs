using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class StunController : MonoBehaviour
{
    [Header("----- Runtime Values -----")]
    [SerializeField] private int currentStunValue = 0;        // 目前暈眩值
    [Tooltip("各級暈眩持續秒數，長度須與 stunLevelGates 一致")]
    [SerializeField] private float[] stunTimes;               // 各級暈眩秒數（對應門檻索引）
    [Tooltip("暈眩門檻（嚴格遞增）。例如：{3, 6, 10}")]
    [SerializeField] private int[] stunLevelGates;            // 暈眩門檻

    [Header("----- UI (Optional) -----")]
    [SerializeField] private bool showBarUI = true;   // 是否顯示暈眩條
    [SerializeField] private GameObject stunBar;              // 暈眩條 parent（可為空）
    [SerializeField] private Transform stunBarFill;           // 暈眩條填充（localScale.x：0~1，可為空）
    [SerializeField] private GameObject[] stunStarVFXs;       // 依等級對應的暈眩特效（0->Lv1, 1->Lv2, ...）

    [Header("----- Events -----")]
    public UnityEvent onStunFull;                             // 進入/升級暈眩時觸發
    public UnityEvent onStunRecovered;                        // （最終）暈眩結束時觸發

    [Header("----- Input / Trigger -----")]
    [SerializeField] private string stunTriggerTag = "AttackStun"; // 造成暈眩的攻擊 Trigger Tag

    // 狀態
    private bool isStunned = false;               // 是否處於暈眩倒數中
    private int activeStunLevelIndex = -1;        // 目前暈眩等級（-1 = 不在暈眩）
    private Coroutine stunRoutine = null;

    #region Unity Life Cycle
    private void Awake()
    {
        ValidateConfig();
        SetBarActive(false);
        UpdateBarInstant();
        HideAllStunVFX();
    }

    private void OnEnable()
    {
        SetBarActive(false);
        UpdateBarInstant();
        HideAllStunVFX();
    }

    /// <summary>
    /// 2D 觸發：當有攜帶 stunTriggerTag 的 Trigger 進入時，增加暈眩值
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || string.IsNullOrEmpty(stunTriggerTag)) return;
        if (!other.CompareTag(stunTriggerTag)) return;

        // 從攻擊身上讀取傷害與是否貫穿（若沒有元件就忽略）
        AttackDataInfo aktInfo = other.GetComponentInParent<AttackDataInfo>();
        if (!aktInfo) return;

        // 受擊 → 顯示 UI 並累積
        SetBarActive(true);
        AddStun(aktInfo.attackValue, aktInfo.isPiercing);
    }
    #endregion

    #region Public API
    /// <summary>
    /// 手動增加暈眩值（暈眩中也可累積）。
    /// isPiercing=true：直接加到 newValue=current+amount（可跨越多個門檻）；
    /// isPiercing=false：只加到「目前區段上限」（下一個門檻值），不會超過該門檻。
    /// </summary>
    public void AddStun(int amount, bool isPiercing)
    {
        if (!ConfigUsable()) return;
        if (amount == 0) return;

        int before = currentStunValue;
        int after;

        if (isPiercing)
        {
            // 直接加總，可超過最後門檻
            after = Mathf.Max(0, before + amount);
        }
        else
        {
            // 非貫穿：只加到目前區段上限（下一個門檻）
            int nextGate = GetNextGateValue(before); // 若已在最後段，回傳最後門檻值
            // 只能往 nextGate 靠近，但不可超過
            long tentative = (long)before + amount; // 用 long 避免極端大數溢位
            if (amount > 0)
                after = (int)Mathf.Min(nextGate, Mathf.Max(0, (int)tentative));
            else
                after = Mathf.Max(0, (int)tentative); // 負值時正常遞減
        }

        currentStunValue = after;

        // UI 立即更新（以區段百分比顯示）
        SetBarActive(true);
        UpdateBarInstant();

        // 檢查是否跨過新門檻
        int achievedLevel = GetAchievedLevelIndex(currentStunValue); // 最大 i 使得 current >= gate[i]，若沒有則 -1

        if (!isStunned)
        {
            // 未暈眩 → 若已跨過任一門檻則進入該等級暈眩
            if (achievedLevel >= 0)
                EnterOrEscalateStun(achievedLevel);
        }
        else
        {
            // 已在暈眩 → 若達到更高門檻則升級並重置計時
            if (achievedLevel > activeStunLevelIndex)
                EnterOrEscalateStun(achievedLevel);
            // 若沒有升級，維持原本倒數繼續跑
        }
    }

    /// <summary>重置暈眩系統（清除暈眩、值歸零、關閉 UI 與特效）。</summary>
    public void ResetStun()
    {
        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
            stunRoutine = null;
        }
        isStunned = false;
        activeStunLevelIndex = -1;

        currentStunValue = 0;
        UpdateBarInstant();
        SetBarActive(false);
        HideAllStunVFX();
    }

    public int GetCurrentStun() => currentStunValue;
    public bool IsStunned() => isStunned;
    #endregion

    #region Core Flow
    /// <summary>進入或升級暈眩：以指定等級重置倒數並觸發 onStunFull；切換對應等級特效。</summary>
    private void EnterOrEscalateStun(int newLevelIndex)
    {
        newLevelIndex = Mathf.Clamp(newLevelIndex, 0, stunLevelGates.Length - 1);

        activeStunLevelIndex = newLevelIndex;
        isStunned = true;

        // 條顯示保持對應區段比例（或已超最後門檻則滿條）
        UpdateBarInstant();

        // 顯示對應等級特效
        UpdateStunVFX(activeStunLevelIndex);

        onStunFull?.Invoke();

        if (stunRoutine != null) StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(StunCountdown(stunTimes[newLevelIndex]));
    }

    /// <summary>暈眩倒數；若期間又升級，會被 Stop 並以新時間重啟。</summary>
    private IEnumerator StunCountdown(float seconds)
    {
        float t = Mathf.Max(0f, seconds);
        if (t > 0f) yield return new WaitForSeconds(t);

        // 暈眩結束（若期間沒升級到更高等級）
        isStunned = false;

        // 恢復邏輯：
        // - 若 current 曾「超過」最後門檻 → 直接歸 0
        // - 否則退回到「上一門檻」：i>0 → gate[i-1]；i==0 → 0
        int lastGateIndex = stunLevelGates.Length - 1;
        bool beyondLast = currentStunValue > stunLevelGates[lastGateIndex];

        if (beyondLast)
        {
            currentStunValue = 0;
        }
        else
        {
            int i = Mathf.Clamp(activeStunLevelIndex, 0, lastGateIndex);
            currentStunValue = (i > 0) ? stunLevelGates[i - 1] : 0;
        }

        onStunRecovered?.Invoke();

        activeStunLevelIndex = -1;
        UpdateBarInstant();

        // 若恢復後沒有任何值可顯示，則關閉條
        if (currentStunValue <= 0) SetBarActive(false);

        // 暈眩結束 → 關閉全部特效
        HideAllStunVFX();

        stunRoutine = null;
    }
    #endregion

    #region UI Helpers
    private void SetBarActive(bool active)
    {
        if (!showBarUI) 
        {
            if (stunBar && stunBar.activeSelf) stunBar.SetActive(false);
            return;
        }

        if (stunBar && stunBar.activeSelf != active)
            stunBar.SetActive(active);
    }

    /// <summary>
    /// 以「區段百分比」更新條：
    /// ratio = (current - prevGate) / (nextGate - prevGate)
    /// 若 current > 最後門檻 → ratio = 1。
    /// </summary>
    private void UpdateBarInstant()
    {
        if (!showBarUI)
        {
            SetFill01(0f);
            return;
        }

        if (!stunBarFill || !ConfigUsable())
        {
            SetFill01(0f);
            return;
        }

        int lastIndex = stunLevelGates.Length - 1;

        // 超過最後門檻 → 滿條
        if (currentStunValue > stunLevelGates[lastIndex])
        {
            SetFill01(1f);
            return;
        }

        // 找到「目前所在區段」：以 nextGate 為第一個 >= current 的門檻
        int nextIdx = 0;
        while (nextIdx < stunLevelGates.Length && currentStunValue > stunLevelGates[nextIdx])
            nextIdx++;

        if (nextIdx == 0)
        {
            // 第一段：prev = 0, next = gate[0]
            float prev = 0f;
            float next = Mathf.Max(1, stunLevelGates[0]);
            float ratio = Mathf.Clamp01((currentStunValue - prev) / (next - prev));
            SetFill01(ratio);
        }
        else
        {
            // 一般段：prev = gate[nextIdx-1], next = (nextIdx 在範圍內 ? gate[nextIdx] : gate[last])
            float prev = stunLevelGates[nextIdx - 1];
            float next = (nextIdx < stunLevelGates.Length) ? stunLevelGates[nextIdx] : stunLevelGates[lastIndex];
            float denom = Mathf.Max(1f, next - prev);
            float ratio = Mathf.Clamp01((currentStunValue - prev) / denom);
            SetFill01(ratio);
        }
    }

    private void SetFill01(float x01)
    {
        if (!stunBarFill) return;
        var s = stunBarFill.localScale;
        s.x = x01;
        stunBarFill.localScale = s;
    }

    /// <summary>關閉所有暈眩特效。</summary>
    private void HideAllStunVFX()
    {
        if (stunStarVFXs == null) return;
        for (int i = 0; i < stunStarVFXs.Length; i++)
        {
            if (stunStarVFXs[i])
                stunStarVFXs[i].SetActive(false);
        }
    }

    /// <summary>只開啟指定等級的特效（其餘關閉）。levelIndex：0 -> Lv1, 1 -> Lv2 ...</summary>
    private void UpdateStunVFX(int levelIndex)
    {
        if (stunStarVFXs == null || stunStarVFXs.Length == 0) return;

        int idx = Mathf.Clamp(levelIndex, 0, stunStarVFXs.Length - 1);

        for (int i = 0; i < stunStarVFXs.Length; i++)
        {
            if (!stunStarVFXs[i]) continue;
            stunStarVFXs[i].SetActive(i == idx);
        }
    }
    #endregion

    #region Helpers & Validation
    /// <summary>回傳「已達成的最高門檻索引」。若尚未達任何門檻則回傳 -1。</summary>
    private int GetAchievedLevelIndex(int value)
    {
        if (!ConfigUsable()) return -1;
        int idx = -1;
        for (int i = 0; i < stunLevelGates.Length; i++)
        {
            if (value >= stunLevelGates[i]) idx = i;
            else break;
        }
        return idx;
    }

    /// <summary>
    /// 回傳「下一個門檻值」。若 current 已在或超過最後門檻，回傳最後門檻值本身。
    /// 用於非貫穿攻擊的「區段上限」計算。
    /// </summary>
    private int GetNextGateValue(int current)
    {
        if (!ConfigUsable()) return current;
        int lastIdx = stunLevelGates.Length - 1;
        for (int i = 0; i <= lastIdx; i++)
        {
            if (current < stunLevelGates[i])
                return stunLevelGates[i];
        }
        return stunLevelGates[lastIdx];
    }

    private bool ConfigUsable()
    {
        return stunLevelGates != null && stunTimes != null &&
               stunLevelGates.Length > 0 &&
               stunTimes.Length == stunLevelGates.Length;
    }

    private void ValidateConfig()
    {
        // 陣列長度檢查
        if (stunLevelGates == null || stunTimes == null || stunLevelGates.Length == 0 || stunTimes.Length == 0)
        {
            Debug.LogWarning("[StunController] stunLevelGates / stunTimes 未設定或為空。");
            return;
        }
        if (stunTimes.Length != stunLevelGates.Length)
        {
            Debug.LogWarning("[StunController] stunTimes 長度須與 stunLevelGates 相同。");
        }

        // 遞增性檢查與修正提示
        for (int i = 1; i < stunLevelGates.Length; i++)
        {
            if (stunLevelGates[i] <= stunLevelGates[i - 1])
            {
                Debug.LogWarning("[StunController] 建議將 stunLevelGates 設為嚴格遞增。例如：3, 6, 10。");
                break;
            }
        }
    }
    #endregion
}
