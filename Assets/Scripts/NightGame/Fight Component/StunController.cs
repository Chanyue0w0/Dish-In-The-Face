using UnityEngine;
using UnityEngine.Events;

public class StunController : MonoBehaviour
{
    [Header("----- Stun Setting -----")]
    [SerializeField] private int maxStunPerStage = 6;        // 每階段需要的暈眩值
    [SerializeField] private float noDamageResetTime = 10f;  // 幾秒沒受攻擊就清空暈眩值

    [Header("Star Stun Time ")]
    [SerializeField] private float stunTime1Star = 3f;
    [SerializeField] private float stunTime2Star = 7f;
    [SerializeField] private float stunTime3Star = 12f;

    [Header("UI Setting ")]
    [SerializeField] private bool showBarUI = true;   // 是否開啟暈眩條 UI
    [SerializeField] private bool showStarsUI = true; // 是否開啟星星數量顯示

    // [Header("----- Reference (可選) -----")]
    // [SerializeField] private BeGrabByPlayer beGrabByPlayer;
    [Header("----- Reference -----")]
    [SerializeField] private GameObject[] stars;      // 星星圖示陣列，依序 1,2,3 星
    [SerializeField] private GameObject stunBar;      // 暈眩條物件
    [SerializeField] private Transform stunBarFill;   // 暈眩條填充


    [Header("----- Events -----")]
    public UnityEvent onStunFull;
    public UnityEvent onStunRecovered;

    // ===== Runtime 狀態 =====
    private int currentStun;
    private int starCount;
    private bool isStunned;
    private float stunRemaining;
    private bool countdownPaused;
    private float lastDamageTime = -999f;

    private void Awake()
    {
        if (stunBar != null) stunBar.SetActive(showBarUI);
        UpdateUI();
        UpdateStars();
    }

    private void OnEnable()
    {
        currentStun = 0;        // 當前暈眩值
        starCount = 0;          // 暈眩星數
        isStunned = false;     // 是否暈眩中
        stunRemaining = 0f;   // 暈眩剩餘時間
        countdownPaused = false;
        lastDamageTime = -999f;
    }

    // private void OnDisable()
    // {
        // if (beGrabByPlayer != null)
        // {
        //     beGrabByPlayer.UnregisterOnBeGrabbingAction(true, OnGrabbed);
        //     beGrabByPlayer.UnregisterOnBeGrabbingAction(false, OnReleased);
        // }
    // }

    private void Update()
    {
        // === 沒有暈眩中 → 檢查「多久沒受攻擊就清空暈眩值」 ===
        if (!isStunned && Time.time - lastDamageTime >= noDamageResetTime && currentStun > 0)
        {
            currentStun = 0;
            UpdateUI();
            if (stunBar != null && showBarUI) stunBar.SetActive(false);
        }

        // === 暈眩中倒數 ===
        if (isStunned && !countdownPaused && stunRemaining > 0f)
        {
            stunRemaining -= Time.deltaTime;
            if (stunRemaining <= 0f)
            {
                RecoverFromStun();
            }
            else
            {
                UpdateUI();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BasicAttack"))
        {
            AddStun(1);
        }
    }

    /// <summary> 增加暈眩值 </summary>
    public void AddStun(int amount)
    {
        if (amount <= 0) return;

        lastDamageTime = Time.time;
        if (stunBar != null && showBarUI) stunBar.SetActive(true);

        if (isStunned)
        {
            UpdateUI();
            return;
        }

        currentStun += amount;

        // === 檢查是否升星 ===
        while (currentStun >= maxStunPerStage && starCount < 3)
        {
            currentStun -= maxStunPerStage;
            starCount++;
            UpdateStars();
            EnterStun();
        }

        UpdateUI();
    }

    public void ResetStun()
    {
        isStunned = false;
        currentStun = 0;
        stunRemaining = 0f;
        countdownPaused = false;

        if (stunBar != null && showBarUI) stunBar.SetActive(false);
        UpdateUI();
    }

    public bool IsStunned() => isStunned;

    private void EnterStun()
    {
        isStunned = true;
        countdownPaused = false;

        switch (starCount)
        {
            case 1: stunRemaining = stunTime1Star; break;
            case 2: stunRemaining = stunTime2Star; break;
            case 3: stunRemaining = stunTime3Star; break;
            default: stunRemaining = stunTime1Star; break;
        }

        if (stunBar != null && showBarUI) stunBar.SetActive(true);
        UpdateUI();

        onStunFull?.Invoke();
    }

    private void RecoverFromStun()
    {
        isStunned = false;
        stunRemaining = 0f;
        countdownPaused = false;
        currentStun = 0;

        if (stunBar != null && showBarUI) stunBar.SetActive(false);
        UpdateUI();

        onStunRecovered?.Invoke();
    }

    private void UpdateUI()
    {
        if (!showBarUI || stunBarFill == null) return;

        float ratio = 0f;
        if (isStunned)
        {
            float total = (starCount == 1) ? stunTime1Star :
                          (starCount == 2) ? stunTime2Star :
                          (starCount == 3) ? stunTime3Star : 1f;
            ratio = Mathf.Clamp01(stunRemaining / total);
        }
        else
        {
            ratio = (maxStunPerStage > 0) ? Mathf.Clamp01((float)currentStun / maxStunPerStage) : 0f;
        }

        stunBarFill.localScale = new Vector3(ratio, 1f, 1f);
    }

    private void UpdateStars()
    {
        if (!showStarsUI || stars == null || stars.Length == 0) return;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
                stars[i].SetActive(i < starCount);
        }
    }

    private void StunTimePause() { countdownPaused = true; }
    private void StunTimeContinue() { if (isStunned) countdownPaused = false; }
    
    // private 中斷 stun
}
