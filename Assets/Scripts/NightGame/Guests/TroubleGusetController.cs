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
	[SerializeField] private RoundManager roundManager;
	[SerializeField] private GameObject attackRangeBox;


	[SerializeField] private GameObject hpBar;              // 血條物件
	[SerializeField] private Transform barFill;             // 血條內部填色

	[SerializeField] private GameObject dieVFX;
	[SerializeField] private GameObject attackVFX;

	private Transform player;
	private float lastAttackTime = -Mathf.Infinity;
	private bool isCharging = false;
	private float chargeStartTime;

	private void Awake()
	{
		roundManager = GameObject.Find("Rround Manager").GetComponent<RoundManager>();
		player = GameObject.FindGameObjectWithTag("Player").transform;


		maxHp = Random.Range(1, 4); // 隨機 1~3
		currentHp = maxHp;

		agent.updateRotation = false;
		agent.updateUpAxis = false;
		agent.speed = moveSpeed;

		hpBar.SetActive(false);
		attackHitBox.SetActive(false); // 初始關閉
		attackRangeBox.SetActive(false);
	}

	private void Update()
	{
		if (player == null) return;


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
		attackRangeBox.SetActive (true);
		isCharging = true;
		chargeStartTime = Time.time;
		lastAttackTime = Time.time;
		agent.isStopped = true;
	}

	private void PerformAttack()
	{
		attackHitBox.SetActive(true);
		Instantiate(attackVFX, attackHitBox.transform.position, Quaternion.identity);

		// 用 Collider2D 判斷是否打中 Player
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
					//Debug.Log("Trouble guest attacked player via hitbox!");
				}
			}
		}

		// 0.1 秒後關閉 hitbox，繼續移動
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
		//Debug.Log(ratio);
		barFill.localScale = new Vector3(ratio, 1f, 1f);
		
		//Debug.Log(this.name + "die");
		if (currentHp <= 0)
		{
			Instantiate(dieVFX, attackHitBox.transform.position, Quaternion.identity);
			roundManager.DefeatEnemySuccess();
			Destroy(gameObject);
		}

	}

	// 偵測到被攻擊
	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("AttackObject"))
		{
			Collider2D beAttackedObj = other;
			TakeDamage(1);
		}
	}
}
