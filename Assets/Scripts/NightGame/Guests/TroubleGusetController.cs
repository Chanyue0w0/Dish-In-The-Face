using UnityEngine;
using UnityEngine.AI;

public class TroubleGusetController : MonoBehaviour
{
	[Header("----- Status -----")]
	[SerializeField] private int hp = 1;
	[SerializeField] private int atk = 1;
	[SerializeField] private float moveSpeed = 2f;  // 以格/s 計算時，每格 1 單位
	[SerializeField] private int rewardCoin = 10;

	[Header("----- Attack Setting -----")]
	[SerializeField] private float attackCooldown = 2f;
	[SerializeField] private float chargeTime = 1f;
	[SerializeField] private float attackRange = 1.5f;
	[SerializeField] private LayerMask playerLayer;


	[Header("----- Reference -----")]
	[SerializeField] private GameObject attackHitBox;
	[SerializeField] private NavMeshAgent agent;


	private Transform player;
	private float lastAttackTime = -Mathf.Infinity;
	private bool isCharging = false;
	private float chargeStartTime;

	private void Awake()
	{
		player = GameObject.FindGameObjectWithTag("Player").transform;
		agent.updateRotation = false;
		agent.updateUpAxis = false;
		agent.speed = moveSpeed;
	}

	private void Update()
	{
		if (isCharging)
		{
			if (Time.time - chargeStartTime >= chargeTime)
			{
				PerformAttack();
				isCharging = false;
			}
			else
			{
				agent.isStopped = true; // 蓄力中停止移動
				return;
			}
		}

		// 移動追蹤
		if (player != null)
		{
			agent.isStopped = false;
			agent.SetDestination(player.position);

			// 是否進入攻擊距離與冷卻
			float dist = Vector3.Distance(transform.position, player.position);
			if (dist <= attackRange && Time.time - lastAttackTime >= attackCooldown)
			{
				StartCharge();
			}
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
		// 攻擊玩家
		// 開啟hit box
		// 如果 hit box collider2d tag == player 呼叫 playerstatus
	}

	public void TakeDamage(int damage)
	{
		hp -= damage;
		if (hp <= 0)
		{
			// 撥錢、特效等等
			Destroy(gameObject);
		}
	}
}
