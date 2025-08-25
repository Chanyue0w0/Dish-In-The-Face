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
	[SerializeField] private float attackCooldown = 2f; // Attack cooldown (seconds)
	[SerializeField] private float chargeTime = 1f;     // Charge time (seconds)
	[SerializeField] private LayerMask playerLayer;     // Player layer
	[SerializeField] private Transform attackOrigin;    // AttackRangeBox will be automatically assigned

	[Header("----- Knockback Setting -----")]
	[SerializeField] private float knockbackForce = 5f;     // Knockback force
	[SerializeField] private float knockbackDuration = 0.2f; // Knockback duration

	[Header("-------- Appearance --------")]
	[SerializeField] private List<Sprite> guestAppearanceList = new List<Sprite>();

	[Header("----- Reference -----")]
	[SerializeField] private SpriteRenderer guestSpriteRenderer;
	[SerializeField] private Animator animator;
	[SerializeField] private GameObject attackHitBox;   // Attack collision/effects
	[SerializeField] private NavMeshAgent agent;
	[SerializeField] private SpriteRenderer spriteRenderer;
	[SerializeField] private GameObject attackRangeBox; // Detection range based on this object's size
	[SerializeField] private GameObject hpBar;
	[SerializeField] private Transform barFill;
	[SerializeField] private GameObject dieVFX;
	[SerializeField] private GameObject attackVFX;

	private Transform player;
	private float lastAttackTime;
	private bool isCharging;
	private float chargeStartTime;

	private bool isKnockback = false;

	// Object pool handler
	private GuestPoolHandler poolHandler;

	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		agent.updateRotation = false;
		agent.updateUpAxis = false;
		agent.speed = moveSpeed;

		poolHandler = GetComponent<GuestPoolHandler>();

		// Default attack origin = Attack detection object's Transform
		if (attackOrigin == null && attackRangeBox != null)
			attackOrigin = attackRangeBox.transform; // Use attackRangeBox as reference:contentReference[oaicite:2]{index=2}
	}

	private void Start()
	{
		if (RoundManager.Instance) player = RoundManager.Instance.Player;
	}
	private void OnEnable()
	{
		SetSprite(); // Initialize appearance:contentReference[oaicite:3]{index=3}

		if (RoundManager.Instance) player = RoundManager.Instance.Player;

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
		// Cancel any pending Invoke calls to avoid EndAttack() being called after recycling
		CancelInvoke(nameof(EndAttack));
		isCharging = false;
	}

	private void Update()
	{
		if (player == null) return;

		// Face movement direction:contentReference[oaicite:4]{index=4}
		if (agent.velocity.x < -0.01f)
			transform.rotation = Quaternion.Euler(0, 0, 0);
		else if (agent.velocity.x > 0.01f)
			transform.rotation = Quaternion.Euler(0, 180, 0);

		// Charging: attack if player is still in range
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

		// Chase player
		agent.isStopped = false;
		agent.SetDestination(player.position);

		// Start charging when player in range and cooldown finished
		if (Time.time - lastAttackTime >= attackCooldown && IsPlayerInRange())
		{
			StartCharge();
		}
	}

	/// <summary>
	/// Get attack range from attackRangeBox, returns attackRange based on attached object.
	/// Reference: Use CircleCollider2D.radius (scaled) or SpriteRenderer.bounds.extents as attackRange
	/// </summary>
	private float GetAttackRadius()
	{
		// Attack detection object appearance determination
		if (attackRangeBox != null)
		{
			var t = attackRangeBox.transform;

			// 1) CircleCollider2D (circular detection)
			var circle = attackRangeBox.GetComponent<CircleCollider2D>();
			if (circle != null)
			{
				// Get max scale ratio (avoid mismatch with circular detection for non-uniform scaling)
				float scale = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
				return circle.radius * scale;
			}

			// 2) SpriteRenderer (use bounds as circle)
			var sr = attackRangeBox.GetComponent<SpriteRenderer>();
			if (sr != null && sr.sprite != null)
			{
				// Average x/y extents as circle radius
				var ext = sr.bounds.extents;
				return (ext.x + ext.y) * 0.5f;
			}
		}

		// 3) Default: empirical setting
		return 2f;
	}

	/// <summary>
	/// Use attackRangeBox position and size (circular detection) to check if player is in attack range.
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

		// Attack collision/effects
		if (attackHitBox != null) attackHitBox.SetActive(true);
		var fxPos = (attackOrigin != null ? attackOrigin.position : transform.position);
		VFXPool.Instance.SpawnVFX("Attack", fxPos, Quaternion.identity, 1f);
		AudioManager.Instance.PlayOneShot(FMODEvents.Instance.enemyAttack, transform.position);

		// Detection and attack (same circle)
		float radius = GetAttackRadius();
		if (attackOrigin == null) attackOrigin = transform;

		var hit = Physics2D.OverlapCircle(attackOrigin.position, radius, playerLayer);
		if (hit != null && hit.CompareTag("Player"))
		{
			var playerStatus = hit.GetComponent<PlayerStatus>();
			if (playerStatus != null)
				playerStatus.TakeDamage(atk);
		}

		// Can delay 0.2 seconds before disabling attack, protected by CancelInvoke in OnDisable()
		Invoke(nameof(EndAttack), 0.2f);
	}

	private void EndAttack()
	{
		// Check if object has been recycled/destroyed: preliminary check
		if (this == null || !gameObject.activeInHierarchy) return;
		if (agent == null || !agent.isActiveAndEnabled) return;

		// If not on NavMesh, try to return to NavMesh; if fails, can't find nearby area
		if (!agent.isOnNavMesh && !TryEnsureOnNavMesh(2f))
			return;

		if (attackHitBox != null) attackHitBox.SetActive(false);
		if (attackRangeBox != null) attackRangeBox.SetActive(false);

		agent.isStopped = false;
	}

	/// <summary>Try to move Agent back to nearest NavMesh position.</summary>
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

		isKnockback = true;
		agent.isStopped = true; // Stop NavMeshAgent movement

		animator.SetTrigger("BeAttack"); // Play hit animation
		float elapsed = 0f;
		while (elapsed < knockbackDuration)
		{
			// Move physics body
			transform.position += (Vector3)(direction * knockbackForce * Time.deltaTime);

			elapsed += Time.deltaTime;
			yield return null;
		}

		// Knockback finished, resume chasing
		agent.isStopped = false;
		isKnockback = false;
		TakeDamage(1);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("AttackObject"))
		{
			// Knockback direction
			Vector2 knockDir = (transform.position - other.transform.position).normalized;
			StartCoroutine(ApplyKnockback(knockDir));
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
