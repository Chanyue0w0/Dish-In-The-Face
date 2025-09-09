using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.AI;

public class TroubleGusetController : MonoBehaviour
{
    private static readonly int BeAttack = Animator.StringToHash("BeAttack");

    [Header("----- Status -----")]
    [SerializeField] private int currentHp;
    [SerializeField] private int maxHp = 3;
    [SerializeField] private int atk = 1;
    [SerializeField] private float moveSpeed = 2f;

    [Header("----- Attack Setting -----")]
    [SerializeField] private float attackCooldown = 2f; // 冷卻(秒)
    [SerializeField] private float chargeTime = 1f;     // 蓄力時間(秒)
    [SerializeField] private LayerMask playerLayer;     // 玩家圖層
    [SerializeField] private Transform attackOrigin;    // 供 attackRangeBox 參考的原點

    [Header("----- Knockback Setting -----")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("-------- Appearance --------")]
    [SerializeField] private List<Sprite> guestAppearanceList = new List<Sprite>();

    [Header("----- Reference -----")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer guestSpriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject attackHitBox;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject attackRangeBox;
    [SerializeField] private GameObject hpBar;
    [SerializeField] private Transform barFill;
    [SerializeField] private GameObject dieVFX;
    [SerializeField] private GameObject attackVFX;
    [SerializeField] private BeGrabByPlayer beGrabByPlayer;

    [Header("----- Stun Controller (外掛) -----")]
    [SerializeField] private StunController stun; // 交由外部暈眩元件控制

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
        SetSprite();

        if (RoundManager.Instance) player = RoundManager.Instance.player;

        maxHp = Random.Range(1, 4);
        currentHp = maxHp;

        if (hpBar != null) hpBar.SetActive(false);
        if (attackHitBox != null) attackHitBox.SetActive(false);
        if (attackRangeBox != null) attackRangeBox.SetActive(false);

        lastAttackTime = -Mathf.Infinity;
        isCharging = false;

        // 事件註冊：被抓/放下（停/啟 NavMeshAgent）
        // if (beGrabByPlayer != null)
        // {
        //     beGrabByPlayer.RegisterOnBeGrabbingAction(true, OnGrabbedByPlayer);
        //     beGrabByPlayer.RegisterOnBeGrabbingAction(false, OnReleasedByPlayer);
        // }

        TryEnsureOnNavMesh();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(EndAttack));
        isCharging = false;

        // if (beGrabByPlayer != null)
        // {
        //     beGrabByPlayer.UnregisterOnBeGrabbingAction(true, OnGrabbedByPlayer);
        //     beGrabByPlayer.UnregisterOnBeGrabbingAction(false, OnReleasedByPlayer);
        // }
    }

    private void Update()
    {
        if (player == null) return;

        // ✅ 不再因為 agent.isStopped 就直接 return，
        //    只在「真的暈眩時」跳出攻擊/移動流程（但仍讓其他計時正常更新）
        if (stun != null && stun.IsStunned())
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            return;
        }

        // ===== 左右翻面 =====
        if (agent != null && agent.enabled)
        {
            if (agent.velocity.x < -0.01f)
                transform.rotation = Quaternion.Euler(0, 0, 0);
            else if (agent.velocity.x > 0.01f)
                transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        // ===== 蓄力期間不移動，但會持續計時，時間到就 PerformAttack() =====
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
                // 這裡不 return，讓其他邏輯仍能照原本節奏運作（如動畫或特效計時）
                return;
            }
        }

        // ===== 追玩家 =====
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        // ===== 進入攻擊範圍就開始蓄力 =====
        if (Time.time - lastAttackTime >= attackCooldown && IsPlayerInRange())
        {
            StartCharge();
        }
    }

    /// <summary>從 attackRangeBox 取得攻擊半徑</summary>
    /// <summary>從 attackRangeBox 取得攻擊半徑</summary>
    /// <summary>從 attackRangeBox 取得攻擊半徑</summary>
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

    /// <summary>嘗試把 Agent 放回 NavMesh 合法位置。</summary>
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

    public void SetSprite(Sprite sprite = null)
    {
        guestSpriteRenderer.color = Color.white;
        if (sprite != null)
        {
            guestSpriteRenderer.sprite = sprite;
            return;
        }

        if (guestAppearanceList is { Count: > 0 })
        {
            int idx = Random.Range(0, guestAppearanceList.Count);
            guestSpriteRenderer.sprite = guestAppearanceList[idx];
            return;
        }

        Debug.LogWarning("Not Get Enemy Guest Sprite!!!!");
    }

    public void TakeDamage(int damage)
    {
        if (hpBar != null) hpBar.SetActive(true);
        currentHp -= damage;

        float ratio = (float)currentHp / maxHp;
        if (barFill != null) barFill.localScale = new Vector3(ratio, 1f, 1f);

        if (currentHp <= 0)
        {
            Dead(true);
        }
    }

    private void Dead(bool isDefeated)
    {
        if (isDefeated)
        {
            VFXPool.Instance.SpawnVFX("CoinFountain", (attackOrigin != null ? attackOrigin.position : transform.position), Quaternion.identity, 2f);
            RoundManager.Instance.DefeatEnemySuccess();
        }

        if (poolHandler != null) poolHandler.Release();
        else gameObject.SetActive(false);
    }

    private IEnumerator ApplyKnockback(Vector2 direction)
    {
        if (agent == null) yield break;

        // 被擊退時先暫停 NavMesh 控制，直接位移 Transform
        bool reEnableAfter = false;
        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false; // 短暫關閉避免被 NavMesh 拉回
            reEnableAfter = true;
        }

        if (animator != null) animator.SetTrigger(BeAttack);
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

        TakeDamage(1);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 原本的擊退
        if (other.CompareTag("AttackObject"))
        {
            Vector2 knockDir = (transform.position - other.transform.position).normalized;
            StartCoroutine(ApplyKnockback(knockDir));
        }

        // 「BasicAttack」造成暈眩的部分改由 StunController 處理

        if (other.CompareTag("ExitDoor"))
        {
            BeForceOut();
        }
    }

    private void BeForceOut()
    {
        // 取得原本的移動方向
        Vector2 moveDir = rb != null ? rb.velocity.normalized : Vector2.zero;
        if (moveDir == Vector2.zero && agent != null)
        {
            moveDir = agent.velocity.normalized;
        }
        if (moveDir == Vector2.zero)
        {
            moveDir = Vector2.right; // 預設一個方向，避免沒有速度時完全不動
        }

        // 計算目標位置（往前一段距離）
        float forwardDistance = 2f;   // 可調
        Vector3 targetPos = transform.position + (Vector3)(moveDir * forwardDistance);

        // 播放 Tween（0.5 秒推進），結束後呼叫 Dead(false)
        Tween.Position(transform, targetPos, 0.5f)
            .OnComplete(() => Dead(false));
    }

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
        // 1) 不再允許被抓
        if (beGrabByPlayer != null)
        {
            // 若仍在被抓狀態，強制鬆手（會連帶觸發已註冊的 OnReleasedByPlayer）
            if (beGrabByPlayer.GetIsOnBeGrabbing())
                beGrabByPlayer.SetIsOnBeGrabbing(false);

            beGrabByPlayer.SetIsCanBeGrabByPlayer(false);
        }

        // 2) 確保 Agent 恢復可用與行走
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
    // ===== 被抓/放下 事件（獨立於暈眩，用於切換 NavMeshAgent）=====
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
            // 是否立刻恢復移動交由暈眩與攻擊狀態決定
        }
    }
}
