using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
	public float moveSpeed = 2f;
	public float directionChangeInterval = 1.5f;
	public float attackInterval = 5f;
	[SerializeField] private float attackHitBoxDuration = 0.1f; // 可調整攻擊判定持續時間
	[SerializeField] private GameObject attackHitBox;           // 攻擊碰撞盒

	private Rigidbody2D rb;
	private Vector2 movementDirection;
	private float directionChangeTimer;
	private float attackTimer;

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		ChooseNewDirection();
		directionChangeTimer = directionChangeInterval;
		attackTimer = attackInterval;

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

		attackTimer -= Time.deltaTime;
		if (attackTimer <= 0f)
		{
			Attack();
			attackTimer = attackInterval;
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
