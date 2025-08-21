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

	// === �s�W�G�Ʀ椤�_�]�w ===
	[Header("Slide Interrupt")]
	[SerializeField] private bool canInterruptSlide = true;           // �O�_�i���_�Ʀ�
	[SerializeField, Min(0f)] private float slideInterruptDelay = 0f; // �}�l�Ʀ��h�[�~�i���_�]��^

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

	// ======= �u��o�@��GAnimator �޲z �� Spine �޲z =======
	private PlayerSpineAnimationManager animationManager;

	private Vector2 moveInput;
	private Vector2 moveVelocity;

	private float dashDuration;
	private float lastDashTime = -999f;

	// ��e�a�Ʀ�i��
	private float slideS;
	private int slideDir;
	private bool slideCancelRequested = false; // �Ʀ椤�A���� Dash => �n���}
	private float slideStartTime;              // �� �s�W�G�����}�l�Ʀ檺�ɶ�

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		playerCollider = GetComponent<Collider2D>();
		playerInput = GetComponent<PlayerInput>();

		// ======= �u��o�@��G����s�� Spine �ʵe�޲z�� =======
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
		// ======= �u��o�@��G�I�s�s�� Spine �ʵe�]��l���ʡ^ =======
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
		if (isSlide) return; // �Ʀ�� Slide() ����
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
		// �Y���b�Ʀ�G�̤��\�P����P�_�O�_�त�_
		if (isSlide)
		{
			if (canInterruptSlide && (Time.time - slideStartTime) >= slideInterruptDelay)
			{
				slideCancelRequested = true;
			}
			// �����\�Ω|����i���_�ɶ� �� ���������o������
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

			// ����ୱ + ����e�a => ��J�Ʀ�
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

	// �ߨ����~�Ψϥθ˸m
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

	/// �u��e�a�Ʀ�]��ʤU���ݲŦX���\ & ����F���������ʡ^
	private IEnumerator Slide()
	{
		isSlide = true;
		isDashing = false;
		slideCancelRequested = false;     // �i�J�Ʀ���M�X��
		slideStartTime = Time.time;       // �� �����Ʀ�}�l�ɶ�
		RumbleManager.Instance.StopRumble();

		TableConveyorBelt belt = currentTableCollider.GetComponent<TableConveyorBelt>();

		// �Ʀ�����קK�d��
		if (belt.BoardCollider && playerCollider)
			Physics2D.IgnoreCollision(playerCollider, belt.BoardCollider, true);

		// ��l�_�I�P��V�]�a���Y/���^
		belt.DecideStartAndDirection(transform.position, out slideS, out slideDir);

		// �l���_�I
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
			// �� ��ʤU���G�ݦP�ɲŦX�u���\���_�v�P�u�F�쩵��v�~�|����
			if (slideCancelRequested)
			{
				slideCancelRequested = false; // consume �@��
				if (canInterruptSlide && (Time.time - slideStartTime) >= slideInterruptDelay)
				{
					// �η�e moveInput �@����V��A�ǤJ s �P slideDir
					bool cancelled = belt.TryCancelSlideAndEject(rb, slideS, slideDir, moveInput);
					if (cancelled)
					{
						// �w�g�������~�A���n�A�]�Ʀ�A��������
						break;
					}
					// �������]��V�P�V�^�� �~��Ʀ�
				}
				// �����\�Υ���ɶ� �� �~��Ʀ�
			}

			if (isSlideAutoPullDish) PullDownDish();

			// ���i�ò���
			slideS = belt.StepAlong(slideS, slideDir, Time.fixedDeltaTime);
			Vector3 nextPos = belt.EvaluatePositionByDistance(slideS);
			rb.MovePosition(nextPos);

			// ���V�]�i��^
			Vector3 tan = belt.EvaluateTangentByDistance(slideS);
			if (tan.x != 0f)
				transform.rotation = (tan.x < 0) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);

			// �D loop�G���I�۰ʤU��
			if (!belt.Loop && (Mathf.Approximately(slideS, 0f) || Mathf.Approximately(slideS, belt.TotalLength)))
				stop = true;

			yield return waiter;
		}

		// ��_�I��
		if (belt.BoardCollider && playerCollider)
			Physics2D.IgnoreCollision(playerCollider, belt.BoardCollider, false);

		isSlide = false;

		// �����������a��e��J������
		moveVelocity = moveInput * moveSpeed;
	}

	// ���򲾰ʤ@�q�Z��
	private IEnumerator MoveDistanceCoroutine(float distance, float speed, Vector2 direction)
	{
		if (direction == Vector2.zero) direction = moveInput;

		direction = direction.normalized;
		float duration = distance / speed;
		float elapsed = 0f;

		// ��w��l��m�A�קK���ʹL�{��V�Q���_
		Vector2 start = rb.position;
		Vector2 target = start + direction * distance;

		while (elapsed < duration)
		{
			// �u�ʴ��Ȩ�ؼ��I
			Vector2 nextPos = Vector2.Lerp(start, target, elapsed / duration);
			rb.MovePosition(nextPos);

			elapsed += Time.fixedDeltaTime;
			yield return new WaitForFixedUpdate();
		}
		// �T�O�̫ᰱ�b�ؼЦ�m
		rb.MovePosition(target);
	}

	// ===== �ȤlĲ�o���@ =====
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

	// ===== �ୱ/��e�a��Ĳ���@�]�覡 A�G����I���^=====
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

	// ===== ��~ API =====
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
	/// �����a�·�e moveInput ����V���ʤ@�q�Z��
	/// </summary>
	public void MoveDistance(float distance, float speed, Vector2 direction)
	{
		if (speed <= 0f) speed = moveSpeed;
		StartCoroutine(MoveDistanceCoroutine(distance, speed, direction));
	}

	public float GetMoveSpeed() => moveSpeed;
	/// �H��e���u y �P�_�W/�U�]�����즳 API�^
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

	// �]�i��^��~Ū���ثe�O�_�i���_
	public bool IsSlideInterruptibleNow()
	{
		return isSlide && canInterruptSlide && (Time.time - slideStartTime) >= slideInterruptDelay;
	}
}
