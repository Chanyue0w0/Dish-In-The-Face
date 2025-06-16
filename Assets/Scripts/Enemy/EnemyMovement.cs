using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
	public float moveSpeed = 2f;
	public float directionChangeInterval = 1.5f;
	public float attackCheckInterval = 1.0f; // 每隔幾秒檢查是否攻擊
	[SerializeField] private float attackProbability = 0.3f; // 攻擊機率（0~1）
	[SerializeField] private float attackHitBoxDuration = 0.1f;
	[SerializeField] private GameObject attackHitBox;

	private Rigidbody2D rb;
	private Vector2 movementDirection;
	private float directionChangeTimer;
	private float attackCheckTimer;

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		ChooseNewDirection();
		directionChangeTimer = directionChangeInterval;
		attackCheckTimer = attackCheckInterval;

		if (attackHitBox != null)
			attackHitBox.SetActive(false);
	}

	void Update()
	{
		directionChangeTimer -= Time.deltaTime;
		if (directionChangeTimer <= 0f)
		{
			ChooseNewDirection();
			directionChangeTimer = directionChangeInterval;
		}

		attackCheckTimer -= Time.deltaTime;
		if (attackCheckTimer <= 0f)
		{
			// 機率性攻擊
			if (Random.value < attackProbability)
			{
				Attack();
			}
			attackCheckTimer = attackCheckInterval;
		}
	}

	void FixedUpdate()
	{
		rb.velocity = movementDirection * moveSpeed;
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
}
