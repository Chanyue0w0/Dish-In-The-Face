using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempPlayerController : MonoBehaviour
{
	[Header("-------- Move Setting ---------")]
	[SerializeField] private float moveSpeed = 5f;

	[Header("Dash")]
	[SerializeField] private float dashSpeed = 10f;           // 閃避時的速度
	[SerializeField] private float dashDistance = 2f;         // 閃避距離（位移距離 = dashSpeed * dashDuration）
	[SerializeField] private float dashCooldown = 0.1f;       // 閃避冷卻時間
	[SerializeField] private List<string> passThroughTags = new List<string> { "Table", "Enemy" };// 閃避時可穿過的 tag


	[Header("Attack")]
	[SerializeField] private float attackHitBoxDuration = 0.1f; // 可調整的攻擊判定持續時間
	[Header("-------- State ---------")]
	[SerializeField] private bool isDashing = false;


	[Header("-------- Reference ---------")]
	[Header("Script")]
	[SerializeField] private TableGroupManager tableGroupManager;
	[Header("Object")]
	[SerializeField] private GameObject handItemNow; // 玩家手上的道具顯示
	[SerializeField] private GameObject attackHitBox;

	private Collider2D currentFoodTrigger;   // 當前接觸到的食物觸發器
	private Collider2D currentTableCollider; // 當前接觸到的桌子碰撞器

	private Rigidbody2D rb;
	private SpriteRenderer spriteRenderer;
	private Collider2D playerCollider;
	private Animator animator;

	private Vector2 moveInput;
	private Vector2 moveVelocity;

	private float dashTimer = 0f;
	private float dashDuration;
	private float lastDashTime = -999f;

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		playerCollider = GetComponent<Collider2D>();
		animator = GetComponent<Animator>();

		// 計算持續時間 = 跑完 dashDistance 所需時間
		dashDuration = dashDistance / dashSpeed;

		handItemNow.SetActive(false);
		attackHitBox.SetActive(false);
	}

	void Update()
	{
		HandleMovementInput();
		HandleActionInput();

		if (isDashing)
		{
			dashTimer -= Time.deltaTime;
			ShadowPool.instance.GetFormPool();
			if (dashTimer <= 0f)
			{
				EndDash();
			}
		}

		UpdateAnimatorStates();

	}

	void FixedUpdate()
	{
		Move();
	}

	void HandleMovementInput()
	{
		float moveX = Input.GetAxisRaw("Horizontal");
		float moveY = Input.GetAxisRaw("Vertical");
		moveInput = new Vector2(moveX, moveY).normalized;

		if (isDashing)
		{
			moveVelocity = moveInput * dashSpeed;
		}
		else
		{
			moveVelocity = moveInput * moveSpeed;
		}

		if (moveX != 0)
		{
			gameObject.transform.rotation = (moveX < 0) ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);
		}
	}

	void Move()
	{
		rb.velocity = moveVelocity;
	}

	void HandleActionInput()
	{
		// 撿取物品或使用
		if (Input.GetKeyDown(KeyCode.E))
		{
			Interact();
		}

		// 攻擊或投擲
		if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
		{
			BasicAttack();
		}

		// 閃避：加速並穿過 Table
		if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K)) && Time.time - lastDashTime >= dashCooldown)
		{
			StartDash();
		}
	}

	void StartDash()
	{
		isDashing = true;
		dashTimer = dashDuration;
		lastDashTime = Time.time;

		// 避免沒有輸入方向造成無法移動
		if (moveInput == Vector2.zero)
			moveInput = Vector2.right;

		PullDownDish();

		// 開始穿越帶有 Table tag 的物件
		Collider2D[] colliders = GameObject.FindObjectsOfType<Collider2D>();
		foreach (var col in colliders)
		{
			foreach (string tag in passThroughTags)
			{
				if (col.CompareTag(tag))
				{
					Physics2D.IgnoreCollision(playerCollider, col, true);
				}
			}
		}


		// TODO: 未來這裡可加上角色閃避無敵（例如0.5秒）
		// StartCoroutine(Invincibility(0.5f));
	}

	void EndDash()
	{
		isDashing = false;

		// 恢復碰撞
		Collider2D[] colliders = GameObject.FindObjectsOfType<Collider2D>();
		foreach (var col in colliders)
		{
			foreach (string tag in passThroughTags)
			{
				if (col.CompareTag(tag))
				{
					Physics2D.IgnoreCollision(playerCollider, col, false);
				}
			}
		}
	}

	// 空函式：撿取物品或使用裝置
	void Interact()
	{
		if (currentFoodTrigger != null)
		{
			Debug.Log("get foood");
			SpriteRenderer foodSprite = currentFoodTrigger.GetComponent<SpriteRenderer>();
			if (foodSprite != null)
			{
				SpriteRenderer handSprite = handItemNow.GetComponent<SpriteRenderer>();
				handSprite.sprite = foodSprite.sprite;
				handItemNow.SetActive(true);
				Debug.Log("撿取物品並顯示在手上");
			}
		}

		PullDownDish();
	}


	// 空函式：普通攻擊或投擲餐點
	void BasicAttack()
	{
		// TODO: 攻擊或投擲行為
		//Debug.Log("觸發攻擊或投擲");
		attackHitBox.SetActive(true);
		StartCoroutine(DisableAttackHitBoxAfterDelay(attackHitBoxDuration));
	}
	// 協程：延遲關閉 attackHitBox
	private IEnumerator DisableAttackHitBoxAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		attackHitBox.SetActive(false);
	}

	private void PullDownDish()
	{
		// 如果有碰到 Table 且有手持物品 → 放下
		if (currentTableCollider != null && handItemNow.activeSelf)
		{
			SpriteRenderer handSprite = handItemNow.GetComponent<SpriteRenderer>();
			tableGroupManager.SetTableItem(currentTableCollider.gameObject, handItemNow);
			Debug.Log($"物品 {handItemNow.GetComponent<SpriteRenderer>().sprite.name} 放回桌 {currentTableCollider.name} 上");

			handSprite.sprite = null;
			handItemNow.SetActive(false);
		}
	}


	void UpdateAnimatorStates()
	{
		if (isDashing)
		{
			animator.SetBool("isDash", true);
			animator.SetBool("isWalk", false);
		}
		else if (moveInput != Vector2.zero)
		{
			animator.SetBool("isWalk", true);
			animator.SetBool("isDash", false);
		}
		else
		{
			animator.SetBool("isWalk", false);
			animator.SetBool("isDash", false);
		}
	}

	void OnTriggerStay2D(Collider2D other)
	{
		if (other.CompareTag("Foods"))
		{
			currentFoodTrigger = other;
		}
	}
	void OnTriggerExit2D(Collider2D other)
	{
		if (other == currentFoodTrigger)
		{
			currentFoodTrigger = null;
		}
	}

	void OnCollisionStay2D(Collision2D collision)
	{
		if (collision.collider.CompareTag("Table"))
		{
			currentTableCollider = collision.collider;
		}
	}
	void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.collider == currentTableCollider)
		{
			currentTableCollider = null;
		}
	}

}
