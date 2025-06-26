using UnityEngine;
using UnityEngine.AI;

public class TroubleGusetController : MonoBehaviour
{
	[Header("----- Status -----")]
	[SerializeField] private int hp = 1;
	[SerializeField] private int atk = 1;
	[SerializeField] private float moveSpeed = 2f;  // �H��/s �p��ɡA�C�� 1 ���
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
				agent.isStopped = true; // �W�O�������
				return;
			}
		}

		// ���ʰl��
		if (player != null)
		{
			agent.isStopped = false;
			agent.SetDestination(player.position);

			// �O�_�i�J�����Z���P�N�o
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
		// �������a
		// �}��hit box
		// �p�G hit box collider2d tag == player �I�s playerstatus
	}

	public void TakeDamage(int damage)
	{
		hp -= damage;
		if (hp <= 0)
		{
			// �����B�S�ĵ���
			Destroy(gameObject);
		}
	}
}
