using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
	[Header("-------- Move Setting ---------")]
	[SerializeField] private float moveSpeed = 5f;

	[Header("Dash")]
	[SerializeField] private float dashSpeed = 10f;           // 閃避時的速度
	[SerializeField] private float dashDistance = 2f;         // 閃避距離（位移距離 = dashSpeed * dashDuration）
	[SerializeField] private float dashCooldown = 0.1f;       // 閃避冷卻時間
	[SerializeField] private List<string> passThroughTags;    // 閃避時可穿過的 tag


	[Header("-------- State ---------")]
	[SerializeField] private bool isDashing = false;


	[Header("-------- Reference ---------")]
	[Header("Script")]
	[SerializeField] private TableGroupManager tableGroupManager;
	[SerializeField] private PlayerAttackController attackController;
	[Header("Object")]
	[SerializeField] private GameObject handItemNow; // 玩家手上的道具顯示

	private Collider2D currentFoodTrigger;   // 當前接觸到的食物觸發器
	private Collider2D currentTableCollider; // 當前接觸到的桌子碰撞器

	private Rigidbody2D rb;
	private SpriteRenderer spriteRenderer;
	private Collider2D playerCollider;
	private Animator animator;
	private PlayerInput playerInput;

	private Vector2 moveInput;
	private Vector2 moveVelocity;

	private float dashTimer = 0f;
	private float dashDuration;
	private float lastDashTime = -999f;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		playerCollider = GetComponent<Collider2D>();
		animator = GetComponent<Animator>();
		playerInput = GetComponent<PlayerInput>();
	}
	void Start()
	{
		isDashing = false;
		// 計算持續時間 = 跑完 dashDistance 所需時間
		dashDuration = dashDistance / dashSpeed;

	}

	void Update()
	{
		//InputManager_Old();


		UpdateAnimatorStates();
	}

	void FixedUpdate()
	{
		Move();
	}

	void HandleMovementInput(float moveX, float moveY)
	{
		//Debug.Log(moveX.ToString() + " " + moveY.ToString());
		moveInput = new Vector2(moveX, moveY).normalized;

		if (isDashing)
		{
			Debug.Log("is dashing");
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

	void InputManager_Old()
	{
		HandleMovementInput(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

		// 撿取物品或使用
		if (Input.GetButtonDown("Interact"))
		{
			Interact();
		}

		// 攻擊或投擲
		if (Input.GetButtonDown("Fire1"))
		{
			Attack();
		}

		// 閃避：加速並穿過 Table
		if ((Input.GetAxis("Dash") > 0f || Input.GetButtonDown("Dash")))
		{
			StartDash();
		}
	}

	public void InputMovement(InputAction.CallbackContext context)
	{
		//Debug.Log(context);
		Vector2 move = context.ReadValue<Vector2>();
		HandleMovementInput(move.x, move.y);
	}
	public void InputAttack(InputAction.CallbackContext context)
	{
		//Debug.Log(context);
		if (context.started)
			Attack();
	}
	public void InputDash(InputAction.CallbackContext context)
	{
		//Debug.Log(context);
		if (context.started)
			StartDash();
	}
	public void InputInteract(InputAction.CallbackContext context)
	{
		//Debug.Log(context);
		if (context.started)
			Interact();
	}
	void Attack()
	{
		attackController.IsAttackSuccess();
	}
	void StartDash()
	{
		if (isDashing || Time.time - lastDashTime < dashCooldown) return;

		StartCoroutine(PerformDash());
	}

	private IEnumerator PerformDash()
	{
		isDashing = true;
		lastDashTime = Time.time;

		// 開始穿越指定的 tag
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
		RumbleManager.Instance.RumbleContinuous(0.4f, 0.4f);


		// Dash 持續時間內保持 dash 速度
		float elapsed = 0f;
		while (elapsed < dashDuration)
		{
			// 期間可放下餐點
			PullDownDish();

			moveVelocity = moveInput * dashSpeed;

			ShadowPool.instance.GetFormPool();
			yield return null;
			elapsed += Time.deltaTime;
		}

		// Dash 結束，恢復碰撞
		foreach (var col in colliders)
		{
			foreach (string tag in passThroughTags)
			{
				if (col != null && col.CompareTag(tag))
				{
					Physics2D.IgnoreCollision(playerCollider, col, false);
				}
			}
		}

		isDashing = false;
		RumbleManager.Instance.StopRumble();
		moveVelocity = moveInput * moveSpeed;
	}

	// 撿取物品或使用裝置
	void Interact()
	{
		if (currentFoodTrigger != null)
		{
			//Debug.Log("get foood");

			// 清空手上已有物品
			foreach (Transform child in handItemNow.transform)
			{
				Destroy(child.gameObject);
			}

			// 生成撿到的物品並附加到 handItemNow
			GameObject newItem = Instantiate(currentFoodTrigger.gameObject, handItemNow.transform.position, Quaternion.identity);
			newItem.transform.SetParent(handItemNow.transform);
			newItem.transform.localScale = new Vector3(0.8f, 0.8f, 1);
			newItem.GetComponent<Collider2D>().enabled = false; // 關閉碰撞避免干擾
		}


		PullDownDish();
	}


	private void PullDownDish()
	{
		if (currentTableCollider != null && handItemNow.transform.childCount > 0)
		{
			GameObject item = handItemNow.transform.GetChild(0).gameObject;

			// 傳入 handItemNow 的子物件給 Table
			tableGroupManager.PullDownTableItem(currentTableCollider.gameObject, item);
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

	// 公開方法：設定 dashSpeed
	public void SetDashSpeed(float newSpeed)
	{
		dashSpeed = newSpeed;
		dashDuration = dashDistance / dashSpeed; // 更新持續時間
	}

	// 公開方法：設定 dashDistance
	public void SetDashDistance(float newDistance)
	{
		dashDistance = newDistance;
		dashDuration = dashDistance / dashSpeed; // 更新持續時間
	}

	// 公開方法：設定 dashCooldown
	public void SetDashCooldown(float newCooldown)
	{
		dashCooldown = newCooldown;
	}

}
