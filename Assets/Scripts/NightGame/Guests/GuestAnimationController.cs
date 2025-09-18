using UnityEngine;

public class GuestAnimationController : MonoBehaviour
{
    // === Triggers ===
    private static readonly int TrigIsAttacking   = Animator.StringToHash("isAttacking"); // 改為 Trigger
    private static readonly int TrigBeAttacked    = Animator.StringToHash("BeAttacked");  // 新增 Trigger

    // === Bools ===
    private static readonly int IsMoving              = Animator.StringToHash("isMoving");
    private static readonly int IsLeft                = Animator.StringToHash("isLeft");
    private static readonly int IsWaitingOrThinking   = Animator.StringToHash("isWaitingOrThinking");
    private static readonly int IsOrderingOrEating    = Animator.StringToHash("isOrderingOrEating");

    [SerializeField] private Animator animator;

    [Header("Layer Names")]
    [SerializeField] private string baseLayerName = "Base Layer";
    [SerializeField] private string attackLayerName = "Attack Layer";

    private NormalGuestController _normal;
    private TroubleGuestController _trouble;
    private WanderGuestController _wander;

    private int _baseLayerIndex = -1;
    private int _attackLayerIndex = -1;

    // 邊緣偵測用：只在 false -> true 時送 Trigger
    private bool _prevAttacking;
    private bool _prevBeAttacked;

    private void Awake() {
        _normal  = GetComponentInParent<NormalGuestController>();
        _trouble = GetComponentInParent<TroubleGuestController>();
        _wander  = GetComponentInParent<WanderGuestController>();

        if (animator != null) {
            _baseLayerIndex  = SafeGetLayerIndex(animator, baseLayerName);
            _attackLayerIndex = SafeGetLayerIndex(animator, attackLayerName);
        }

        OnEnable(); // 保持原本行為
    }

    private void OnEnable() {
        // 啟用時切到正確 Layer 並重置前一幀狀態
        if (_normal != null || _wander != null)
            SetLayerWeights(baseOn: true);
        else if (_trouble != null)
            SetLayerWeights(baseOn: false);

        _prevAttacking  = false;
        _prevBeAttacked = false;

        if (animator) {
            // 確保觸發器是乾淨狀態
            animator.ResetTrigger(TrigIsAttacking);
            animator.ResetTrigger(TrigBeAttacked);
        }
    }

    private void Update() {
        if (!animator) return;

        bool isMoving = false;
        bool isWaitingOrThinking = false;
        bool isOrderingOrEating = false;
        bool isLeft = false;

        if (_normal != null) {
            isMoving = _normal.IsMoving();
            isWaitingOrThinking = _normal.IsThinking();
            isOrderingOrEating = _normal.IsOrdering() || _normal.IsWaitingDish() || _normal.IsEating();
            SetLayerWeights(baseOn: true);
        }
        else if (_trouble != null) {
            // ---- Trigger：Attacking ----
            bool attackingNow = _trouble.IsAttacking(); // 由 TroubleGuest 提供
            if (attackingNow && !_prevAttacking) {
                animator.SetTrigger(TrigIsAttacking);
            }
            _prevAttacking = attackingNow;

            // ---- Trigger：BeAttacked ----
            bool beAttackedNow = _trouble.IsBeAttacked(); // 由 TroubleGuest 提供
            if (beAttackedNow && !_prevBeAttacked) {
                animator.SetTrigger(TrigBeAttacked);
            }
            _prevBeAttacked = beAttackedNow;

            isMoving = _trouble.IsMoving();
            isLeft   = _trouble.IsLeft();
            SetLayerWeights(baseOn: false);
        }
        else if (_wander != null) {
            isMoving = _wander.IsMoving();
            SetLayerWeights(baseOn: true);
        }

        // 維持其它狀態為 Bool
        animator.SetBool(IsMoving, isMoving);
        animator.SetBool(IsWaitingOrThinking, isWaitingOrThinking);
        animator.SetBool(IsOrderingOrEating, isOrderingOrEating);
        animator.SetBool(IsLeft, isLeft);
    }

    /// <summary>切換 Animator Layer 權重</summary>
    private void SetLayerWeights(bool baseOn) {
        if (!animator) return;

        if (_baseLayerIndex >= 0)
            animator.SetLayerWeight(_baseLayerIndex, baseOn ? 1f : 0f);
        if (_attackLayerIndex >= 0)
            animator.SetLayerWeight(_attackLayerIndex, baseOn ? 0f : 1f);
    }

    private static int SafeGetLayerIndex(Animator ani, string layerName) {
        if (!ani || string.IsNullOrEmpty(layerName)) return -1;
        return ani.GetLayerIndex(layerName);
    }
}
