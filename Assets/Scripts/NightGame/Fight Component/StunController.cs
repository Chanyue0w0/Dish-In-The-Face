using UnityEngine;
using UnityEngine.Events;

public class StunController : MonoBehaviour
{
    [Header("----- Stun Setting -----")]
    [SerializeField] private int maxStunPerStage = 6;        // 每階段需要的暈眩值
    [SerializeField] private float noDamageResetTime = 10f;  // 幾秒沒受攻擊就清空暈眩值 & 星星（僅在非暈眩時檢查）
    [SerializeField] private string stunTriggerTag = "AttackStun";
    [SerializeField] private bool isInvincible = false; // 無敵時不會累積暈眩

    [Header("Star Stun Time")]
    [SerializeField] private float stunTime1Star = 3f;
    [SerializeField] private float stunTime2Star = 7f;
    [SerializeField] private float stunTime3Star = 12f;
    
    [Header("UI Setting")]
    [SerializeField] private bool showBarUI = true;   // 是否開啟暈眩條 UI（顯示累積值）
    [SerializeField] private bool showStarsUI = true; // 是否開啟星星數量顯示

    [Header("----- Reference -----")]
    [SerializeField] private GameObject[] stars;      // 星星圖示陣列，依序 1,2,3 星
    [SerializeField] private GameObject stunBar;      // 暈眩條物件
    [SerializeField] private Transform stunBarFill;   // 暈眩條填充（localScale.x 0~1）
    
    [Header("----- Events -----")]
    public UnityEvent onStunFull;       // 進入/更新暈眩（升星）時呼叫
    public UnityEvent onStunRecovered;  // 暈眩倒數結束時呼叫

    // ===== Runtime 狀態 =====
    private int currentStun;         // 目前累積值（顯示於條）
    private int starCount;           // 目前星數（0~3）
    private bool isStunned;          // 是否暈眩中
    private float stunRemaining;     // 暈眩剩餘秒數（內部倒數）
    private bool countdownPaused;    // 是否暫停倒數
    private float lastIncreaseTime;  // 最近一次 AddStun() 的時間（用於 10 秒規則）

    private void Awake()
    {
        if (stunBar != null) stunBar.SetActive(showBarUI);
        currentStun = 0;
        starCount = 0;
        isStunned = false;
        stunRemaining = 0f;
        countdownPaused = false;
        lastIncreaseTime = -999f;

        UpdateUI();     // 條歸 0
        UpdateStars();  // 全關
    }

    private void OnEnable()
    {
        UpdateUI();
        UpdateStars();
    }

    private void Update()
    {
        // ===（規則 #3）非暈眩中：若超過 noDamageResetTime 沒有增加暈眩值 → 星星與暈眩值清 0 ===
        if (!isStunned && Time.time - lastIncreaseTime >= noDamageResetTime)
        {
            if (currentStun > 0 || starCount > 0)
            {
                currentStun = 0;
                starCount = 0;
                UpdateStars();
                UpdateUI();
                if (stunBar != null && showBarUI) stunBar.SetActive(false);
            }
        }

        // === 暈眩中倒數 ===
        if (isStunned && !countdownPaused && stunRemaining > 0f)
        {
            stunRemaining -= Time.deltaTime;
            if (stunRemaining <= 0f)
            {
                RecoverFromStun(); // 結束暈眩
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isInvincible) return; // 無敵時直接忽略
        if (other.CompareTag(stunTriggerTag))
        {
            AddStun(1);
        }
    }

    /// <summary> 增加暈眩值（顯示為「距離下一次升星」的累積進度；第三顆星例外） </summary>
    public void AddStun(int amount)
    {
        if (isInvincible) return; // 無敵時不累積
        if (amount <= 0) return;

        lastIncreaseTime = Time.time;

        if (showBarUI && stunBar != null)
            stunBar.SetActive(true);

        // 在任何狀態下都累積條
        currentStun += amount;

        // 檢查是否升星（一次可能跨越多段）
        bool gainedStar = false;
        while (currentStun >= maxStunPerStage && starCount < 3)
        {
            // 👉 這次升星之後 starCount 將變成 newStarCount
            int newStarCount = starCount + 1;

            if (newStarCount == 3)
            {
                // 規則（第三顆星例外）：不把暈眩值歸零，僅夾到滿格上限，維持滿格顯示
                starCount = 3;
                currentStun = Mathf.Min(currentStun, maxStunPerStage);
            }
            else
            {
                // 第一、第二顆星 → 以條作為「下一次升星」累積，因此扣掉一段容量
                starCount = newStarCount;
                currentStun -= maxStunPerStage; // 條歸零（或保留餘量）
            }

            EnterOrRefreshStunForCurrentStars();
            gainedStar = true;
        }

        if (gainedStar)
        {
            UpdateStars();
            onStunFull?.Invoke();
        }

        UpdateUI();
    }

    /// <summary> 手動完全重置（外部重置用）。</summary>
    public void FullReset()
    {
        isStunned = false;
        stunRemaining = 0f;
        countdownPaused = false;

        currentStun = 0;
        starCount = 0;

        if (stunBar != null && showBarUI) stunBar.SetActive(false);

        UpdateStars();
        UpdateUI();
    }

    /// <summary> 只清除條的累積值，不動星星與暈眩狀態。 </summary>
    public void ResetStunProgress()
    {
        currentStun = 0;
        UpdateUI();
    }

    public bool IsStunned() => isStunned;

    /// <summary>
    /// 依目前星數進入或更新暈眩狀態：
    /// - 設為暈眩中
    /// - 將剩餘時間重置為該星級完整秒數
    /// - 條維持顯示「下一次升星」累積（第三顆星則保持滿格）
    /// </summary>
    private void EnterOrRefreshStunForCurrentStars()
    {
        isStunned = true;
        countdownPaused = false;

        stunRemaining = GetFullDurationForStars(starCount);

        if (stunBar != null && showBarUI)
            stunBar.SetActive(true);

        UpdateUI();
    }

    private void RecoverFromStun()
    {
        // 若是第三顆星結束 → 規則：暈眩值與星數都歸零
        if (starCount == 3)
        {
            isStunned = false;
            stunRemaining = 0f;
            countdownPaused = false;

            currentStun = 0;
            starCount = 0;

            if (stunBar != null && showBarUI) stunBar.SetActive(false);

            UpdateStars();
            UpdateUI();
            onStunRecovered?.Invoke();
            return;
        }

        // 否則（第 1 或第 2 顆星結束）：保留星數、清條（維持你之前的規則 #2）
        isStunned = false;
        stunRemaining = 0f;
        countdownPaused = false;

        currentStun = 0;

        if (stunBar != null && showBarUI) stunBar.SetActive(false);

        UpdateUI();
        UpdateStars(); // 視需求可保留或移除；這裡保留同步 UI
        onStunRecovered?.Invoke();
    }

    private float GetFullDurationForStars(int starsCount)
    {
        switch (starsCount)
        {
            case 1: return stunTime1Star;
            case 2: return stunTime2Star;
            case 3: return stunTime3Star;
            default: return stunTime1Star; // fallback
        }
    }

    private void UpdateUI()
    {
        if (!showBarUI || stunBarFill == null) return;

        // 條顯示「下一次升星」累積比例；第三顆星例外會被夾成滿格
        float ratio = (maxStunPerStage > 0)
            ? Mathf.Clamp01((float)currentStun / maxStunPerStage)
            : 0f;

        stunBarFill.localScale = new Vector3(ratio, 1f, 1f);
    }

    private void UpdateStars()
    {
        if (!showStarsUI || stars == null || stars.Length == 0) return;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
                stars[i].SetActive(i+1 == starCount);
        }
    }

    // 可供外部暫停/繼續暈眩倒數（例如被抓起來時暫停）
    public void StunTimePause() { countdownPaused = true; }
    public void StunTimeContinue() { if (isStunned) countdownPaused = false; }
    /// <summary>設定是否無敵（無敵時不會受到暈眩攻擊）。</summary>
    public void SetInvincible(bool value) => isInvincible = value;

    /// <summary>目前是否為無敵狀態。</summary>
    public bool IsInvincible() => isInvincible;

}
