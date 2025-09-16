using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// StunController（獨立門檻版）
/// 規則摘要：
/// 1) 累積方式：每次受到攻擊便會累積暈眩值（暈眩期間也可持續累積）
/// 2) 暈眩判定：暈眩值跨過門檻（可一次跨多星）就立刻進入對應等級暈眩
/// 3) 恢復：非暈眩狀態下，若超過 noDamageResetTime 秒未受擊，暈眩值清零
/// 4) 超出累積：超過當前階段門檻會溢位到下一階段（可連跨）
/// 5) 星星顯示：一開始全部關；暈眩時只顯示對應等級那一顆；暈眩結束關閉
/// 6) Event：跨門檻進入暈眩時呼叫 onStunFull；暈眩時間結束時呼叫 onStunRecovered
/// </summary>
public class StunController : MonoBehaviour
{
    [SerializeField] private int currentStunValue;   // 目前暈眩值（0 ~ 總門檻）
    
    [Header("----- Stun Setting (Independent thresholds) -----")]
    [Tooltip("每一星所需新增量（獨立）。例如 [3,6,10] 代表 T1=3, T2=6, T3=10。")]
    [SerializeField, Min(1)] private int[] stunLevelValues = { 3, 6, 10 };
    
    [Tooltip("各星暈眩秒數（索引 = 星數-1）。長度不足會自動補齊為最後一個值。")]
    [SerializeField] private float[] stunTimes = { 3f, 7f, 12f };
    
    [Tooltip("非暈眩狀態下，若超過該秒數未受擊 → 暈眩值清零。")]
    [SerializeField] private float noDamageResetTime = 10f;
    
    [SerializeField] private bool isInvincible = false;       // 無敵不累積
    [SerializeField] private string stunTriggerTag = "AttackStun"; // 造成暈眩的攻擊 Trigger Tag
    
    [Header("UI Setting")]
    [SerializeField] private bool showBarUI = true;           // 是否顯示暈眩條
    [SerializeField] private bool showStarsUI = true;         // 是否顯示星星

    [Header("----- Reference -----")]
    [Tooltip("依序 1,2,3,... 星的圖示（本版暈眩時只亮當前那一顆）。")]
    [SerializeField] private GameObject[] starObjects;        // 可為空（不顯示）
    [SerializeField] private GameObject stunBar;              // 暈眩條 parent（可為空）
    [SerializeField] private Transform stunBarFill;           // 暈眩條填充（localScale.x 0~1，可為空）

    [Header("----- Events -----")]
    public UnityEvent onStunFull;       // 升星（跨門檻進入新的星等暈眩）時觸發
    public UnityEvent onStunRecovered;  // 暈眩倒數結束時觸發

    // ===== Runtime 狀態 =====
    private readonly List<int> levelGateSum = new List<int>(); // 前綴和門檻（T1, T1+T2, T1+T2+T3, …）
    private bool _isStunned;         // 是否暈眩中
    private float _stunRemaining;    // 暈眩剩餘秒數
    private bool _countdownPaused;   // 是否暫停倒數
    private float _lastIncreaseTime; // 最近一次增加的時間（用於 noDamageResetTime）
    private bool _isBeAttack;        // 觸發器防抖（同幀/同次碰撞）

    private int MaxTotalNeeded => levelGateSum.Count > 0 ? levelGateSum[levelGateSum.Count - 1] : 0;

    private void Awake()
    {
        FullReset();
    }

    private void OnEnable()
    {
        FullReset();
    }

    private void Update()
    {
        // 非暈眩狀態 + 長時間未受擊 → 清空
        if (!_isStunned && noDamageResetTime > 0f && _lastIncreaseTime > 0f)
        {
            if (Time.time - _lastIncreaseTime >= noDamageResetTime && currentStunValue > 0)
            {
                currentStunValue = 0;
                if (stunBar != null) stunBar.SetActive(showBarUI && currentStunValue > 0);
                HideAllStars();
                UpdateUI();
            }
        }

        // 暈眩倒數（暈眩中仍可被 AddStun 疊加，若跨下一坎會刷新暈眩時間）
        if (_isStunned && !_countdownPaused)
        {
            _stunRemaining -= Time.deltaTime;
            if (_stunRemaining <= 0f)
            {
                RecoverFromStun();
            }
        }
    }

    // ======== 碰撞吸收攻擊（確保每次被打都會累積） ========
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || !other.CompareTag(stunTriggerTag)) return;
        if (!_isBeAttack)
        {
            var atkInfo = other.GetComponent<AttackDataInfo>();
            if (atkInfo == null)
            {
                Debug.LogWarning("[StunController] Attack collider has no AttackDataInfo.");
                return;
            }
            AddStun(atkInfo.attackValue, atkInfo.isPiercing);
            _isBeAttack = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other || !other.CompareTag(stunTriggerTag)) return;
        _isBeAttack = false;
    }

    // ======== 對外 API ========
    /// <summary>
    /// 增加暈眩值。
    /// 本版不再區分貫穿/不貫穿：永遠可跨越多個門檻（超出累積帶到下一階段）。
    /// 暈眩期間也可持續累積；若跨更高門檻，立即刷新為更高星的暈眩時間。
    /// </summary>
    public void AddStun(int damage, bool _isPiercingUnused)
    {
        if (isInvincible || damage <= 0)
            return;

        // 保障門檻初始化
        if (MaxTotalNeeded <= 0)
            RebuildGates();

        _lastIncreaseTime = Time.time;
        if (showBarUI && stunBar != null) stunBar.SetActive(true);

        int prevStars = GetStarCount(currentStunValue);

        // 超出累積自動溢位（可一次跨多坎）
        currentStunValue = Mathf.Clamp(currentStunValue + damage, 0, MaxTotalNeeded);

        int postStars = GetStarCount(currentStunValue);
        if (postStars > prevStars)
        {
            // 進入/提升暈眩等級：依最終星等給暈眩時間，顯示對應星
            EnterStunForStars(postStars);
            onStunFull?.Invoke();
        }
        else if (_isStunned && postStars > 0)
        {
            // 已在暈眩中但沒有跨過新門檻：可選擇不刷新時間
            // 若你想在暈眩中被持續攻擊就延長當前星級的暈眩時間，取消下兩行註解：
            // int idx = Mathf.Clamp(postStars - 1, 0, stunTimes.Length - 1);
            // _stunRemaining = Mathf.Max(_stunRemaining, stunTimes[idx]); // 僅延長，不縮短
        }

        UpdateUI();
    }

    /// <summary>手動完全重置（外部重置用）。</summary>
    public void FullReset()
    {
        RebuildGates();

        // 調整 stunTimes 長度
        if (stunTimes == null || stunTimes.Length < levelGateSum.Count)
        {
            var list = new List<float>();
            if (stunTimes != null) list.AddRange(stunTimes);
            while (list.Count < levelGateSum.Count)
            {
                list.Add(list.Count > 0 ? list[list.Count - 1] : 3f);
            }
            stunTimes = list.ToArray();
        }

        if (stunBar != null) stunBar.SetActive(showBarUI && currentStunValue > 0);

        _isStunned = false;
        _stunRemaining = 0f;
        _countdownPaused = false;
        _lastIncreaseTime = -999f;
        _isBeAttack = false;

        // ⭐ 一開始關掉全部星星
        HideAllStars();

        UpdateUI();
    }

    /// <summary>暫停/繼續暈眩倒數。</summary>
    public void StunTimePause()    { _countdownPaused = true; }
    public void StunTimeContinue() { if (_isStunned) _countdownPaused = false; }

    public void SetInvincible(bool value) => isInvincible = value;
    public bool IsInvincible() => isInvincible;
    public bool IsStunned() => _isStunned;

    // ======== 內部 ========
    private void RebuildGates()
    {
        levelGateSum.Clear();

        if (stunLevelValues == null || stunLevelValues.Length == 0)
            stunLevelValues = new[] { 3, 6, 10 };

        int runSum = 0;
        for (int i = 0; i < stunLevelValues.Length; i++)
        {
            int v = Mathf.Max(1, stunLevelValues[i]);
            runSum += v;
            levelGateSum.Add(runSum);
        }
    }

    private void EnterStunForStars(int stars)
    {
        if (stars <= 0) return;

        int idx = Mathf.Clamp(stars - 1, 0, stunTimes.Length - 1);
        _isStunned = true;
        _countdownPaused = false;
        _stunRemaining = Mathf.Max(0f, stunTimes[idx]);

        if (showBarUI && stunBar != null) stunBar.SetActive(true);

        // ⭐ 暈眩時只亮對應等級星星
        ShowOnlyStarIndex(idx);
    }

    private void RecoverFromStun()
    {
        _isStunned = false;
        _stunRemaining = 0f;
        _countdownPaused = false;

        // 暈眩結束不強制清零暈眩值（保留 10 秒未受擊清零規則）
        if (stunBar != null && showBarUI) stunBar.SetActive(false);

        // ⭐ 暈眩結束關閉所有星星
        HideAllStars();

        UpdateUI();
        onStunRecovered?.Invoke();
    }

    // ======== UI ========
    private void UpdateUI()
    {
        if (!showBarUI || stunBarFill == null || MaxTotalNeeded <= 0) return;

        float t = Mathf.Clamp01((float)currentStunValue / MaxTotalNeeded);
        var s = stunBarFill.localScale;
        stunBarFill.localScale = new Vector3(t, s.y, s.z);
    }

    private void HideAllStars()
    {
        if (!showStarsUI || starObjects == null || starObjects.Length == 0) return;
        foreach (var star in starObjects)
            if (star) star.SetActive(false);
    }

    private void ShowOnlyStarIndex(int idx)
    {
        if (!showStarsUI || starObjects == null || starObjects.Length == 0) return;

        for (int i = 0; i < starObjects.Length; i++)
        {
            if (starObjects[i]) starObjects[i].SetActive(i == idx);
        }
    }

    // ======== Helper ========
    /// <summary>目前暈眩值對應的星數（>=門檻的數量）。</summary>
    private int GetStarCount(int stunValue)
    {
        if (levelGateSum.Count == 0) return 0;
        int stars = 0;
        for (int i = 0; i < levelGateSum.Count; i++)
        {
            if (stunValue >= levelGateSum[i]) stars++;
            else break;
        }
        return stars;
    }

    /// <summary>取得「下一個」門檻值；若已滿最後門檻則回傳目前值。</summary>
    private int GetNextGate(int current)
    {
        for (int i = 0; i < levelGateSum.Count; i++)
        {
            if (current < levelGateSum[i]) return levelGateSum[i];
        }
        return current; // 已經 >= 最後門檻
    }
}