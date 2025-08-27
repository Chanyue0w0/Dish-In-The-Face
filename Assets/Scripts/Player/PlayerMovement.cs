using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerAttackController;

public class PlayerMovement : MonoBehaviour
{
	[Header("-------- Move Setting ---------")]
	[SerializeField] private bool isSlideAutoPullDish = false;
	[SerializeField] private float moveSpeed = 10f;
	[SerializeField] private float defaultMoveSpeed = 10f;

	[Header("Dash")]
	[SerializeField] private float dashSpeed = 10f;
	[SerializeField] private float dashDistance = 2f;
	[SerializeField] private float dashCooldown = 0.1f;
	[SerializeField] private float dashVirbation = 0.7f;
	[SerializeField] private List<string> passThroughTags;

	// === Slide Interrupt Settings ===
	[Header("Slide Interrupt")]
	[SerializeField] private bool canInterruptSlide = true;           // Can interrupt slide
	[SerializeField, Min(0f)] private float slideInterruptDelay = 0f; // Delay before slide can be interrupted (seconds)

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

	private Rigidbody2D rb;
	private Collider2D playerCollider;
	private PlayerInteraction playerInteraction;
	//private PlayerInput playerInput;
	//private SpriteRenderer spriteRenderer;

	// ======= Animation Manager (Spine) =======
	private PlayerSpineAnimationManager animationManager;

	private Vector2 moveInput;
	private Vector2 moveVelocity;

	private float dashDuration;
	private float lastDashTime = -999f;

	// Slide variables
	private float slideS;
	private int slideDir;
	private bool slideCancelRequested = false; // Slide interrupt requested
	private float slideStartTime;              // Time when slide started

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		//spriteRenderer = GetComponent<SpriteRenderer>();
		playerCollider = GetComponent<Collider2D>();
		//playerInput = GetComponent<PlayerInput>();
		playerInteraction = GetComponent<PlayerInteraction>();
		animationManager = GetComponent<PlayerSpineAnimationManager>();
	}
	void Start()
	{
		moveSpeed = defaultMoveSpeed;
		isDashing = false;
		dashDuration = dashDistance / dashSpeed;
		if (handItemUI) handItemUI.ChangeHandItemUI();
	}

	void Update()
	{
		// Update Spine animation
		animationManager.UpdateFromMovement(moveInput, isDashing, isSlide);
	}

	void FixedUpdate()
	{
		Move();
	}

	// ===== New Input System =====
	public void InputMovement(InputAction.CallbackContext context)
	{
		Vector2 move = context.ReadValue<Vector2>();
		HandleMovementInput(move.x, move.y);
	}
	public void InputAttack(InputAction.CallbackContext context)
	{
		if (context.performed) Attack();
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

	// ===== Actions =====
	void HandleMovementInput(float moveX, float moveY)
	{
		moveInput = new Vector2(moveX, moveY).normalized;

		if (isDashing) moveVelocity = moveInput * dashSpeed;
		else moveVelocity = moveInput * moveSpeed;

		if (moveX != 0)
			transform.rotation = (moveX < 0) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);
	}

	void Move()
	{
		if (isSlide) return; // Handled by Slide() coroutine
		if (!isEnableMoveControll)
		{
			rb.velocity = Vector2.zero;
			return;
		}

		rb.velocity = moveVelocity;
	}
	void Attack()
	{
		if (handItemUI) handItemUI.ChangeHandItemUI();
		attackController.IsAttackSuccess();
	}

	void SwitchWeapon()
	{
		AttackMode mode = attackController.GetAttackMode();
		if (mode == AttackMode.Basic)
			attackController.SetAttackModeUI(AttackMode.Food);
		else if (mode == AttackMode.Food)
			attackController.SetAttackModeUI(AttackMode.Basic);
		else attackController.SetAttackModeUI(AttackMode.Basic);
	}

	void UseItem()
	{
		if (attackController.GetAttackMode() == AttackMode.Food)
		{
			AudioManager.Instance.PlayOneShot(FMODEvents.Instance.playerEatFood, transform.position);
			DestoryFirstItem();
		}

	}
	void StartDash()
	{
		// If sliding, check if you can be interrupted
		if (isSlide)
		{
			if (canInterruptSlide && (Time.time - slideStartTime) >= slideInterruptDelay)
			{
				slideCancelRequested = true;
			}
			// Cannot interrupt or delay not met, ignore dash
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

			// Touching table + dashing => Enter slide
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

	

	/// Table slide coroutine
	private IEnumerator Slide()
	{
		isSlide = true;
		isDashing = false;
		slideCancelRequested = false;     // Clear cancel request
		slideStartTime = Time.time;       // Record slide start time
		RumbleManager.Instance.StopRumble();

		TableConveyorBelt belt = currentTableCollider.GetComponent<TableConveyorBelt>();

		// Ignore collision during slide
		if (belt.BoardCollider && playerCollider)
			Physics2D.IgnoreCollision(playerCollider, belt.BoardCollider, true);

		// Initialize start point and direction
		belt.DecideStartAndDirection(transform.position, out slideS, out slideDir);

		// Snap to start position
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
			// Check if you can interrupt and delay has passed
			if (slideCancelRequested)
			{
				slideCancelRequested = false; // consume the cancel request
				if (canInterruptSlide && (Time.time - slideStartTime) >= slideInterruptDelay)
				{
					// Use current moveInput as eject direction
					bool cancelled = belt.TryCancelSlideAndEject(rb, slideS, slideDir, moveInput);
					if (cancelled)
					{
						// Successfully ejected, stop sliding
						break;
					}
					// Cannot eject (direction mismatch), continue sliding
				}
				// Cannot interrupt or delay not met, continue sliding
			}

			if (isSlideAutoPullDish) playerInteraction.TryPullDownDish();

			// Move along path
			slideS = belt.StepAlong(slideS, slideDir, Time.fixedDeltaTime);
			Vector3 nextPos = belt.EvaluatePositionByDistance(slideS);
			rb.MovePosition(nextPos);

			// Update facing direction
			Vector3 tan = belt.EvaluateTangentByDistance(slideS);
			if (tan.x != 0f)
				transform.rotation = (tan.x < 0) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);

			// Non-loop: auto-stop at endpoints
			if (!belt.Loop && (Mathf.Approximately(slideS, 0f) || Mathf.Approximately(slideS, belt.TotalLength)))
				stop = true;

			yield return waiter;
		}

		// Restore collision
		if (belt.BoardCollider && playerCollider)
			Physics2D.IgnoreCollision(playerCollider, belt.BoardCollider, false);

		isSlide = false;

		// Restore normal movement
		moveVelocity = moveInput * moveSpeed;
	}

	// Move backwards for a distance
	private IEnumerator MoveDistanceCoroutine(float distance, float duration, Vector2 direction)
	{
		SetEnableMoveControll(false);

		// Determine move direction
		if (direction == Vector2.zero)
		{
			direction = transform.rotation.y >= 0 ? new Vector2(-1, 0) : new Vector2(1, 0);
		}

		direction = direction.normalized;
		float elapsed = 0f;

		// Fix initial position to prevent direction changes
		Vector2 start = rb.position;
		Vector2 target = start + direction * distance;

		while (elapsed < duration)
		{
			if (isEnableMoveControll) break;

			// Calculate interpolation (0 to 1)
			float t = elapsed / duration;
			Vector2 nextPos = Vector2.Lerp(start, target, t);
			rb.MovePosition(nextPos);

			elapsed += Time.fixedDeltaTime;
			yield return new WaitForFixedUpdate();
		}

		// Ensure reaches target
		if (!isEnableMoveControll)
			rb.MovePosition(target);

		// Restore control
		SetEnableMoveControll(true);
	}


	// ===== Table collision detection =====
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

	// ===== Public API =====
	public void SetDashSpeed(float newSpeed) { dashSpeed = newSpeed; dashDuration = dashDistance / dashSpeed; }
	public void SetDashDistance(float newDistance) { dashDistance = newDistance; dashDuration = dashDistance / dashSpeed; }
	public void SetDashCooldown(float newCooldown) { dashCooldown = newCooldown; }
	public void SetMoveSpeed(float newMoveSpeed) { moveSpeed = newMoveSpeed; }
	public void SetEnableMoveControll(bool isEnable) { isEnableMoveControll = isEnable; }
	public void DestoryFirstItem()
	{
		if (handItemNow.transform.childCount > 0)
		{
			Destroy(handItemNow.transform.GetChild(0).gameObject);
			handItemUI.ChangeHandItemUI();
		}
	}

	/// Move backwards based on current moveInput direction
	public void MoveDistance(float distance, float duration, Vector2 direction)
	{
		StartCoroutine(MoveDistanceCoroutine(distance, duration, direction));
	}

	public float GetMoveSpeed() => moveSpeed;

	public Vector2 GetMoveInput() => moveInput;
	/// Get slide direction based on tangent y (for external API)
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

	// Check if slide can be interrupted now
	public bool IsSlideInterruptibleNow()
	{
		return isSlide && canInterruptSlide && (Time.time - slideStartTime) >= slideInterruptDelay;
	}
}
