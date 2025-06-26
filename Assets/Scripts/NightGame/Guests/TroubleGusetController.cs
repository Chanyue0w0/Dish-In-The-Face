using UnityEngine;
using UnityEngine.AI;

public class TroubleGusetController : MonoBehaviour
{
	[Header("----- Status -----")]
	[SerializeField] private int hp = 1;
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

	private Transform player;
	private float lastAttackTime = -Mathf.Infinity;
	private bool isCharging = false;
	private float chargeStartTime;

	private void Awake()
	{

		roundManager = GameObject.Find("Rround Manager").GetComponent<RoundManager>();
		player = GameObject.FindGameObjectWithTag("Player").transform;
		agent.updateRotation = false;
		agent.updateUpAxis = false;
		agent.speed = moveSpeed;

		attackHitBox.SetActive(false); // 初始關閉
	}

	private void Update()
	{
		if (player == null) return;


		if (agent.velocity.x < -0.01f)
			transform.rotation = Quaternion.Euler(0, 180, 0);
		else if (agent.velocity.x > 0.01f)
			transform.rotation = Quaternion.Euler(0, 0, 0);


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
		isCharging = true;
		chargeStartTime = Time.time;
		lastAttackTime = Time.time;
		agent.isStopped = true;
	}

	private void PerformAttack()
	{
		attackHitBox.SetActive(true);

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
	}


	public void TakeDamage(int damage)
	{
		Debug.Log(this.name + "die");
		hp -= damage;
		if (hp <= 0)
		{
			roundManager.DefeatEnemySuccess();
			Destroy(gameObject);
		}
	}

	//private void OnDrawGizmosSelected()
	//{
	//	if (spriteRenderer == null) return;
	//	Vector3 attackCenter = transform.position + transform.right * (spriteRenderer.flipX ? -1 : 1) * attackRange * 0.5f;
	//	Gizmos.color = Color.red;
	//	Gizmos.DrawWireSphere(attackCenter, 0.5f);
	//}
}
