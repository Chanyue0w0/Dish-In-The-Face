using UnityEngine;

public class GuestAnimationController : MonoBehaviour
{
    private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private static readonly int IsWaitingOrThinking = Animator.StringToHash("isWaitingOrThinking");
    private static readonly int IsOrderingOrEating = Animator.StringToHash("isOrderingOrEating");
    [SerializeField] private Animator animator;

    [Header("Layer Names")]
    [SerializeField] private string baseLayerName = "Base Layer";
    [SerializeField] private string attackLayerName = "Attack Layer";

    private NormalGuestController _normal;
    private TroubleGuestController _trouble;
    private WanderGuestController _wander;

    private int _baseLayerIndex = -1;
    private int _attackLayerIndex = -1;

    private void Awake() {
        _normal = GetComponentInParent<NormalGuestController>();
        _trouble = GetComponentInParent<TroubleGuestController>();
        _wander = GetComponentInParent<WanderGuestController>();

        if (animator != null) {
            _baseLayerIndex = SafeGetLayerIndex(animator, baseLayerName);
            _attackLayerIndex = SafeGetLayerIndex(animator, attackLayerName);
        }

        OnEnable();
    }

    private void OnEnable() {
        // 一啟用就切換到正確的 Layer
        if (_normal != null || _wander != null)
            SetLayerWeights(baseOn: true);
        else if (_trouble != null)
            SetLayerWeights(baseOn: false);
    }

    private void Update() {
        bool isAttacking = false;
        bool isMoving = false;
        bool isWaitingOrThinking = false;
        bool isOrderingOrEating = false;

        if (_normal != null) {
            isMoving = _normal.IsMoving();
            isWaitingOrThinking = _normal.IsThinking();
            isOrderingOrEating = _normal.IsOrdering() || _normal.IsWaitingDish() || _normal.IsEating();
            SetLayerWeights(baseOn: true);
        } 
        else if (_trouble != null) {
            isAttacking = _trouble.IsAttacking();
            isMoving = _trouble.IsMoving();
            SetLayerWeights(baseOn: false);
        } 
        else if (_wander != null) {
            isMoving = _wander.IsMoving();
            SetLayerWeights(baseOn: true);
        }

        animator.SetBool(IsAttacking, isAttacking);
        animator.SetBool(IsMoving, isMoving);
        animator.SetBool(IsWaitingOrThinking, isWaitingOrThinking);
        animator.SetBool(IsOrderingOrEating, isOrderingOrEating);
    }

    /// <summary>
    /// 切換 Animator Layer 權重
    /// </summary>
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
