using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
	#region ===== Inspector：移動/衝刺設定 =====
	[Header("-------- Move Setting ---------")]
	[SerializeField] private bool isSlideAutoPullDish = false;
	[SerializeField] private float moveSpeed = 10f;
	[SerializeField] private float defaultMoveSpeed = 10f;

	[Header("Dash")]
	[SerializeField] private float dashSpeed = 10f;
	[SerializeField] private float dashDistance = 2f;
	[SerializeField] private float dashCooldown = 0.1f;
	[SerializeField] private float dashVibration = 0.7f;
	[SerializeField] private List<string> passThroughTags;

	[Header("Slide Interrupt")]
	[SerializeField] private bool canInterruptSlide = true;           // 是否允許中斷滑行
	[SerializeField, Min(0f)] private float slideInterruptDelay = 0f; // 開始滑行後，經過幾秒才允許被中斷
	#endregion

	#region ===== Inspector：參考腳本/物件 =====
	[Header("-------- State ---------")]
	[SerializeField] private bool isDashing = false;
	[SerializeField] private bool isSlide = false;
	[SerializeField] private bool isEnableMoveControl = true;

	[Header("-------- Reference ---------")]
	[Header("Script")]
	[SerializeField] private PlayerAttackController attackController;
	[SerializeField] private PlayerSpineAnimationManager animationManager;
	[SerializeField] private HandItemUI handItemUI;

	[Header("Object")]
	[SerializeField] private GameObject handItemNow;
	#endregion

	#region ===== 私有欄位 =====
	private Collider2D currentTableCollider;
	private Rigidbody2D rb;
	private Collider2D playerCollider;
	private PlayerInteraction playerInteraction;

	// 移動相關
	private Vector2 moveInput;
	private Vector2 moveVelocity;
	private float dashDuration;
	private float lastDashTime = -999f;

	// 滑行相關
	private float slideS;
	private int slideDir;
	private bool slideCancelRequested = false;
	private float slideStartTime;
	#endregion

	#region ===== Unity 生命週期 =====
	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		playerCollider = GetComponent<Collider2D>();
		playerInteraction = GetComponent<PlayerInteraction>();
	}

	private void Start()
	{
		moveSpeed = defaultMoveSpeed;
		isDashing = false;
		dashDuration = dashDistance / dashSpeed;

		if (handItemUI) handItemUI.ChangeHandItemUI();
	}

	private void Update()
	{
		// 更新 Spine 動畫
		animationManager.UpdateFromMovement(moveInput, isDashing, isSlide);
	}

	private void FixedUpdate()
	{
		Move();
	}
	#endregion

	#region ===== 新輸入系統：輸入處理 =====
	public void InputMovement(InputAction.CallbackContext context)
	{
		Vector2 move = context.ReadValue<Vector2>();
		HandleMovementInput(move.x, move.y);
	}

	/// <summary> 攻擊鍵：按下→BeginCharge；放開→ReleaseChargeAndAttack（邏輯已搬到 AttackController） </summary>
	public void InputAttack(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			attackController.BeginCharge();
		}
		else if (context.canceled)
		{
			attackController.ReleaseChargeAndAttack();
		}
	}

	public void InputDash(InputAction.CallbackContext context)
	{
		if (context.performed) StartDash();
	}

	public void InputInteract(InputAction.CallbackContext context)
	{
		if (context.performed) playerInteraction.Interact();
	}

	public void InputUseItem(InputAction.CallbackContext context)
	{
		if (context.performed) UseItem();
	}

	public void InputSwitchWeapon(InputAction.CallbackContext context)
	{
		if (context.performed) SwitchWeapon();
	}
	#endregion

	#region ===== 角色移動/旋轉 =====
	private void HandleMovementInput(float moveX, float moveY)
	{
		moveInput = new Vector2(moveX, moveY).normalized;

		if (isDashing) moveVelocity = moveInput * dashSpeed;
		else moveVelocity = moveInput * moveSpeed;

		// X 朝向翻轉（你的美術若相反可調整）
		if (!isSlide && moveX != 0)
			transform.rotation = (moveX < 0) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);
	}

	private void Move()
	{
		if (isSlide) return; // 滑行時由 Slide() 控制位置
		if (!isEnableMoveControl)
		{
			rb.velocity = Vector2.zero;
			return;
		}
		rb.velocity = moveVelocity;
	}
	#endregion

	#region ===== 攻擊（由 AttackController 呼回） =====
	/// <summary>
	/// 由 AttackController.ReleaseChargeAndAttack 結算後呼叫。
	/// 這裡維持你原本流程：先更新手上道具 UI，再把是否為重攻傳給 AttackController.IsAttackSuccess。
	/// </summary>
	public void PerformAttack(bool isPowerAttack)
	{
		if (handItemUI) handItemUI.ChangeHandItemUI();
		attackController.IsAttackSuccess(isPowerAttack);
	}
	#endregion

	#region ===== 切換武器 / 使用物品 =====
	private void SwitchWeapon()
	{
		var mode = attackController.GetAttackMode();
		if (mode == AttackMode.Basic)
			attackController.SetAttackModeUI(AttackMode.Food);
		else if (mode == AttackMode.Food)
			attackController.SetAttackModeUI(AttackMode.Basic);
		else
			attackController.SetAttackModeUI(AttackMode.Basic);
	}

	private void UseItem()
	{
		if (attackController.GetAttackMode() == AttackMode.Food)
		{
			AudioManager.Instance.PlayOneShot(FMODEvents.Instance.playerEatFood, transform.position);
			DestroyFirstItem();
		}
	}
	#endregion

	#region ===== 衝刺/滑行 =====
	private void StartDash()
	{
		// 衝刺前若正在滑行：判斷是否可中斷，否則直接返回
		if (isSlide)
		{
			if (canInterruptSlide && (Time.time - slideStartTime) >= slideInterruptDelay)
				slideCancelRequested = true;
			return;
		}

		// 衝刺開始時取消蓄力（避免邊衝刺邊蓄力）
		attackController.CancelChargeIfAny();

		if (isDashing || Time.time - lastDashTime < dashCooldown) return;
		StartCoroutine(PerformDash());
	}

	private IEnumerator PerformDash()
	{
		isDashing = true;
		lastDashTime = Time.time;
		RumbleManager.Instance.RumbleContinuous(dashVibration, dashVibration);

		float elapsed = 0f;
		while (elapsed < dashDuration)
		{
			moveVelocity = moveInput * dashSpeed;

			// 觸碰桌面 + 衝刺 → 轉為滑行
			if (currentTableCollider != null)
			{
				yield return StartCoroutine(Slide());
				break;
			}

			yield return null;
			elapsed += Time.deltaTime;
		}

		isDashing = false;
		RumbleManager.Instance.StopRumble();
		moveVelocity = moveInput * moveSpeed;
	}

	private IEnumerator Slide()
	{
		isSlide = true;
		isDashing = false;
		slideCancelRequested = false;
		slideStartTime = Time.time;
		RumbleManager.Instance.StopRumble();

		// 滑行開始時取消蓄力（避免 UI 殘留）
		attackController.CancelChargeIfAny();

		TableConveyorBelt belt = currentTableCollider.GetComponent<TableConveyorBelt>();

		// 滑行期間忽略與桌面的碰撞
		if (belt.BoardCollider && playerCollider)
			Physics2D.IgnoreCollision(playerCollider, belt.BoardCollider, true);

		// 初始化起點與方向
		belt.DecideStartAndDirection(transform.position, out slideS, out slideDir);

		// 若需要對齊到路徑起點
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
			// 檢查是否提出中斷請求
			if (slideCancelRequested)
			{
				slideCancelRequested = false; // 消耗請求
				if (canInterruptSlide && (Time.time - slideStartTime) >= slideInterruptDelay)
				{
					// 使用目前移動輸入作為噴出方向
					bool cancelled = belt.TryCancelSlideAndEject(rb, slideS, slideDir, moveInput);
					if (cancelled) break; // 成功噴出，結束滑行
				}
			}

			if (isSlideAutoPullDish) playerInteraction.TryPullDownDish();

			// 沿著路徑移動
			slideS = belt.StepAlong(slideS, slideDir, Time.fixedDeltaTime);
			Vector3 nextPos = belt.EvaluatePositionByDistance(slideS);
			transform.rotation = (nextPos.x >  transform.position.x) ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);
			rb.MovePosition(nextPos);

			// 非循環：到端點自動停止
			if (!belt.Loop && (Mathf.Approximately(slideS, 0f) || Mathf.Approximately(slideS, belt.TotalLength)))
				stop = true;

			yield return waiter;
		}

		// 還原碰撞
		if (belt.BoardCollider && playerCollider)
			Physics2D.IgnoreCollision(playerCollider, belt.BoardCollider, false);

		isSlide = false;

		// 還原一般移動
		moveVelocity = moveInput * moveSpeed;
	}
	#endregion


	#region ===== 碰撞檢測（桌面） =====
	private void OnCollisionStay2D(Collision2D collision)
	{
		if (collision.collider.CompareTag("Table"))
		{
			currentTableCollider = collision.collider;
		}
	}

	private void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.collider == currentTableCollider)
		{
			currentTableCollider = null;
		}
	}
	#endregion

	#region ===== Public API =====
	public void SetDashSpeed(float newSpeed) { dashSpeed = newSpeed; dashDuration = dashDistance / dashSpeed; }
	public void SetDashDistance(float newDistance) { dashDistance = newDistance; dashDuration = dashDistance / dashSpeed; }
	public void SetDashCooldown(float newCooldown) { dashCooldown = newCooldown; }
	public void SetMoveSpeed(float newMoveSpeed) { moveSpeed = newMoveSpeed; }
	public void SetEnableMoveControl(bool isEnable) { isEnableMoveControl = isEnable; }

	public void DestroyFirstItem()
	{
		if (handItemNow != null && handItemNow.transform.childCount > 0)
		{
			Destroy(handItemNow.transform.GetChild(0).gameObject);
			if (handItemUI) handItemUI.ChangeHandItemUI();
		}
	}
	
	
	

	public float GetMoveSpeed() => moveSpeed;
	public void ResetMoveSpeed() => moveSpeed = defaultMoveSpeed;
	public Vector2 GetMoveInput() => moveInput;

	/// <summary> 取得滑行切線方向（y 軸） </summary>
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

	/// <summary> 當下是否可以中斷滑行 </summary>
	public bool IsSlideInterruptibleNow()
	{
		return isSlide && canInterruptSlide && (Time.time - slideStartTime) >= slideInterruptDelay;
	}
	#endregion
}
