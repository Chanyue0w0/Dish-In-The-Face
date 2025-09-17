using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TroubleGuestController : MonoBehaviour
{
    [Header("----- Status -----")]
    [SerializeField] private int atk = 1;
    [SerializeField] private float moveSpeed = 2f;

    [Header("----- Attack Setting -----")]
    [SerializeField] private float attackCooldown = 2f; // 冷卻(秒)
    [SerializeField] private float chargeTime = 1f;     // 蓄力時間(秒)
    [SerializeField] private LayerMask playerLayer;     // 玩家圖層
    [SerializeField] private Transform attackOrigin;    // 供 attackRangeBox 參考的原點
    [SerializeField] private string attackTriggerTag = "AttackStun";

    [Header("-------- Appearance --------")]
    [SerializeField] private List<GameObject> guestAppearanceList = new List<GameObject>();

    [Header("----- Reference -----")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer guestSpriteRenderer;
    [SerializeField] private GameObject attackHitBox;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject attackRangeBox;
    [SerializeField] private GameObject dieVFX;
    [SerializeField] private GameObject attackVFX;
    [SerializeField] private BeGrabByPlayer beGrabByPlayer;

    [Header("----- Reference (Select) -----")]
    [SerializeField] private StunController stun;
    [SerializeField] private EnemyHpController hpController;
    
    private Transform player;
    private float lastAttackTime;
    private bool isCharging;
    private float chargeStartTime;

    private GuestPoolHandler poolHandler;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.speed = moveSpeed;
        }

        poolHandler = GetComponent<GuestPoolHandler>();

        if (attackOrigin == null && attackRangeBox != null)
            attackOrigin = attackRangeBox.transform;
    }

    private void Start()
    {
        if (RoundManager.Instance) player = RoundManager.Instance.player;
    }

    private void OnEnable()
    {
        SetAppearance();
        
        if (RoundManager.Instance) player = RoundManager.Instance.player;

        // if (stun != null) stun.FullReset(); // 啟用時重置暈眩條與星星
        if (attackHitBox != null) attackHitBox.SetActive(false);
        if (attackRangeBox != null) attackRangeBox.SetActive(false);

        lastAttackTime = -Mathf.Infinity;
        isCharging = false;

        TryEnsureOnNavMesh();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(EndAttack));
        isCharging = false;
    }

    private void Update()
    {
        if (player == null) return;

        // 暈眩中：停下
        if (stun != null && stun.IsStunned())
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            return;
        }

        // 左右翻面（依 NavMesh 速度）
        if (agent != null && agent.enabled)
        {
            if (agent.velocity.x < -0.01f)
                transform.rotation = Quaternion.Euler(0, 0, 0);
            else if (agent.velocity.x > 0.01f)
                transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        // 蓄力
        if (isCharging)
        {
            if (Time.time - chargeStartTime >= chargeTime)
            {
                PerformAttack();
                isCharging = false;
            }
            else
            {
                if (agent != null && agent.enabled) agent.isStopped = true;
                return;
            }
        }

        // 追玩家
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        // 進入攻擊範圍開始蓄力
        if (Time.time - lastAttackTime >= attackCooldown && IsPlayerInRange())
        {
            StartCharge();
        }
    }

    private float GetAttackRadius()
    {
        if (attackRangeBox != null)
        {
            var t = attackRangeBox.transform;

            var circle = attackRangeBox.GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                float scale = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
                return circle.radius * scale;
            }

            var sr = attackRangeBox.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                var ext = sr.bounds.extents;
                return (ext.x + ext.y) * 0.5f;
            }
        }
        return 2f;
    }

    private bool IsPlayerInRange()
    {
        if (attackRangeBox != null && attackOrigin == null)
            attackOrigin = attackRangeBox.transform;

        if (attackOrigin == null) attackOrigin = transform;

        float radius = GetAttackRadius();
        Collider2D hit = Physics2D.OverlapCircle(attackOrigin.position, radius, playerLayer);
        return hit != null;
    }

    private void StartCharge()
    {
        if (attackRangeBox != null) attackRangeBox.SetActive(true);
        isCharging = true;
        chargeStartTime = Time.time;
        lastAttackTime = Time.time;
        if (agent != null && agent.enabled) agent.isStopped = true;
    }

    private void PerformAttack()
    {
        if (agent != null && agent.enabled) agent.isStopped = true;

        if (attackHitBox != null) attackHitBox.SetActive(true);
        var fxPos = (attackOrigin != null ? attackOrigin.position : transform.position);
        VFXPool.Instance.SpawnVFX("Attack", fxPos, Quaternion.identity, 1f);
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.enemyAttack, transform.position);

        float radius = GetAttackRadius();
        if (attackOrigin == null) attackOrigin = transform;

        var hit = Physics2D.OverlapCircle(attackOrigin.position, radius, playerLayer);
        if (hit != null && hit.CompareTag("Player"))
        {
            var playerStatus = hit.GetComponent<PlayerStatus>();
            if (playerStatus != null)
                playerStatus.TakeDamage(atk);
        }

        Invoke(nameof(EndAttack), 0.2f);
    }

    private void EndAttack()
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        if (agent == null || !agent.isActiveAndEnabled || !agent.enabled) return;

        if (!agent.isOnNavMesh && !TryEnsureOnNavMesh())
            return;

        if (attackHitBox != null) attackHitBox.SetActive(false);
        if (attackRangeBox != null) attackRangeBox.SetActive(false);

        agent.isStopped = false;
    }

    private bool TryEnsureOnNavMesh(float searchRadius = 2f)
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
        {
            return agent.Warp(hit.position);
        }
        return false;
    }

    public void SetAppearance(GameObject appearance = null)
    {
        var components = GetComponentsInChildren<GuestAnimationController>(true); // true = 包含未啟用物件
        foreach (var comp in components)
        {
            DestroyImmediate(comp.gameObject); // 編輯器下立刻刪除
        }
        
        if (appearance != null)
        {
            Instantiate(appearance, transform.position, transform.rotation, transform);
            return;
        }

        // 隨機外觀
        if (guestAppearanceList != null && guestAppearanceList.Count > 0)
        {
            int idx = Random.Range(0, guestAppearanceList.Count);
            Instantiate(guestAppearanceList[idx], transform.position, transform.rotation, transform);
            return;
        }

        Debug.LogWarning("Not Get Enemy Guest Sprite!!!!");
    }

    private IEnumerator ApplyKnockback(Vector2 direction, float knockbackForce = 10f, float knockbackDuration = 0.3f)
    {
        if (agent == null) yield break;

        // 暫停 NavMesh 控制，直接位移 Transform
        bool reEnableAfter = false;
        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
            reEnableAfter = true;
        }

        // 受擊動畫觸發已移到 EnemyHPController.TakeDamage()

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            transform.position += (Vector3)(direction * (knockbackForce * Time.deltaTime));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 恢復 NavMesh
        if (reEnableAfter && agent != null)
        {
            agent.enabled = true;
            TryEnsureOnNavMesh();
            agent.isStopped = false;
        }
        
        if (hpController) hpController.TakeDamage(1);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 被攻擊
        if (other.CompareTag(attackTriggerTag))
        {
            VFXPool.Instance.SpawnVFX("BasicAttack", transform.position, Quaternion.identity, 1f);
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.pieAttack, transform.position);
            // 擊退
            Vector2 knockDir = (transform.position - other.transform.position).normalized;
            StartCoroutine(ApplyKnockback(knockDir));
        }
        
    }

    // public void BeForceOut()
    // {
        // // 取得原本的移動方向
        // Vector2 moveDir = rb != null ? rb.velocity.normalized : Vector2.zero;
        // if (moveDir == Vector2.zero && agent != null)
        // {
        //     moveDir = agent.velocity.normalized;
        // }
        // if (moveDir == Vector2.zero)
        // {
        //     moveDir = Vector2.right; // 預設一個方向
        // }
        //
        // float forwardDistance = 10f;
        // Vector3 targetPos = transform.position + (Vector3)(moveDir * forwardDistance);

        // Tween.Position(transform, targetPos, 0.5f)
        //     .OnComplete(() => Dead(false));
    // }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (attackRangeBox == null && attackOrigin == null) return;
        if (attackOrigin == null) attackOrigin = (attackRangeBox != null ? attackRangeBox.transform : transform);

        float r = GetAttackRadius();
        Gizmos.DrawWireSphere(attackOrigin.position, r);
    }
#endif

    // =========================
    // ===== 暈眩事件（給 StunController 的 UnityEvent 綁定）=====
    // =========================

    public void OnStunFull()
    {
        isCharging = false;

        if (agent != null && agent.enabled)
            agent.isStopped = true;

        if (beGrabByPlayer != null)
            beGrabByPlayer.SetIsCanBeGrabByPlayer(true);
    }

    public void OnStunRecovered()
    {
        if (beGrabByPlayer != null)
        {
            if (beGrabByPlayer.GetIsCanBeGrabbing())
                beGrabByPlayer.SetIsOnBeGrabbing(false);

            beGrabByPlayer.SetIsCanBeGrabByPlayer(false);
        }

        if (agent != null)
        {
            if (!agent.enabled)
            {
                agent.enabled = true;
                TryEnsureOnNavMesh();
            }
            agent.isStopped = false;
        }

        transform.SetParent(RoundManager.Instance.guestGroupManager.transform);
    }

    // =========================
    // ===== 被抓/放下 事件 =====
    // =========================
    public void OnGrabbedByPlayer()
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
    }

    public void OnReleasedByPlayer()
    {
        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
            TryEnsureOnNavMesh();
        }
    }

    // =========================
    // ===== HP 歸零處理 =====
    // =========================

    /// <summary>提供給 EnemyHPController 的 onHpZero 綁定。</summary>

    public void Dead(bool defeated)
    {
        if (defeated)
        {
            var pos = (attackOrigin != null ? attackOrigin.position : transform.position);
            VFXPool.Instance.SpawnVFX("CoinFountain", pos, Quaternion.identity, 2f);
        }

        RoundManager.Instance.DefeatEnemySuccess();
        if (poolHandler != null) poolHandler.Release();
        else gameObject.SetActive(false);
    }
    
    public bool IsMoving() {
        return agent != null && agent.enabled && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.01f;
    }

    public bool IsAttacking() {
        return isCharging || (attackHitBox != null && attackHitBox.activeSelf);
    }

}