using UnityEngine;
using System.Collections;

public class TempEnemyMovement : MonoBehaviour
{
	public float moveSpeed = 2f;
	public float directionChangeInterval = 1.5f;
	public float attackCheckInterval = 1.0f;
	public float attackProbability = 0.3f;
	public float attackHitBoxDuration = 0.1f;
	public bool isMoveToPlayer = false;

	[SerializeField] private GameObject attackHitBox;

	private Rigidbody2D rb;
	private Vector2 movementDirection;
	private float directionChangeTimer;
	private float attackCheckTimer;

	private Transform player;

	void Start()
	{
		isMoveToPlayer = Random.value < 0.5f; // 50% 機率朝玩家移動


		rb = GetComponent<Rigidbody2D>();
		player = GameObject.FindGameObjectWithTag("Player")?.transform;

		ChooseNewDirection();
		directionChangeTimer = directionChangeInterval;
		attackCheckTimer = attackCheckInterval;

		if (attackHitBox != null)
			attackHitBox.SetActive(false);
	}

	void Update()
	{
		if (isMoveToPlayer)
		{
			MoveTowardPlayer();
		}
		else
		{
			WanderMovement();
		}

		attackCheckTimer -= Time.deltaTime;
		if (attackCheckTimer <= 0f)
		{
			if (Random.value < attackProbability)
			{
				Attack();
			}
			attackCheckTimer = attackCheckInterval;
		}

		UpdateFacingDirection();
	}

	void FixedUpdate()
	{
		rb.velocity = movementDirection * moveSpeed;
	}

	// ---------------- 移動模式 ----------------

	void WanderMovement()
	{
		directionChangeTimer -= Time.deltaTime;
		if (directionChangeTimer <= 0f)
		{
			ChooseNewDirection();
			directionChangeTimer = directionChangeInterval;
		}
	}

	void MoveTowardPlayer()
	{
		if (player != null)
		{
			Vector2 dirToPlayer = (player.position - transform.position).normalized;
			movementDirection = dirToPlayer;
		}
	}

	void ChooseNewDirection()
	{
		int dir = Random.Range(0, 4);
		switch (dir)
		{
			case 0: movementDirection = Vector2.up; break;
			case 1: movementDirection = Vector2.down; break;
			case 2: movementDirection = Vector2.left; break;
			case 3: movementDirection = Vector2.right; break;
		}
	}

	// ---------------- 攻擊 ----------------

	void Attack()
	{
		Debug.Log("Enemy attacks!");
		if (attackHitBox != null)
		{
			attackHitBox.SetActive(true);
			StartCoroutine(DisableAttackHitBoxAfterDelay(attackHitBoxDuration));
		}
	}

	IEnumerator DisableAttackHitBoxAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		if (attackHitBox != null)
			attackHitBox.SetActive(false);
	}

	// ---------------- 朝向調整 ----------------

	void UpdateFacingDirection()
	{
		if (movementDirection.x > 0.01f)
		{
			transform.rotation = Quaternion.Euler(0f, 0f, 0f);  // 面向右
		}
		else if (movementDirection.x < -0.01f)
		{
			transform.rotation = Quaternion.Euler(0f, 180f, 0f); // 面向左
		}
	}
}
