using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotPointManager : MonoBehaviour
{
    #region ===== Public Types / Properties =====

    public enum HeatLevel { C, B, A, S }

    public float Heat => heat;
    public HeatLevel Level => currentLevel;
    public bool IsSLocked => Time.time < sLockUntil;

    #endregion

    #region ===== Events (外部可 += 訂閱) =====

    public Action<float, float> OnHeatChanged;
    public Action<HeatLevel, HeatLevel> OnLevelChanged;
    public Action<float> OnSLockStarted;
    public Action OnSLockEnded;
    public Action<int, float> OnComboChanged;

    #endregion

    #region ===== Inspector: Base / Thresholds / S-Lock / Combo / Penalties / UI =====

    [Header("-------- Base --------")]
    [SerializeField] private float HEAT_MIN = 0f;
    [SerializeField] private float HEAT_MAX = 100f;
    [SerializeField] private float HEAT_INIT = 30f;

    [Header("-------- Level Thresholds --------")]
    [Tooltip("C:[0,25), B:[25,50), A:[50,90), S:[90,100]")]
    [SerializeField] private float thresholdB = 25f;
    [SerializeField] private float thresholdA = 50f;
    [SerializeField] private float thresholdS = 90f;

    [Header("-------- S Lock --------")]
    [SerializeField] private float sLockDuration = 30f; // 進 S 後鎖定秒數
    [SerializeField] private float sExitDropTo = 30f;   // S 結束後回落到此數值（預設 30）

    [Header("-------- Combo (Deliver) --------")]
    [SerializeField] private float comboWindowSeconds = 3f;
    [SerializeField] private int   comboMaxStack = 3;
    [SerializeField] private int[] comboPoints = new int[] { 1, 3, 5 }; // stack=1/2/3

    // [Header("-------- Continuous Penalties --------")]
    // [SerializeField] private float riotPerSecond = 1f;      // 客人暴走預設扣分/秒
    // [SerializeField] private float riotDuration = 5f;
    // [SerializeField] private float spreadPerSecond = 1f;    // 擴散預設扣分/秒
    // [SerializeField] private float spreadDuration = 5f;

    [Header("-------- UI (Optional) --------")]
    [SerializeField] private Sprite[] hotLevelSprites; // C/B/A/S -> 0/1/2/3
    [SerializeField] private Image hotPointImage;
    [SerializeField] private Image hotPointFillBar;
    [SerializeField] private Slider hotPointSlider;    // 0~1

    #endregion

    #region ===== Runtime State =====

    private float heat;
    private HeatLevel currentLevel;

    // 送餐連擊
    private int comboStack = 0;            // 0 表示目前無連擊，下一次送餐會進入 stack=1
    private float lastDeliverTime = -999f; // 最後一次送餐時間

    // S 鎖定
    private float sLockUntil = -1f;

    // 持續扣分（多條並行）
    private readonly List<Drain> drains = new List<Drain>();
    private struct Drain { public float until; public float perSec; }

    #endregion

    #region ===== Unity Lifecycle =====

    private void Awake()
    {
        // 初始化
        heat = Mathf.Clamp(HEAT_INIT, HEAT_MIN, HEAT_MAX);
        currentLevel = ComputeLevel(heat);
        UpdateUI();
    }

    private void Update()
    {
        // S 鎖定中：忽略所有正常加減與連擊與持續扣分（只是凍結）
        if (IsSLocked)
        {
            // 顯示 UI（含滑條），但不改變 heat
            UpdateUI();
            // 回報一下 combo 視覺（時間剩餘）
            float windowRemain = Mathf.Max(0f, comboWindowSeconds - (Time.time - lastDeliverTime));
            OnComboChanged?.Invoke(comboStack, windowRemain);
            return;
        }

        // 1) 套用持續扣分
        if (drains.Count > 0)
        {
            float delta = 0f;
            for (int i = drains.Count - 1; i >= 0; i--)
            {
                var d = drains[i];
                if (Time.time >= d.until)
                {
                    drains.RemoveAt(i);
                    continue;
                }
                delta += d.perSec * Time.deltaTime;
            }
            if (Mathf.Abs(delta) > 0.0001f)
            {
                ApplyDelta(-delta); // perSec 是正值，這裡要扣分
            }
        }

        // 2) 連擊視覺剩餘時間回呼
        float windowRemaining = Mathf.Max(0f, comboWindowSeconds - (Time.time - lastDeliverTime));
        if (windowRemaining <= 0f && comboStack > 0)
        {
            comboStack = 0; // 超時重置
        }
        OnComboChanged?.Invoke(comboStack, windowRemaining);

        // 3) UI
        UpdateUI();
    }

    private void LateUpdate()
    {
        // 鎖定到期的時機點在 LateUpdate 檢查，
        // 可避免 Update 當禮物/扣分與解鎖衝突
        if (!IsSLocked && sLockUntil > 0f) // 剛好在這幀到期
        {
            EndSLock();
        }
    }

    #endregion

    #region ===== Public API =====

    public void ResetHeat()
    {
        comboStack = 0;
        drains.Clear();
        sLockUntil = -1f;
        SetHeat(HEAT_INIT);
    }

    public void DeliverDish()
    {
        if (IsSLocked) return;

        // 判定是否仍在連擊窗
        bool inWindow = (Time.time - lastDeliverTime) <= comboWindowSeconds;
        if (!inWindow) comboStack = 0;

        comboStack = Mathf.Min(comboStack + 1, comboMaxStack);
        lastDeliverTime = Time.time;

        int gain = comboPoints[Mathf.Clamp(comboStack - 1, 0, comboPoints.Length - 1)];
        AddHeat(gain);

        // 回報
        float windowRemain = Mathf.Max(0f, comboWindowSeconds - (Time.time - lastDeliverTime));
        OnComboChanged?.Invoke(comboStack, windowRemain);
    }

    public void DefeatEnemy(float amount = 1f)
    {
        if (IsSLocked) return;
        AddHeat(amount);
    }

    public void TakeDamage(float damage, float scale = 1f)
    {
        if (IsSLocked) return;
        ApplyDelta(-Mathf.Abs(damage) * Mathf.Max(0f, scale));
    }

    public void StartRiotPenalty(float perSecond = -1f, float duration = 5f)
    {
        if (perSecond < 0f) perSecond = -perSecond; // 傳負也可
        AddDrain(perSecond, duration);
    }

    public void StartRiotSpreadPenalty(float perSecond = -1f, float duration = 5f)
    {
        if (perSecond < 0f) perSecond = -perSecond;
        AddDrain(perSecond, duration);
    }

    /// 可調試：直接進入 S 並鎖定
    public void ForceEnterSAndLock(float duration = 30f)
    {
        SetHeat(Mathf.Max(thresholdS, HEAT_MIN));
        StartSLock(duration);
    }

    public int GetMoneyMultiplier()
    {
        // C=1, B=2, A=3, S=4
        return ((int)currentLevel) + 1;
    }

    #endregion

    #region ===== Internal: Drain / Heat Apply / State =====

    private void AddDrain(float perSec, float duration)
    {
        if (IsSLocked) return;
        drains.Add(new Drain
        {
            perSec = perSec,
            until = Time.time + duration
        });
    }

    private void AddHeat(float amount)
    {
        if (IsSLocked) return;

        float before = heat;
        heat = Mathf.Clamp(heat + amount, HEAT_MIN, HEAT_MAX);

        // 進 S：立即鎖定
        if (before < thresholdS && heat >= thresholdS)
        {
            StartSLock(sLockDuration);
        }

        AfterHeatChanged(before, heat);
    }

    private void ApplyDelta(float delta)
    {
        if (IsSLocked) return;

        float before = heat;
        heat = Mathf.Clamp(heat + delta, HEAT_MIN, HEAT_MAX);
        AfterHeatChanged(before, heat);
    }

    private void SetHeat(float value)
    {
        float before = heat;
        heat = Mathf.Clamp(value, HEAT_MIN, HEAT_MAX);
        AfterHeatChanged(before, heat);
    }

    private void AfterHeatChanged(float before, float after)
    {
        if (Mathf.Approximately(before, after) == false)
        {
            OnHeatChanged?.Invoke(before, after);
        }

        var newLevel = ComputeLevel(after);
        if (newLevel != currentLevel)
        {
            var old = currentLevel;
            currentLevel = newLevel;
            OnLevelChanged?.Invoke(old, newLevel);
            HookLevelVFX(newLevel);
        }

        UpdateUI();
    }

    #endregion

    #region ===== Internal: Compute / UI / VFX =====

    private HeatLevel ComputeLevel(float value)
    {
        if (value >= thresholdS) return HeatLevel.S;
        if (value >= thresholdA) return HeatLevel.A;
        if (value >= thresholdB) return HeatLevel.B;
        return HeatLevel.C;
    }

    private void UpdateUI()
    {
        // Icon / Fill
        int idx = (int)currentLevel; // C=0, B=1, A=2, S=3
        if (hotLevelSprites != null && hotLevelSprites.Length > idx)
        {
            if (hotPointImage)    hotPointImage.sprite = hotLevelSprites[idx];
            if (hotPointFillBar)  hotPointFillBar.sprite = hotLevelSprites[idx];
        }
        // Slider：0~1 = 低→高
        if (hotPointSlider)
        {
            hotPointSlider.value = Mathf.InverseLerp(HEAT_MIN, HEAT_MAX, heat);
        }
    }

    private void HookLevelVFX(HeatLevel level)
    {
        // TODO: 依你現有的 RoundManager / GlobalLightManager 來掛燈效/音效
        // 例如：
        // var gl = RoundManager.Instance.globalLightManager;
        // switch (level)
        // {
        //     case HeatLevel.A:
        //         // A：球光開啟
        //         gl.SetLightGroupActive(0, true);
        //         gl.SetLightCycleLoopEnabled(false);
        //         break;
        //     case HeatLevel.S:
        //         // S：聚光燈 + 球光
        //         gl.SetLightGroupActive(0, true);
        //         gl.SetLightGroupActive(1, true);
        //         gl.SetLightCycleLoopEnabled(true);
        //         break;
        //     default:
        //         // C/B：關閉特效
        //         gl.SetLightGroupActive(0, false);
        //         gl.SetLightGroupActive(1, false);
        //         gl.SetLightCycleLoopEnabled(false);
        //         break;
        // }
    }

    private void StartSLock(float duration)
    {
        sLockUntil = Time.time + duration;
        OnSLockStarted?.Invoke(duration);
        // 這裡可立即切 S 特效
        HookLevelVFX(HeatLevel.S);
    }

    private void EndSLock()
    {
        sLockUntil = -1f;
        // 規格：S 結束後數值直接回落到 30
        SetHeat(sExitDropTo);
        comboStack = 0;
        OnSLockEnded?.Invoke();
    }

    #endregion
}
