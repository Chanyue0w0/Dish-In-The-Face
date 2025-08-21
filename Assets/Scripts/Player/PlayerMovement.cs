using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
	[Header("-------- Move Setting ---------")]
	[SerializeField] private bool isSlideAutoPullDish = false;
	[SerializeField] private float moveSpeed = 5f;
	[SerializeField] private float defaultMoveSpeed = 5f;
	[SerializeField] private int holdItemCount = 10;

	[Header("Dash")]
	[SerializeField] private float dashSpeed = 10f;
	[SerializeField] private float dashDistance = 2f;
	[SerializeField] private float dashCooldown = 0.1f;
	[SerializeField] private float dashVirbation = 0.7f;
	[SerializeField] private List<string> passThroughTags;

	// === 新增：滑行中斷設定 ===
	[Header("Slide Interrupt")]
	[SerializeField] private bool canInterruptSlide = true;           // 是否可中斷滑行
	[SerializeField, Min(0f)] private float slideInterruptDelay = 0f; // 開始滑行後多久才可中斷（秒）

	[Header("-------- State ---------")]
	[SerializeField] private bool isDashing = false;
	[SerializeField] private bool isSlide = false;
	[SerializeField] private bool isEnableMoveControll = true;
	[Header("-------- Reference ---------")]
	[Header("Script")]
	[SerializeField] private PlayerAttackController attackController;
	[SerializeField] private HandItemUI handItemUI;
	[Header("Object")]
	[SerializeField] private GameObject handItemNow;

	private Collider2D currentTableCollider;
	private List<Collider2D> currentChairTriggers = new List<Collider2D>();

	private Rigidbody2D rb;
	private SpriteRenderer spriteRenderer;
	private Collider2D playerCollider;
	private PlayerInput playerInput;

	// ======= 只改這一行：Animator 管理 → Spine 管理 =======
	private PlayerSpineAnimationManager animationManager;

	private Vector2 moveInput;
	private Vector2 moveVelocity;

	private float dashDuration;
	private float lastDashTime = -999f;

	// 輸送帶滑行進度
	private float slideS;
	private int slideDir;
	private bool slideCancelRequested = false; // 滑行中再次按 Dash => 要離開
	private float slideStartTime;              // ★ 新增：紀錄開始滑行的時間

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		playerCollider = GetComponent<Collider2D>();
		playerInput = GetComponent<PlayerInput>();

		// ======= 只改這一行：抓取新版 Spine 動畫管理器 =======
		animationManager = GetComponent<PlayerSpineAnimationManager>();
	}
	void Start()
	{
		isDashing = false;
		dashDuration = dashDistance / dashSpeed;
		if (handItemUI) handItemUI.ChangeHandItemUI();
	}

	void Update()
	{
		// ======= 只改這一行：呼叫新版 Spine 動畫（其餘不動） =======
		if (animationManager != null)
			animationManager.UpdateFromMovement(moveInput, isDashing, isSlide);
	}

	void FixedUpdate()
	{
		Move();
	}

	void HandleMovementInput(float moveX, float moveY)
	{
		moveInput = new Vector2(moveX, moveY).normalized;
		if (!isEnableMoveControll) return;


		if (isDashing) moveVelocity = moveInput * dashSpeed;
		else moveVelocity = moveInput * moveSpeed;

		if (moveX != 0)
			transform.rotation = (moveX < 0) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);
	}

	void Move()
	{
		if (isSlide) return; // 滑行由 Slide() 接管
		rb.velocity = moveVelocity;
	}

	// ===== New Input System =====
	public void InputMovement(InputAction.CallbackContext context)
	{
		Vector2 move = context.ReadValue<Vector2>();
		HandleMovementInput(move.x, move.y);
	}
	public void InputAttack(InputAction.CallbackContext context)
	{
		if (context.started) Attack();
	}
	public void InputDash(InputAction.CallbackContext context)
	{
		if (context.started) StartDash();
	}
	public void InputInteract(InputAction.CallbackContext context)
	{
		if (context.started) Interact();
	}

	// ===== Actions =====
	void Attack()
	{
		if (handItemUI) handItemUI.ChangeHandItemUI();
		attackController.IsAttackSuccess();
	}

	void StartDash()
	{
		// 若正在滑行：依允許與延遲判斷是否能中斷
		if (isSlide)
		{
			if (canInterruptSlide && (Time.time - slideStartTime) >= slideInterruptDelay)
			{
				slideCancelRequested = true;
			}
			// 不允許或尚未到可中斷時間 → 直接忽略這次按鍵
			return;
		}

		if (isDashing || Time.time - lastDashTime < dashCooldown) return;
		StartCoroutine(PerformDash());
	}

	private IEnumerator PerformDash()
	{
		isDashing = true;
		lastDashTime = Time.time;
		RumbleManager.Instance.RumbleContinuous(dashVirbation, dashVirbation);

		float elapsed = 0f;
		while (elapsed < dashDuration)
		{
			moveVelocity = moveInput * dashSpeed;

			// 撞到桌面 + 有輸送帶 => 轉入滑行
			if (currentTableCollider != null)
			{
				yield return StartCoroutine(Slide());
				break;
			}

			//ShadowPool.instance.GetFormPool();
			yield return null;
			elapsed += Time.deltaTime;
		}

		isDashing = false;
		RumbleManager.Instance.StopRumble();
		moveVelocity = moveInput * moveSpeed;
	}

	// 撿取物品或使用裝置
	void Interact()
	{
		if (handItemUI) handItemUI.ChangeHandItemUI();

		GameObject currentFood = RoundManager.Instance.foodsGroupManager.GetCurrentDishObject();
		if (currentFood != null)
		{
			foreach (Transform child in handItemNow.transform)
				Destroy(child.gameObject);

			for (int i = 0; i < holdItemCount; i++)
			{
				GameObject newItem = Instantiate(currentFood, handItemNow.transform.position, Quaternion.identity);
				newItem.transform.SetParent(handItemNow.transform);
				newItem.GetComponent<Collider2D>().enabled = false;
			}
		}
		PullDownDish();
	}

	private void PullDownDish()
	{
		if (currentChairTriggers.Count > 0 && handItemNow.transform.childCount > 0)
		{
			GameObject item = handItemNow.transform.GetChild(0).gameObject;
			foreach (var chair in currentChairTriggers)
				RoundManager.Instance.chairGroupManager.PullDownChairItem(chair.transform, item);
		}
	}

	/// 沿輸送帶滑行（手動下車需符合允許 & 延遲；不取消移動）
	private IEnumerator Slide()
	{
		isSlide = true;
		isDashing = false;
		slideCancelRequested = false;     // 進入滑行先清旗標
		slideStartTime = Time.time;       // ★ 紀錄滑行開始時間
		RumbleManager.Instance.StopRumble();

		TableConveyorBelt belt = currentTableCollider.GetComponent<TableConveyorBelt>();

		// 滑行期間避免卡邊
		if (belt.BoardCollider && playerCollider)
			Physics2D.IgnoreCollision(playerCollider, belt.BoardCollider, true);

		// 初始起點與方向（靠近頭/尾）
		belt.DecideStartAndDirection(transform.position, out slideS, out slideDir);

		// 吸附起點
		if (belt.SnapOnStart)
		{
			Vector3 snap = belt.EvaluatePositionByDistance(slideS);
			rb.position = snap;
			rb.velocity = Vector2.zero;
			rb.angularVelocity = 0f;
		}

		var waiter = new WaitForFixedUpdate();
		bool stop = false;

		while (!stop)
		{
			// ★ 手動下車：需同時符合「允許中斷」與「達到延遲」才會執行
			if (slideCancelRequested)
			{
				slideCancelRequested = false; // consume 一次
				if (canInterruptSlide && (Time.time - slideStartTime) >= slideInterruptDelay)
				{
					// 用當前 moveInput 作為方向鍵，傳入 s 與 slideDir
					bool cancelled = belt.TryCancelSlideAndEject(rb, slideS, slideDir, moveInput);
					if (cancelled)
					{
						// 已經瞬移到桌外，不要再跑滑行，直接收尾
						break;
					}
					// 未取消（方向同向）→ 繼續滑行
				}
				// 不允許或未到時間 → 繼續滑行
			}

			if (isSlideAutoPullDish) PullDownDish();

			// 推進並移動
			slideS = belt.StepAlong(slideS, slideDir, Time.fixedDeltaTime);
			Vector3 nextPos = belt.EvaluatePositionByDistance(slideS);
			rb.MovePosition(nextPos);

			// 面向（可選）
			Vector3 tan = belt.EvaluateTangentByDistance(slideS);
			if (tan.x != 0f)
				transform.rotation = (tan.x < 0) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);

			// 非 loop：端點自動下車
			if (!belt.Loop && (Mathf.Approximately(slideS, 0f) || Mathf.Approximately(slideS, belt.TotalLength)))
				stop = true;

			yield return waiter;
		}

		// 恢復碰撞
		if (belt.BoardCollider && playerCollider)
			Physics2D.IgnoreCollision(playerCollider, belt.BoardCollider, false);

		isSlide = false;

		// 收尾維持玩家當前輸入的移動
		moveVelocity = moveInput * moveSpeed;
	}

	// 持續移動一段距離
	private IEnumerator MoveDistanceCoroutine(float distance, float speed, Vector2 direction)
	{
		if (direction == Vector2.zero) direction = moveInput;

		direction = direction.normalized;
		float duration = distance / speed;
		float elapsed = 0f;

		// 鎖定初始位置，避免移動過程方向被打斷
		Vector2 start = rb.position;
		Vector2 target = start + direction * distance;

		while (elapsed < duration)
		{
			// 線性插值到目標點
			Vector2 nextPos = Vector2.Lerp(start, target, elapsed / duration);
			rb.MovePosition(nextPos);

			elapsed += Time.fixedDeltaTime;
			yield return new WaitForFixedUpdate();
		}
		// 確保最後停在目標位置
		rb.MovePosition(target);
	}

	// ===== 椅子觸發維護 =====
	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Chair"))
		{
			if (handItemNow.transform.childCount > 0)
			{
				GameObject item = handItemNow.transform.GetChild(0).gameObject;
				RoundManager.Instance.chairGroupManager.EnablePullDishSignal(other.transform, item, true);
			}
		}
	}
	void OnTriggerStay2D(Collider2D other)
	{
		if (other.CompareTag("Chair"))
		{
			if (!currentChairTriggers.Contains(other))
				currentChairTriggers.Add(other);
		}
	}
	void OnTriggerExit2D(Collider2D other)
	{
		if (other.CompareTag("Chair"))
		{
			RoundManager.Instance.chairGroupManager.EnablePullDishSignal(other.transform, null, false);
			if (currentChairTriggers.Contains(other))
				currentChairTriggers.Remove(other);
		}
	}

	// ===== 桌面/輸送帶接觸維護（方式 A：實體碰撞）=====
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

	// ===== 對外 API =====
	public void SetDashSpeed(float newSpeed) { dashSpeed = newSpeed; dashDuration = dashDistance / dashSpeed; }
	public void SetDashDistance(float newDistance) { dashDistance = newDistance; dashDuration = dashDistance / dashSpeed; }
	public void SetDashCooldown(float newCooldown) { dashCooldown = newCooldown; }
	public void SetMoveSpeed(float newMoveSpeed) { moveSpeed = newMoveSpeed; }
	public void SetEnableMoveControll(bool isEnable) { isEnableMoveControll = isEnable; }
	public void DestoryFirstItem()
	{
		if (handItemNow.transform.childCount > 0)
			Destroy(handItemNow.transform.GetChild(0).gameObject);
	}

	/// <summary>
	/// 讓玩家朝當前 moveInput 的方向移動一段距離
	/// </summary>
	public void MoveDistance(float distance, float speed, Vector2 direction)
	{
		if (speed <= 0f) speed = moveSpeed;
		StartCoroutine(MoveDistanceCoroutine(distance, speed, direction));
	}

	public float GetMoveSpeed() => moveSpeed;
	/// 以當前切線 y 判斷上/下（維持原有 API）
	public int GetSlideDirection()
	{
		TableConveyorBelt belt = currentTableCollider?.GetComponent<TableConveyorBelt>();
		if (!isSlide || belt == null) return 0;
		Vector3 t = belt.EvaluateTangentByDistance(slideS);
		if (t.y > 0.1f) return 1;
		if (t.y < -0.1f) return -1;
		return 0;
	}

	public bool IsPlayerSlide() => isSlide;
	public bool IsPlayerDash() => isDashing;

	// （可選）對外讀取目前是否可中斷
	public bool IsSlideInterruptibleNow()
	{
		return isSlide && canInterruptSlide && (Time.time - slideStartTime) >= slideInterruptDelay;
	}
}
