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
	[SerializeField] private float attackCooldown = 2f; // �����N�o(��)
	[SerializeField] private float chargeTime = 1f;     // �W�O�ɶ�(��)
	[SerializeField] private LayerMask playerLayer;     // ���a�ϼh
	[SerializeField] private Transform attackOrigin;    // �� attackRangeBox �۰ʱa�J

	[Header("-------- Appearance --------")]
	[SerializeField] private List<Sprite> guestAppearanceList = new List<Sprite>();

	[Header("----- Reference -----")]
	[SerializeField] private SpriteRenderer guestSpriteRenderer;
	[SerializeField] private GameObject attackHitBox;   // �µ�ı/�S��
	[SerializeField] private NavMeshAgent agent;
	[SerializeField] private SpriteRenderer spriteRenderer;
	[SerializeField] private GameObject attackRangeBox; // �� �Υ����j�p������d��
	[SerializeField] private GameObject hpBar;
	[SerializeField] private Transform barFill;
	[SerializeField] private GameObject dieVFX;
	[SerializeField] private GameObject attackVFX;

	private Transform player;
	private float lastAttackTime;
	private bool isCharging;
	private float chargeStartTime;

	// ������B�z��
	private GuestPoolHandler poolHandler;

	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		agent.updateRotation = false;
		agent.updateUpAxis = false;
		agent.speed = moveSpeed;

		poolHandler = GetComponent<GuestPoolHandler>();

		// �w�]�������� = �����d�򪫥� Transform
		if (attackOrigin == null && attackRangeBox != null)
			attackOrigin = attackRangeBox.transform; // �H attackRangeBox ������:contentReference[oaicite:2]{index=2}
	}

	private void OnEnable()
	{
		SetSprite(); // ��l�~�[�Ϊu�Υ~�[:contentReference[oaicite:3]{index=3}

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
		// �����|�����檺 Invoke�A�קK�^���ᤴ�I�s EndAttack()
		CancelInvoke(nameof(EndAttack));
		isCharging = false;
	}

	private void Update()
	{
		if (player == null) return;

		// ½�ୱ�V�]�̳t�ס^:contentReference[oaicite:4]{index=4}
		if (agent.velocity.x < -0.01f)
			transform.rotation = Quaternion.Euler(0, 0, 0);
		else if (agent.velocity.x > 0.01f)
			transform.rotation = Quaternion.Euler(0, 180, 0);

		// �W�O���G��a���ݪ���X��
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

		// �l�����a
		agent.isStopped = false;
		agent.SetDestination(player.position);

		// �i�J�����d��B�N�o���� �� �}�l�W�O
		if (Time.time - lastAttackTime >= attackCooldown && IsPlayerInRange())
		{
			StartCharge();
		}
	}

	/// <summary>
	/// �q attackRangeBox ���o�����b�|�F�Y���ѫh�h�^ attackRange�C
	/// �u�����ǡGCircleCollider2D.radius(�t�Y��) �� SpriteRenderer.bounds.extents �� attackRange
	/// </summary>
	private float GetAttackRadius()
	{
		// �������d�򪫥�~���P�w
		if (attackRangeBox != null)
		{
			var t = attackRangeBox.transform;

			// 1) CircleCollider2D�]��í�^
			var circle = attackRangeBox.GetComponent<CircleCollider2D>();
			if (circle != null)
			{
				// �b�|�ݭ��W�̤j�b�V�Y��]�קK�D�����Y��ɭP�b�|���u�^
				float scale = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
				return circle.radius * scale;
			}

			// 2) SpriteRenderer�]�� bounds ���b�|�^
			var sr = attackRangeBox.GetComponent<SpriteRenderer>();
			if (sr != null && sr.sprite != null)
			{
				// �� x/y �� extents �����@������b�|
				var ext = sr.bounds.extents;
				return (ext.x + ext.y) * 0.5f;
			}
		}

		// 3) ��ơG�γ]�w��
		return 2f;
	}

	/// <summary>
	/// �H attackRangeBox �����߻P�j�p�]�Ϋ�ƥb�|�^�˴����a�O�_�b�����d��C
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

		// ��ı/����
		if (attackHitBox != null) attackHitBox.SetActive(true);
		var fxPos = (attackOrigin != null ? attackOrigin.position : transform.position);
		VFXPool.Instance.SpawnVFX("Attack", fxPos, Quaternion.identity, 1f);
		AudioManager.Instance.PlayOneShot(FMODEvents.Instance.enemyAttack, transform.position);

		// �R���˴��]�P�@�Ӷ�^
		float radius = GetAttackRadius();
		if (attackOrigin == null) attackOrigin = transform;

		var hit = Physics2D.OverlapCircle(attackOrigin.position, radius, playerLayer);
		if (hit != null && hit.CompareTag("Player"))
		{
			var playerStatus = hit.GetComponent<PlayerStatus>();
			if (playerStatus != null)
				playerStatus.TakeDamage(atk);
		}

		// �o�̥i�ध�� 0.2 ������Q�^�� �� �� OnDisable() �� CancelInvoke �O�@
		Invoke(nameof(EndAttack), 0.2f);
	}

	private void EndAttack()
	{
		// ����i��w�Q�^��/���ΡG�����w���ˬd
		if (this == null || !gameObject.activeInHierarchy) return;
		if (agent == null || !agent.isActiveAndEnabled) return;

		// �Y���b NavMesh�A���մN���^ NavMesh�F���ѴN�O��_����
		if (!agent.isOnNavMesh && !TryEnsureOnNavMesh(2f))
			return;

		if (attackHitBox != null) attackHitBox.SetActive(false);
		if (attackRangeBox != null) attackRangeBox.SetActive(false);

		agent.isStopped = false;
	}

	/// <summary>���է� Agent ��^�̪� NavMesh ��m�C</summary>
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
