using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.AI;

public class TroubleGusetController : MonoBehaviour
{
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
	[SerializeField] private SpriteRenderer spriteRenderer;
	[SerializeField] private GameObject attackRangeBox;
	[SerializeField] private GameObject hpBar;
	[SerializeField] private Transform barFill;
	[SerializeField] private GameObject dieVFX;
	[SerializeField] private GameObject attackVFX;
	[SerializeField] private BeGrabByPlayer beGrabByPlayer;

	// === 暈眩系統（累積→爆表→倒數） ===
	[Header("----- Stun (暈眩) -----")]
	[SerializeField] private int maxStun = 10;              // 累積暈眩上限
	[SerializeField] private float stunDuration = 3f;       // NEW 進入暈眩後的倒數秒數
	[SerializeField] private GameObject stunBar;            // 暈眩條根物件
	[SerializeField] private Transform stunBarFill;         // X 縮放顯示
	private int currentStun = 0;                            // 累積值（0 → maxStun）
	private bool isStunned = false;                         // 是否處於「暈眩中/倒數中」
	private float stunRemaining = 0f;                       // NEW 暈眩剩餘秒數（滿→0）
	private bool stunCountdownPaused = false;               // NEW 是否暫停倒數（被抓時）

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

		// 初始化暈眩狀態
		currentStun = 0;
		isStunned = false;
		stunRemaining = 0f;
		stunCountdownPaused = false;
		beGrabByPlayer.SetIsCanBeGrabByPlayer(false);
		UpdateStunBarFill();

		if (stunBar != null) stunBar.SetActive(false);
		if (hpBar != null) hpBar.SetActive(false);
		if (attackHitBox != null) attackHitBox.SetActive(false);
		if (attackRangeBox != null) attackRangeBox.SetActive(false);

		lastAttackTime = -Mathf.Infinity;
		isCharging = false;

		// 事件註冊：被抓/放下時的處理（停/啟 NavMeshAgent、暫停/恢復倒數） // NEW
		if (beGrabByPlayer != null)
		{
			beGrabByPlayer.RegisterOnBeGrabbingAction(true, OnGrabbedByPlayer);
			beGrabByPlayer.RegisterOnBeGrabbingAction(false, OnReleasedByPlayer);
		}

		TryEnsureOnNavMesh(2f);
	}

	private void OnDisable()
	{
		CancelInvoke(nameof(EndAttack));
		isCharging = false;

		// 解除註冊，避免記憶體洩漏 // NEW
		if (beGrabByPlayer != null)
		{
			beGrabByPlayer.UnregisterOnBeGrabbingAction(true, OnGrabbedByPlayer);
			beGrabByPlayer.UnregisterOnBeGrabbingAction(false, OnReleasedByPlayer);
		}
	}

	private void Update()
	{
		if (player == null) return;

		// ===== 暈眩倒數邏輯（即使不能移動，也要在 Update 內持續跑） ===== // NEW
		if (isStunned)
		{
			if (!stunCountdownPaused && stunDuration > 0f)
			{
				stunRemaining -= Time.deltaTime;
				if (stunRemaining <= 0f)
				{
					RecoverFromStun(); // 自動恢復：清空暈眩、關條、恢復行動
				}
				else
				{
					UpdateStunBarFill(); // 用「剩餘/總長」更新 1→0
				}
			}
			// 暈眩中不進行移動/攻擊
			return;
		}

		// ===== 角色左右翻面（非暈眩）=====
		if (agent != null && agent.enabled)
		{
			if (agent.velocity.x < -0.01f)
				transform.rotation = Quaternion.Euler(0, 0, 0);
			else if (agent.velocity.x > 0.01f)
				transform.rotation = Quaternion.Euler(0, 180, 0);
		}

		// ===== 蓄力期間不移動 =====
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

		if (!agent.isOnNavMesh && !TryEnsureOnNavMesh(2f))
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
		if (sprite != null)
		{
			guestSpriteRenderer.sprite = sprite;
			return;
		}

		if (guestAppearanceList != null && guestAppearanceList.Count > 0)
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
			agent.enabled = false; // CHG：短暫關閉避免滾回 NavMesh 控制
			reEnableAfter = true;
		}

		if (animator != null) animator.SetTrigger("BeAttack");
		float elapsed = 0f;
		while (elapsed < knockbackDuration)
		{
			transform.position += (Vector3)(direction * knockbackForce * Time.deltaTime);
			elapsed += Time.deltaTime;
			yield return null;
		}

		// 恢復 NavMesh
		if (reEnableAfter && agent != null)
		{
			agent.enabled = true;
			TryEnsureOnNavMesh(2f);
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

		// 被「BasicAttack」的 hitbox 攻擊，暈眩值 +1（示例）
		if (other.CompareTag("BasicAttack"))
		{
			AddStun(1);
		}
		
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
	// ===== 暈眩：核心區 =====
	// =========================

	/// <summary>外部或事件呼叫：增加暈眩值。</summary>
	private void AddStun(int amount)
	{
		// 顯示暈眩條
		stunBar.SetActive(true);

		// 已在暈眩倒數中就不再累積
		if (isStunned) return;

		currentStun = Mathf.Clamp(currentStun + amount, 0, maxStun);
		if (stunBar != null) stunBar.SetActive(true);
		UpdateStunBarFill(); // 用「累積/maxStun」顯示 0→1

		if (currentStun >= maxStun)
			OnStunFull();
	}

	/// <summary>暈眩值達上限：進入暈眩狀態並開始倒數。</summary>
	private void OnStunFull()
	{
		isStunned = true;
		stunRemaining = Mathf.Max(0f, stunDuration); // NEW：從滿秒開始倒數
		stunCountdownPaused = false;

		if (stunBar != null) stunBar.SetActive(true);
		UpdateStunBarFill(); // 這時顯示「剩餘/總秒」= 1

		if (agent != null && agent.enabled) agent.isStopped = true;

		// 可被玩家抓
		if (beGrabByPlayer != null) beGrabByPlayer.SetIsCanBeGrabByPlayer(true);

		// 動畫（可選）
		if (animator != null)
		{
			// animator.SetBool("IsStunned", true);
		}
	}

	/// <summary>倒數結束或外部強制解除時的恢復流程。</summary>
	private void RecoverFromStun()
	{
		isStunned = false;
		currentStun = 0;
		stunRemaining = 0f;
		stunCountdownPaused = false;

		// 關閉暈眩條
		if (stunBar != null) stunBar.SetActive(false);
		UpdateStunBarFill();

		// 不可再被抓（回到一般狀態）
		if (beGrabByPlayer != null) beGrabByPlayer.SetIsCanBeGrabByPlayer(false);

		if (agent != null && agent.enabled)
			agent.isStopped = false;

		if (animator != null)
		{
			// animator.SetBool("IsStunned", false);
		}
	}

	/// <summary>（保留原 API）外部可呼叫：解除暈眩並清空暈眩值。</summary>
	public void ResetStun()
	{
		RecoverFromStun();
	}

	/// <summary>更新暈眩條的顯示。</summary>
	private void UpdateStunBarFill()
	{
		if (stunBarFill == null) return;

		float ratio = 0f;
		if (isStunned)
		{
			// 暈眩中：顯示剩餘時間（1 → 0）
			ratio = (stunDuration > 0f) ? Mathf.Clamp01(stunRemaining / stunDuration) : 0f;
		}
		else
		{
			// 累積中：顯示累積比例（0 → 1）
			ratio = (maxStun > 0) ? Mathf.Clamp01((float)currentStun / maxStun) : 0f;
		}
		stunBarFill.localScale = new Vector3(ratio, 1f, 1f);
	}

	// =========================
	// ===== 被抓/放下 事件 =====
	// =========================

	// 被抓：停用 NavMeshAgent（允許自由位移/丟擲），暫停暈眩倒數 // NEW
	private void OnGrabbedByPlayer()
	{
		// 暫停倒數
		stunCountdownPaused = true;

		// 停用 NavMeshAgent 以解除 NavMesh 限制
		if (agent != null && agent.enabled)
		{
			agent.isStopped = true;
			agent.enabled = false;
		}
	}

	// 被放下：恢復 NavMeshAgent（嘗試回到合法 NavMesh），恢復暈眩倒數 // NEW
	private void OnReleasedByPlayer()
	{
		// 先恢復 Agent
		if (agent != null && !agent.enabled)
		{
			agent.enabled = true;
			TryEnsureOnNavMesh(2f);
			agent.isStopped = isStunned || isCharging;
		}

		// 恢復倒數（如果仍在暈眩中）
		if (isStunned) stunCountdownPaused = false;
	}
}
