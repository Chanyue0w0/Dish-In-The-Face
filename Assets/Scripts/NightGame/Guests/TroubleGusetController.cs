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
	[SerializeField] private float attackCooldown = 2f;
	[SerializeField] private float chargeTime = 1f;
	[SerializeField] private float attackRange = 1.5f;
	[SerializeField] private LayerMask playerLayer;

	[Header("----- Reference -----")]
	[SerializeField] private GameObject attackHitBox;
	[SerializeField] private NavMeshAgent agent;
	[SerializeField] private SpriteRenderer spriteRenderer;
	[SerializeField] private GameObject attackRangeBox;
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
		// 初始化 NavMeshAgent
		agent = GetComponent<NavMeshAgent>();
		agent.updateRotation = false;
		agent.updateUpAxis = false;
		agent.speed = moveSpeed;

		// 初始化物件池處理器
		poolHandler = GetComponent<GuestPoolHandler>();
	}

	private void OnEnable()
	{
		// 每次從物件池取出時重置狀態
		player = GameObject.FindGameObjectWithTag("Player")?.transform;

		maxHp = Random.Range(1, 4);
		currentHp = maxHp;

		hpBar.SetActive(false);
		attackHitBox.SetActive(false);
		attackRangeBox.SetActive(false);

		lastAttackTime = -Mathf.Infinity;
		isCharging = false;
	}

	private void Update()
	{
		if (player == null) return;

		// 翻轉朝向
		if (agent.velocity.x < -0.01f)
			transform.rotation = Quaternion.Euler(0, 0, 0);
		else if (agent.velocity.x > 0.01f)
			transform.rotation = Quaternion.Euler(0, 180, 0);

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

		agent.isStopped = false;
		agent.SetDestination(player.position);

		float dist = Vector3.Distance(transform.position, player.position);
		if (dist <= attackRange && Time.time - lastAttackTime >= attackCooldown)
		{
			StartCharge();
		}
	}

	private void StartCharge()
	{
		attackRangeBox.SetActive(true);
		isCharging = true;
		chargeStartTime = Time.time;
		lastAttackTime = Time.time;
		agent.isStopped = true;
	}

	private void PerformAttack()
	{
		attackHitBox.SetActive(true);
		Instantiate(attackVFX, attackHitBox.transform.position, Quaternion.identity);

		AudioManager.instance.PlayOneShot(FMODEvents.instance.enemyAttack, transform.position);

		Collider2D[] hits = new Collider2D[5];
		ContactFilter2D filter = new ContactFilter2D();
		filter.SetLayerMask(playerLayer);
		filter.useTriggers = true;

		int count = attackHitBox.GetComponent<Collider2D>().OverlapCollider(filter, hits);

		for (int i = 0; i < count; i++)
		{
			if (hits[i].CompareTag("Player"))
			{
				PlayerStatus playerStatus = hits[i].GetComponent<PlayerStatus>();
				if (playerStatus != null)
				{
					playerStatus.TakeDamage(atk);
					break;
				}
			}
		}

		Invoke(nameof(DisableAttackHitBox), 0.1f);
		agent.isStopped = false;
	}

	private void DisableAttackHitBox()
	{
		attackHitBox.SetActive(false);
		attackRangeBox.SetActive(false);
	}

	public void TakeDamage(int damage)
	{
		hpBar.SetActive(true);
		currentHp -= damage;

		float ratio = (float)currentHp / maxHp;
		barFill.localScale = new Vector3(ratio, 1f, 1f);

		if (currentHp <= 0)
		{
			Instantiate(dieVFX, attackHitBox.transform.position, Quaternion.identity);
			RoundManager.Instance.DefeatEnemySuccess();

			// 回收物件到物件池
			if (poolHandler != null)
				poolHandler.Release();
			else
				gameObject.SetActive(false);
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("AttackObject"))
		{
			TakeDamage(1);
		}
	}
}
