using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
	[Header("-------- Move Setting ---------")]
	[SerializeField] private bool isSlideAutoPullDish = false;
	[SerializeField] private float moveSpeed = 5f;
	[SerializeField] private int holdItemCount = 10;
	[Header("Dash")]
	[SerializeField] private float dashSpeed = 10f;           // 閃避時的速度
	[SerializeField] private float dashDistance = 2f;         // 閃避距離（位移距離 = dashSpeed * dashDuration）
	[SerializeField] private float dashCooldown = 0.1f;       // 閃避冷卻時間
	[SerializeField] private float dashVirbation = 0.7f;
	[SerializeField] private List<string> passThroughTags;    // 閃避時可穿過的 tag（目前未使用）
	[Header("Slide")]
	[SerializeField] private float slideSpeed = 5f; // 仍保留，但輸送帶會用 TableConveyorBelt.speed；此值可作退場平移等用途

	[Header("-------- State ---------")]
	[SerializeField] private bool isDashing = false;
	[SerializeField] private bool isSlide = false;

	[Header("-------- Reference ---------")]
	[Header("Script")]
	[SerializeField] private ChairGroupManager chairGroupManager;
	[SerializeField] private PlayerAttackController attackController;
	[SerializeField] private HandItemUI handItemUI;
	[Header("Object")]
	[SerializeField] private GameObject handItemNow; // 玩家手上的道具顯示

	private Collider2D currentTableCollider; // 當前接觸到的桌面 collider
	private TableConveyorBelt currentBelt;   // 當前桌面上的輸送帶（由碰撞維護）
	private List<Collider2D> currentChairTriggers = new List<Collider2D>(); // 當前接觸到的椅子

	private Rigidbody2D rb;
	private SpriteRenderer spriteRenderer;
	private Collider2D playerCollider;
	private PlayerInput playerInput;
	private PlayerAnimationManager animationManager;

	private Vector2 moveInput;
	private Vector2 moveVelocity;

	// dash 控制
	private float dashDuration;
	private float lastDashTime = -999f;

	// slide（輸送帶騎乘）用的進度
	private float slideS;    // 沿曲線的距離參數 s
	private int slideDir;    // +1: 頭→尾；-1: 尾→頭
	private bool slideCancelRequested = false; // ★ 半路下車旗標

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		playerCollider = GetComponent<Collider2D>();
		playerInput = GetComponent<PlayerInput>();
		animationManager = GetComponent<PlayerAnimationManager>();
	}
	void Start()
	{
		isDashing = false;
		dashDuration = dashDistance / dashSpeed;
		if (handItemUI) handItemUI.ChangeHandItemUI();
	}

	void Update()
	{
		animationManager.UpdateFromMovement(moveInput, isDashing, isSlide);
	}

	void FixedUpdate()
	{
		Move();
	}

	void HandleMovementInput(float moveX, float moveY)
	{
		moveInput = new Vector2(moveX, moveY).normalized;

		if (isDashing)
			moveVelocity = moveInput * dashSpeed;
		else
			moveVelocity = moveInput * moveSpeed;

		// 面向
		if (moveX != 0)
			transform.rotation = (moveX < 0) ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);
	}

	void Move()
	{
		// 滑行中不覆寫剛體位置，讓滑行邏輯接管移動
		if (isSlide) return;
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
		if (context.started)
			Attack();
	}
	public void InputDash(InputAction.CallbackContext context)
	{
		// ★ 滑行中再按一次 Dash => 半路下車
		if (context.started && isSlide)
		{
			slideCancelRequested = true;
			return;
		}

		if (context.started)
			StartDash();
	}
	public void InputInteract(InputAction.CallbackContext context)
	{
		if (context.started)
			Interact();
	}

	// ===== Actions =====
	void Attack()
	{
		if (handItemUI) handItemUI.ChangeHandItemUI();
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
		RumbleManager.Instance.RumbleContinuous(dashVirbation, dashVirbation);

		float elapsed = 0f;
		while (elapsed < dashDuration)
		{
			moveVelocity = moveInput * dashSpeed;

			// ★ 若此時正接觸到桌面 + 有輸送帶，改為啟動「輸送帶滑行」
			if (currentTableCollider != null && currentBelt != null)
			{
				yield return StartCoroutine(Slide(currentBelt)); // ← 用輸送帶版本（含半路下車）
				break; // 結束 dash
			}

			ShadowPool.instance.GetFormPool();
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

		GameObject currentFood = RoundManager.Instance.foodsGroupManager.GetCurrentDishObject();   // 當前接觸到的食物
		if (currentFood != null)
		{
			// 清空手上已有物品
			foreach (Transform child in handItemNow.transform)
				Destroy(child.gameObject);

			// 生成撿到的物品並附加到 handItemNow
			for (int i = 0; i < holdItemCount; i++)
			{
				GameObject newItem = Instantiate(currentFood, handItemNow.transform.position, Quaternion.identity);
				newItem.transform.SetParent(handItemNow.transform);
				newItem.GetComponent<Collider2D>().enabled = false; // 關閉碰撞避免干擾
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
				chairGroupManager.PullDownChairItem(chair.transform, item);
		}
	}

	/// <summary>
	/// 【輸送帶版滑行】沿 TableConveyorBelt 滑行
	/// - 再按一次 Dash 可「半路下車」
	/// - 非 loop 到端點自動結束
	/// - 保留 isSlide 與方法名稱供外部判斷
	/// </summary>
	private IEnumerator Slide(TableConveyorBelt belt)
	{
		isSlide = true;
		isDashing = false;
		slideCancelRequested = false; // ★ 進入滑行時清除旗標
		RumbleManager.Instance.StopRumble();

		// 滑行期間忽略與桌面 BoardCollider 的碰撞，避免卡邊
		if (belt.BoardCollider != null && playerCollider != null)
			Physics2D.IgnoreCollision(playerCollider, belt.BoardCollider, true);

		// 依玩家當前位置決定起點與方向
		belt.DecideStartAndDirection(transform.position, out slideS, out slideDir);

		// 起始吸附
		if (belt.SnapOnStart)
		{
			Vector3 snap = belt.EvaluatePositionByDistance(slideS);
			rb.position = snap;
			rb.velocity = Vector2.zero;
			rb.angularVelocity = 0f;
		}

		// 用 FixedUpdate 節奏推進，確保物理一致
		WaitForFixedUpdate waiter = new WaitForFixedUpdate();
		bool stop = false;

		while (!stop)
		{
			// ★ 半路下車：偵測旗標（在 InputDash 中被設為 true）
			if (slideCancelRequested)
			{
				// 沿左法線小幅側移避免卡邊
				Vector3 left = belt.EvaluateLeftNormalByDistance(slideS);
				rb.MovePosition(rb.position + (Vector2)(left * belt.ExitSideOffset));
				break; // 跳出滑行
			}

			if (isSlideAutoPullDish) PullDownDish();

			// 推進 s 並移動
			slideS = belt.StepAlong(slideS, slideDir, Time.fixedDeltaTime);
			Vector3 nextPos = belt.EvaluatePositionByDistance(slideS);
			rb.MovePosition(nextPos);

			// 角色面向（可選）：依切線方向調整
			Vector3 tan = belt.EvaluateTangentByDistance(slideS);
			if (tan.x != 0f)
				transform.rotation = (tan.x < 0) ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);

			// 非 loop：到端點就結束
			if (!belt.Loop &&
				(Mathf.Approximately(slideS, 0f) || Mathf.Approximately(slideS, belt.TotalLength)))
			{
				stop = true;
			}

			yield return waiter;
		}

		// 結束恢復碰撞
		if (belt.BoardCollider != null && playerCollider != null)
			Physics2D.IgnoreCollision(playerCollider, belt.BoardCollider, false);

		isSlide = false;
		moveVelocity = Vector2.zero;
	}

	// ===== 椅子觸發維護 =====
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
		if (currentChairTriggers.Contains(other))
			currentChairTriggers.Remove(other);
	}

	// ===== 桌面/輸送帶接觸維護（方式 A：實體碰撞）=====
	void OnCollisionStay2D(Collision2D collision)
	{
		if (collision.collider.CompareTag("Table"))
		{
			currentTableCollider = collision.collider;

			// 嘗試由該 collider 或其父物件取得 TableConveyorBelt
			TableConveyorBelt belt = collision.collider.GetComponent<TableConveyorBelt>();
			if (belt == null) belt = collision.collider.GetComponentInParent<TableConveyorBelt>();

			// 僅當接觸到的 collider 正是該帶子的 BoardCollider 時，才視為可搭乘
			if (belt != null && belt.BoardCollider == collision.collider)
				currentBelt = belt;
			else
				currentBelt = null;
		}
	}
	void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.collider == currentTableCollider)
		{
			currentTableCollider = null;
			currentBelt = null;
		}
	}

	// ===== 對外 API =====
	// 公開方法：設定 dashSpeed
	public void SetDashSpeed(float newSpeed)
	{
		dashSpeed = newSpeed;
		dashDuration = dashDistance / dashSpeed;
	}

	// 公開方法：設定 dashDistance
	public void SetDashDistance(float newDistance)
	{
		dashDistance = newDistance;
		dashDuration = dashDistance / dashSpeed;
	}

	// 公開方法：設定 dashCooldown
	public void SetDashCooldown(float newCooldown)
	{
		dashCooldown = newCooldown;
	}

	public void DestoryFirstItem()
	{
		if (handItemNow.transform.childCount > 0)
			Destroy(handItemNow.transform.GetChild(0).gameObject);
	}

	/// 回傳滑行方向（以當前切線 y 判斷上/下）
	public int GetSlideDirection()
	{
		if (!isSlide || currentBelt == null) return 0;

		Vector3 t = currentBelt.EvaluateTangentByDistance(slideS);
		if (t.y > 0.1f) return 1;    // 往上滑
		if (t.y < -0.1f) return -1;  // 往下滑
		return 0;                    // 接近水平
	}

	public bool IsPlayerSlide() => isSlide;
	public bool IsPlayerDash() => isDashing;
}
