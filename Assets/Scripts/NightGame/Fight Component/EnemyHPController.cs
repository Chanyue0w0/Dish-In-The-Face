using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class EnemyHpController : MonoBehaviour
{
    private static readonly int BeAttack = Animator.StringToHash("BeAttack");

    [SerializeField] private bool openTriggerDetect;
    [SerializeField] private string attackTriggerTag = "AttackStun";
    [Header("----- HP -----")]
    [SerializeField] private int maxHp = 3;
    [SerializeField] private int currentHp;

    [Header("----- UI -----")]
    [SerializeField] private GameObject hpBar;   // 整個血條物件（顯示/隱藏）
    [SerializeField] private Transform barFill;  // 填充用 Transform，使用 localScale.x 代表比例

    [Header("----- Animation -----")]
    [SerializeField] private Animator animator;
    [Tooltip("受傷時是否自動播放受擊動畫（Trigger：BeAttack）")]
    [SerializeField] private bool playHitAnimOnDamage = true;

    [Header("----- Events -----")]
    [Tooltip("當 HP 歸零時呼叫（可在 Inspector 綁定 TroubleGustController 的函式）")]
    public UnityEvent onHpZero;


    private void OnEnable()
    {
        InitRandomHp(1, 4, hideBar: true);
    }
    /// <summary>初始化 HP，並可選擇是否隱藏血條。</summary>
    public void InitHp(int max, bool hideBar = true)
    {
        maxHp = Mathf.Max(1, max);
        currentHp = maxHp;
        if (hideBar && hpBar != null) hpBar.SetActive(false);
        UpdateBarUI();
    }

    /// <summary>以亂數設定 HP（區間 [minInclusive, maxExclusive)）。</summary>
    private void InitRandomHp(int minInclusive, int maxExclusive, bool hideBar = true)
    {
        int max = Mathf.Clamp(Random.Range(minInclusive, maxExclusive), 1, 9999);
        InitHp(max, hideBar);
    }

    public int GetCurrentHp() => currentHp;
    public int GetMaxHp() => maxHp;

    /// <summary>造成傷害；自動顯示血條、更新 UI、播放受擊動畫；若歸零觸發 onHpZero。</summary>
    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        if (hpBar != null) hpBar.SetActive(true);

        currentHp = Mathf.Max(0, currentHp - damage);
        UpdateBarUI();

        if (playHitAnimOnDamage && animator != null)
            animator.SetTrigger(BeAttack);

        if (currentHp <= 0)
        {
            // 交給外部（例如 TroubleGustController）處理死亡流程
            onHpZero?.Invoke();
        }
    }

    public void Heal(int value)
    {
        if (value <= 0 || currentHp <= 0) return;
        currentHp = Mathf.Min(maxHp, currentHp + value);
        UpdateBarUI();
    }

    public void SetHp(int hp, int? newMax = null, bool hideBarWhenFull = false)
    {
        if (newMax.HasValue) maxHp = Mathf.Max(1, newMax.Value);
        currentHp = Mathf.Clamp(hp, 0, maxHp);

        if (hideBarWhenFull && hpBar != null)
            hpBar.SetActive(currentHp < maxHp);

        UpdateBarUI();

        if (currentHp <= 0)
            onHpZero?.Invoke();
    }

    private void UpdateBarUI()
    {
        if (barFill == null) return;
        float ratio = maxHp > 0 ? (float)currentHp / maxHp : 0f;
        barFill.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!openTriggerDetect) return;
        
        // 原本的擊退（沿用）
        if (other.CompareTag(attackTriggerTag))
        {
            TakeDamage(1);
        }

    }
    // 方便從外部測試（例如 AnimationEvent 或 Debug）
    public void Debug_Deal1Damage() => TakeDamage(1);
}
