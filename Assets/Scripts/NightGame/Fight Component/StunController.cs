using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StunController : MonoBehaviour
{
    [Header("----- Stun Setting (Independent thresholds) -----")]
    [SerializeField, Min(1)] private int thresholdStar1 = 3;  // T1：進入第1星所需（累積到 T1 即1星）
    [SerializeField, Min(1)] private int thresholdStar2 = 6;  // T2：從1星進2星「額外」需要
    [SerializeField, Min(1)] private int thresholdStar3 = 10; // T3：從2星進3星「額外」需要
    [SerializeField] private float noDamageResetTime = 10f;   // 幾秒沒受攻擊就清空（僅在非暈眩時）
    [SerializeField] private string stunTriggerTag = "AttackStun";
    [SerializeField] private bool isInvincible = false;       // 無敵不累積

    [Header("Star Stun Time")]
    [SerializeField] private float stunTime1Star = 3f;
    [SerializeField] private float stunTime2Star = 7f;
    [SerializeField] private float stunTime3Star = 12f;

    [Header("UI Setting")]
    [SerializeField] private bool showBarUI = true;   // 是否顯示暈眩條
    [SerializeField] private bool showStarsUI = true; // 是否顯示星星

    [Header("----- Reference -----")]
    [SerializeField] private GameObject[] stars;      // 依序 1,2,3 星的圖示（這版只會亮當前那一顆）
    [SerializeField] private GameObject stunBar;      // 暈眩條 parent
    [SerializeField] private Transform stunBarFill;   // 暈眩條填充（localScale.x 0~1）

    [Header("----- Events -----")]
    public UnityEvent onStunFull;       // 升星（進入新的星等）時觸發
    public UnityEvent onStunRecovered;  // 暈眩倒數結束時觸發

    // ===== Runtime 狀態 =====
    private int totalStun;          // ★ 總暈眩值（0 ~ cum3）
    private int starCount;          // 0~3（由 totalStun 推得）
    private bool isStunned;         // 是否暈眩中
    private float stunRemaining;    // 暈眩剩餘秒數
    private bool countdownPaused;   // 是否暫停倒數
    private float lastIncreaseTime; // 最近一次增加的時間（用於 noDamageResetTime）

    // 累積門檻（由 T1,T2,T3 推得）
    private int cum1, cum2, cum3;

    private void Awake()
    {
        RecomputeCumulativeThresholds();
        if (stunBar != null) stunBar.SetActive(showBarUI);

        totalStun = 0;
        starCount = 0;
        isStunned = false;
        stunRemaining = 0f;
        countdownPaused = false;
        lastIncreaseTime = -999f;

        UpdateUI();
        UpdateStars();
    }

    private void OnEnable()
    {
        RecomputeCumulativeThresholds();
        UpdateUI();
        UpdateStars();
    }

    private void RecomputeCumulativeThresholds()
    {
        cum1 = Mathf.Max(1, thresholdStar1);
        cum2 = Mathf.Max(cum1 + 1, thresholdStar1 + thresholdStar2); // 確保嚴格遞增
        cum3 = Mathf.Max(cum2 + 1, thresholdStar1 + thresholdStar2 + thresholdStar3);
    }

    private void Update()
    {
        // 非暈眩 + 超時未受擊 → 清空
        if (!isStunned && Time.time - lastIncreaseTime >= noDamageResetTime)
        {
            if (totalStun > 0 || starCount > 0)
            {
                totalStun = 0;
                starCount = 0;
                if (stunBar != null && showBarUI) stunBar.SetActive(false);
                UpdateStars();
                UpdateUI();
            }
        }

        // 暈眩倒數
        if (isStunned && !countdownPaused && stunRemaining > 0f)
        {
            stunRemaining -= Time.deltaTime;
            if (stunRemaining <= 0f)
            {
                RecoverFromStun();
            }
        }
    }

    // ======== 碰撞吸收攻擊（確保每次被打都會累積） ========
    private void OnTriggerEnter2D(Collider2D other) => TryAbsorbStunFromCollider(other);
    private void OnTriggerStay2D(Collider2D other)  => TryAbsorbStunFromCollider(other);

    private void TryAbsorbStunFromCollider(Collider2D other)
    {
        if (isInvincible) return;
        if (!other || !other.CompareTag(stunTriggerTag)) return;

        // 若攻擊上有 StunHit，使用其配置與去重邏輯；否則 fallback 1 點、非貫穿
        // if (other.TryGetComponent<StunHit>(out var hit))
        // {
        //     if (hit.TryConsume(gameObject))
        //     {
        //         if (hit.piercing) AddStunPiercing(hit.amount);
        //         else AddStunNonPiercing(hit.amount);
        //     }
        // }
        // else
        // {
            // 無 StunHit 時也要確定能累積
            AddStunNonPiercing(1);
        // }
    }

    // ======== 對外 API ========
    /// <summary>非貫穿：只能把 totalStun 加到下一累積門檻為止，不跨星等。</summary>
    public void AddStunNonPiercing(int amount)
    {
        if (isInvincible || amount <= 0) return;

        lastIncreaseTime = Time.time;
        if (showBarUI && stunBar != null) stunBar.SetActive(true);

        int beforeStar = starCount;

        int nextCap = GetNextCumulativeCap(totalStun);
        // 如果已經是第三星滿了，保持不變
        if (nextCap <= totalStun) {
            UpdateUI();
            return;
        }

        totalStun = Mathf.Min(totalStun + amount, nextCap);
        ClampTotal();
        UpdateStarByTotal(beforeStar);

        UpdateUI();
    }

    /// <summary>貫穿：可跨過多個門檻，直接加到 cum3 封頂。</summary>
    public void AddStunPiercing(int amount)
    {
        if (isInvincible || amount <= 0) return;

        lastIncreaseTime = Time.time;
        if (showBarUI && stunBar != null) stunBar.SetActive(true);

        int beforeStar = starCount;

        totalStun = Mathf.Min(totalStun + amount, cum3);
        ClampTotal();
        UpdateStarByTotal(beforeStar);

        UpdateUI();
    }

    /// <summary>手動完全重置（外部重置用）。</summary>
    public void FullReset()
    {
        isStunned = false;
        stunRemaining = 0f;
        countdownPaused = false;

        totalStun = 0;
        starCount = 0;

        if (stunBar != null && showBarUI) stunBar.SetActive(false);

        UpdateStars();
        UpdateUI();
    }

    /// <summary>只清除條的累積值，不動星星與暈眩狀態（把 total 夾到該星級起點）。</summary>
    public void ResetStunProgress()
    {
        totalStun = GetSegmentStartByStar(starCount);
        UpdateUI();
    }

    public bool IsStunned() => isStunned;

    public void StunTimePause()    { countdownPaused = true; }
    public void StunTimeContinue() { if (isStunned) countdownPaused = false; }

    public void SetInvincible(bool value) => isInvincible = value;
    public bool IsInvincible() => isInvincible;

    // ======== 內部：星等/暈眩流程 ========
    private void UpdateStarByTotal(int beforeStar)
    {
        // 依 totalStun 推得 starCount
        if (totalStun <= 0) starCount = 0;
        else if (totalStun >= cum3) starCount = 3;
        else if (totalStun >= cum2) starCount = 2;
        else if (totalStun >= cum1) starCount = 1;
        else starCount = 0;

        if (starCount > beforeStar) // 升星
        {
            EnterOrRefreshStunForCurrentStars();
            UpdateStars();
            onStunFull?.Invoke();
        }
        else
        {
            UpdateStars();
        }
    }

    private void EnterOrRefreshStunForCurrentStars()
    {
        isStunned = true;
        countdownPaused = false;
        stunRemaining = GetFullDurationForStars(starCount);

        if (stunBar != null && showBarUI) stunBar.SetActive(true);
        UpdateUI();
    }

    private void RecoverFromStun()
    {
        if (starCount == 3)
        {
            // 第三星結束：全部歸零
            isStunned = false;
            stunRemaining = 0f;
            countdownPaused = false;

            totalStun = 0;
            starCount = 0;

            if (stunBar != null && showBarUI) stunBar.SetActive(false);

            UpdateStars();
            UpdateUI();
            onStunRecovered?.Invoke();
            return;
        }

        // 第1或第2星結束：保留星數，但把 totalStun 夾回該星段起點（條清零）
        isStunned = false;
        stunRemaining = 0f;
        countdownPaused = false;

        totalStun = GetSegmentStartByStar(starCount);

        if (stunBar != null && showBarUI) stunBar.SetActive(false);

        UpdateUI();
        UpdateStars();
        onStunRecovered?.Invoke();
    }

    private float GetFullDurationForStars(int starsCount)
    {
        switch (starsCount)
        {
            case 1: return stunTime1Star;
            case 2: return stunTime2Star;
            case 3: return stunTime3Star;
            default: return 0f;
        }
    }

    // ======== 內部：UI ========
    private void UpdateUI()
    {
        if (!showBarUI || stunBarFill == null)
            return;

        // 以「當前星段的區間進度」顯示；第三星固定滿格
        if (starCount >= 3)
        {
            stunBarFill.localScale = new Vector3(1f, 1f, 1f);
            return;
        }

        int segStart = GetSegmentStartByStar(starCount);
        int segEnd   = GetSegmentEndByStar(starCount);

        float ratio = 0f;
        if (segEnd > segStart)
        {
            ratio = Mathf.Clamp01((totalStun - segStart) / (float)(segEnd - segStart));
        }

        stunBarFill.localScale = new Vector3(ratio, 1f, 1f);
    }

    private void UpdateStars()
    {
        if (!showStarsUI || stars == null || stars.Length == 0) return;

        // ★ 只亮「當前那一顆」，其餘關掉（例如 2星 → 只亮 index=1）
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
                stars[i].SetActive(starCount > 0 && i == (starCount - 1));
        }
    }

    // ======== 內部：工具 ========
    private void ClampTotal()
    {
        totalStun = Mathf.Clamp(totalStun, 0, cum3);
    }

    private int GetNextCumulativeCap(int valueNow)
    {
        if (valueNow < cum1) return cum1;
        if (valueNow < cum2) return cum2;
        if (valueNow < cum3) return cum3;
        return cum3; // 已滿
    }

    private int GetSegmentStartByStar(int star)
    {
        switch (star)
        {
            case 0: return 0;    // 還沒進1星
            case 1: return 0;    // 1星段：0 ~ cum1
            case 2: return cum1; // 2星段：cum1 ~ cum2
            case 3: return cum2; // 3星段：cum2 ~ cum3（顯示固定滿）
            default: return 0;
        }
    }

    private int GetSegmentEndByStar(int star)
    {
        switch (star)
        {
            case 0: return cum1; // 還沒進1星時，以1星門檻作為段尾
            case 1: return cum1;
            case 2: return cum2;
            case 3: return cum3;
            default: return cum3;
        }
    }
}
