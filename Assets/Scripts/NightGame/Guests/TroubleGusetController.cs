using System.Collections;
using System.Collections.Generic;
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
	
	// === 新增：暈眩系統 ===
	[Header("----- Stun (暈眩) -----")]
	[SerializeField] private int maxStun = 10;             // 暈眩最大值（可調）
	[SerializeField] private GameObject stunBar;           // 暈眩條（被攻擊時顯示）
	[SerializeField] private Transform stunBarFill;        // 暈眩條填滿（縮放 X 0→1）
	private int currentStun = 0;                           // 從 0 開始
	private bool isStunned = false;                        // 是否已暈眩（停止移動控制）

	private Transform player;
	private float lastAttackTime;
	private bool isCharging;
	private float chargeStartTime;

	//private bool isKnockback = false;
	private GuestPoolHandler poolHandler;

	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		agent.updateRotation = false;
		agent.updateUpAxis = false;
		agent.speed = moveSpeed;

		poolHandler = GetComponent<GuestPoolHandler>();

		if (attackOrigin == null && attackRangeBox != null)
			attackOrigin = attackRangeBox.transform;
	}

	private void Start()
	{
		if (RoundManager.Instance) player = RoundManager.Instance.Player;
	}

	private void OnEnable()
	{
		SetSprite();

		if (RoundManager.Instance) player = RoundManager.Instance.Player;

		maxHp = Random.Range(1, 4);
		currentHp = maxHp;

		// 重置暈眩
		currentStun = 0;
		isStunned = false;
		stunBar.SetActive(false);
		UpdateStunBarFill();

		if (hpBar != null) hpBar.SetActive(false);
		if (attackHitBox != null) attackHitBox.SetActive(false);
		if (attackRangeBox != null) attackRangeBox.SetActive(false);

		lastAttackTime = -Mathf.Infinity;
		isCharging = false;

		TryEnsureOnNavMesh(2f);
	}

	private void OnDisable()
	{
		CancelInvoke(nameof(EndAttack));
		isCharging = false;
	}

	private void Update()
	{
		if (player == null) return;

		// 如果已暈眩，完全停止移動控制
		if (isStunned)
		{
			agent.isStopped = true;
			beGrabByPlayer.SetIsCanBeGrabByPlayer(true);
			return;
		}
		else
		{
			beGrabByPlayer.SetIsCanBeGrabByPlayer(false);
		}

		// 角色左右翻面
		if (agent.velocity.x < -0.01f)
			transform.rotation = Quaternion.Euler(0, 0, 0);
		else if (agent.velocity.x > 0.01f)
			transform.rotation = Quaternion.Euler(0, 180, 0);

		// 蓄力期間不移動
		if (isCharging)
		{
			if (Time.time - chargeStartTime >= chargeTime)
			{
				PerformAttack();
				isCharging = false;
			}
			else
			{
				agent.isStopped = true;
				return;
			}
		}

		// 追玩家
		agent.isStopped = false;
		agent.SetDestination(player.position);

		// 進入攻擊範圍就開始蓄力
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
		agent.isStopped = true;
	}

	private void PerformAttack()
	{
		agent.isStopped = true;

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
		if (agent == null || !agent.isActiveAndEnabled) return;

		if (!agent.isOnNavMesh && !TryEnsureOnNavMesh(2f))
			return;

		if (attackHitBox != null) attackHitBox.SetActive(false);
		if (attackRangeBox != null) attackRangeBox.SetActive(false);

		agent.isStopped = false;
	}

	/// <summary>嘗試把 Agent 放回 NavMesh 合法位置。</summary>
	private bool TryEnsureOnNavMesh(float searchRadius = 2f)
	{
		if (agent == null || !agent.isActiveAndEnabled) return false;
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
			Dead();
		}
	}

	private void Dead()
	{
		VFXPool.Instance.SpawnVFX("CoinFountain", (attackOrigin != null ? attackOrigin.position : transform.position), Quaternion.identity, 2f);
		RoundManager.Instance.DefeatEnemySuccess();

		if (poolHandler != null) poolHandler.Release();
		else gameObject.SetActive(false);
	}

	private IEnumerator ApplyKnockback(Vector2 direction)
	{
		if (agent == null) yield break;

		agent.isStopped = true;

		if (animator != null) animator.SetTrigger("BeAttack");
		float elapsed = 0f;
		while (elapsed < knockbackDuration)
		{
			transform.position += (Vector3)(direction * knockbackForce * Time.deltaTime);
			elapsed += Time.deltaTime;
			yield return null;
		}

		agent.isStopped = false;
		TakeDamage(1);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		// 被任何攻擊到 → 顯示暈眩條
		if (stunBar != null) stunBar.SetActive(true);

		// 你原本的擊退來源
		if (other.CompareTag("AttackObject"))
		{
			Vector2 knockDir = (transform.position - other.transform.position).normalized;
			StartCoroutine(ApplyKnockback(knockDir));
		}

		// 新增：被「BasicAttack」的 hitbox 攻擊，暈眩值 +1
		if (other.CompareTag("BasicAttack"))
		{
			AddStun(1);
		}
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
		currentStun = Mathf.Clamp(currentStun + amount, 0, maxStun);
		UpdateStunBarFill();

		if (currentStun >= maxStun)
			OnStunFull();
	}

	/// <summary>當暈眩值達上限：停止移動控制。</summary>
	private void OnStunFull()
	{
		isStunned = true;
		if (agent != null) agent.isStopped = true;

		// 若有動畫可用，這裡可切換暈眩狀態
		if (animator != null)
		{
			// 建議做一個暈眩迴圈動畫；這裡用 Bool 範例
			// animator.SetBool("IsStunned", true);
		}
	}

	/// <summary>（可選）外部可呼叫：解除暈眩並清空暈眩值。</summary>
	public void ResetStun()
	{
		isStunned = false;
		currentStun = 0;
		if (agent != null) agent.isStopped = false;

		UpdateStunBarFill();

		// if (stunBar != null) stunBar.SetActive(false);
		// if (animator != null) animator.SetBool("IsStunned", false);
	}

	/// <summary>更新暈眩條的顯示（0→1）。</summary>
	private void UpdateStunBarFill()
	{
		if (stunBarFill != null && maxStun > 0)
		{
			float ratio = (float)currentStun / maxStun;
			stunBarFill.localScale = new Vector3(ratio, 1f, 1f);
		}
	}
}
