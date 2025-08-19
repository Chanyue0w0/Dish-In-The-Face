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
	[SerializeField] private float attackCooldown = 2f; // 攻擊冷卻(秒)
	[SerializeField] private float chargeTime = 1f;     // 蓄力時間(秒)
	[SerializeField] private LayerMask playerLayer;     // 玩家圖層
	[SerializeField] private Transform attackOrigin;    // 由 attackRangeBox 自動帶入

	[Header("-------- Appearance --------")]
	[SerializeField] private List<Sprite> guestAppearanceList = new List<Sprite>();

	[Header("----- Reference -----")]
	[SerializeField] private SpriteRenderer guestSpriteRenderer;
	[SerializeField] private GameObject attackHitBox;   // 純視覺/特效
	[SerializeField] private NavMeshAgent agent;
	[SerializeField] private SpriteRenderer spriteRenderer;
	[SerializeField] private GameObject attackRangeBox; // ← 用它的大小當攻擊範圍
	[SerializeField] private GameObject hpBar;
	[SerializeField] private Transform barFill;
	[SerializeField] private GameObject dieVFX;
	[SerializeField] private GameObject attackVFX;

	private Transform player;
	private float lastAttackTime;
	private bool isCharging;
	private float chargeStartTime;

	// 物件池處理器
	private GuestPoolHandler poolHandler;

	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		agent.updateRotation = false;
		agent.updateUpAxis = false;
		agent.speed = moveSpeed;

		poolHandler = GetComponent<GuestPoolHandler>();

		// 預設攻擊中心 = 攻擊範圍物件的 Transform
		if (attackOrigin == null && attackRangeBox != null)
			attackOrigin = attackRangeBox.transform; // 以 attackRangeBox 為中心:contentReference[oaicite:2]{index=2}
	}

	private void OnEnable()
	{
		SetSprite(); // 初始外觀或沿用外觀:contentReference[oaicite:3]{index=3}

		player = GameObject.FindGameObjectWithTag("Player")?.transform;

		maxHp = Random.Range(1, 4);
		currentHp = maxHp;

		if (hpBar != null) hpBar.SetActive(false);
		if (attackHitBox != null) attackHitBox.SetActive(false);
		if (attackRangeBox != null) attackRangeBox.SetActive(false);

		lastAttackTime = -Mathf.Infinity;
		isCharging = false;

		TryEnsureOnNavMesh(2f);
	}
	private void OnDisable()
	{
		// 取消尚未執行的 Invoke，避免回收後仍呼叫 EndAttack()
		CancelInvoke(nameof(EndAttack));
		isCharging = false;
	}

	private void Update()
	{
		if (player == null) return;

		// 翻轉面向（依速度）:contentReference[oaicite:4]{index=4}
		if (agent.velocity.x < -0.01f)
			transform.rotation = Quaternion.Euler(0, 0, 0);
		else if (agent.velocity.x > 0.01f)
			transform.rotation = Quaternion.Euler(0, 180, 0);

		// 蓄力中：原地等待直到出手
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

		// 追擊玩家
		agent.isStopped = false;
		agent.SetDestination(player.position);

		// 進入攻擊範圍且冷卻結束 → 開始蓄力
		if (Time.time - lastAttackTime >= attackCooldown && IsPlayerInRange())
		{
			StartCharge();
		}
	}

	/// <summary>
	/// 從 attackRangeBox 推得攻擊半徑；若失敗則退回 attackRange。
	/// 優先順序：CircleCollider2D.radius(含縮放) → SpriteRenderer.bounds.extents → attackRange
	/// </summary>
	private float GetAttackRadius()
	{
		// 有攻擊範圍物件才有判定
		if (attackRangeBox != null)
		{
			var t = attackRangeBox.transform;

			// 1) CircleCollider2D（最穩）
			var circle = attackRangeBox.GetComponent<CircleCollider2D>();
			if (circle != null)
			{
				// 半徑需乘上最大軸向縮放（避免非等比縮放導致半徑失真）
				float scale = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
				return circle.radius * scale;
			}

			// 2) SpriteRenderer（用 bounds 推半徑）
			var sr = attackRangeBox.GetComponent<SpriteRenderer>();
			if (sr != null && sr.sprite != null)
			{
				// 取 x/y 的 extents 平均作為近似半徑
				var ext = sr.bounds.extents;
				return (ext.x + ext.y) * 0.5f;
			}
		}

		// 3) 後備：用設定值
		return 2f;
	}

	/// <summary>
	/// 以 attackRangeBox 的中心與大小（或後備半徑）檢測玩家是否在攻擊範圍。
	/// </summary>
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

		// 視覺/音效
		if (attackHitBox != null) attackHitBox.SetActive(true);
		var fxPos = (attackOrigin != null ? attackOrigin.position : transform.position);
		VFXPool.Instance.SpawnVFX("Attack", fxPos, Quaternion.identity, 1f);
		AudioManager.Instance.PlayOneShot(FMODEvents.Instance.enemyAttack, transform.position);

		// 命中檢測（同一個圓）
		float radius = GetAttackRadius();
		if (attackOrigin == null) attackOrigin = transform;

		var hit = Physics2D.OverlapCircle(attackOrigin.position, radius, playerLayer);
		if (hit != null && hit.CompareTag("Player"))
		{
			var playerStatus = hit.GetComponent<PlayerStatus>();
			if (playerStatus != null)
				playerStatus.TakeDamage(atk);
		}

		// 這裡可能之後 0.2 秒內物件被回收 → 由 OnDisable() 的 CancelInvoke 保護
		Invoke(nameof(EndAttack), 0.2f);
	}

	private void EndAttack()
	{
		// 物件可能已被回收/停用：先做安全檢查
		if (this == null || !gameObject.activeInHierarchy) return;
		if (agent == null || !agent.isActiveAndEnabled) return;

		// 若不在 NavMesh，嘗試就近放回 NavMesh；失敗就別恢復移動
		if (!agent.isOnNavMesh && !TryEnsureOnNavMesh(2f))
			return;

		if (attackHitBox != null) attackHitBox.SetActive(false);
		if (attackRangeBox != null) attackRangeBox.SetActive(false);

		agent.isStopped = false;
	}

	/// <summary>嘗試把 Agent 放回最近的 NavMesh 位置。</summary>
	private bool TryEnsureOnNavMesh(float searchRadius = 2f)
	{
		if (agent == null || !agent.isActiveAndEnabled) return false;
		if (agent.isOnNavMesh) return true;

		NavMeshHit hit;
		if (NavMesh.SamplePosition(transform.position, out hit, searchRadius, NavMesh.AllAreas))
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
			VFXPool.Instance.SpawnVFX("CoinFountain", (attackOrigin != null ? attackOrigin.position : transform.position), Quaternion.identity, 2f);
			RoundManager.Instance.DefeatEnemySuccess();

			if (poolHandler != null) poolHandler.Release();
			else gameObject.SetActive(false);
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("AttackObject"))
		{
			TakeDamage(1);
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
}
