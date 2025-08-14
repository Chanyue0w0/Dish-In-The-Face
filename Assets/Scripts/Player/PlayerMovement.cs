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
	[SerializeField] private float dashSpeed = 10f;           // �{�׮ɪ��t��
	[SerializeField] private float dashDistance = 2f;         // �{�׶Z���]�첾�Z�� = dashSpeed * dashDuration�^
	[SerializeField] private float dashCooldown = 0.1f;       // �{�קN�o�ɶ�
	[SerializeField] private float dashVirbation = 0.7f;
	[SerializeField] private List<string> passThroughTags;    // �{�׮ɥi��L�� tag�]�ثe���ϥΡ^
	[Header("Slide")]
	[SerializeField] private float slideSpeed = 5f; // ���O�d�A����e�a�|�� TableConveyorBelt.speed�F���ȥi�@�h���������γ~

	[Header("-------- State ---------")]
	[SerializeField] private bool isDashing = false;
	[SerializeField] private bool isSlide = false;

	[Header("-------- Reference ---------")]
	[Header("Script")]
	[SerializeField] private ChairGroupManager chairGroupManager;
	[SerializeField] private PlayerAttackController attackController;
	[SerializeField] private HandItemUI handItemUI;
	[Header("Object")]
	[SerializeField] private GameObject handItemNow; // ���a��W���D�����

	private Collider2D currentTableCollider; // ��e��Ĳ�쪺�ୱ collider
	private TableConveyorBelt currentBelt;   // ��e�ୱ�W����e�a�]�ѸI�����@�^
	private List<Collider2D> currentChairTriggers = new List<Collider2D>(); // ��e��Ĳ�쪺�Ȥl

	private Rigidbody2D rb;
	private SpriteRenderer spriteRenderer;
	private Collider2D playerCollider;
	private PlayerInput playerInput;
	private PlayerAnimationManager animationManager;

	private Vector2 moveInput;
	private Vector2 moveVelocity;

	// dash ����
	private float dashDuration;
	private float lastDashTime = -999f;

	// slide�]��e�a�M���^�Ϊ��i��
	private float slideS;    // �u���u���Z���Ѽ� s
	private int slideDir;    // +1: �Y�����F-1: �����Y
	private bool slideCancelRequested = false; // �� �b���U���X��

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

		// ���V
		if (moveX != 0)
			transform.rotation = (moveX < 0) ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);
	}

	void Move()
	{
		// �Ʀ椤���мg�����m�A���Ʀ��޿豵�޲���
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
		// �� �Ʀ椤�A���@�� Dash => �b���U��
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

			// �� �Y���ɥ���Ĳ��ୱ + ����e�a�A�אּ�Ұʡu��e�a�Ʀ�v
			if (currentTableCollider != null && currentBelt != null)
			{
				yield return StartCoroutine(Slide(currentBelt)); // �� �ο�e�a�����]�t�b���U���^
				break; // ���� dash
			}

			ShadowPool.instance.GetFormPool();
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

		GameObject currentFood = RoundManager.Instance.foodsGroupManager.GetCurrentDishObject();   // ��e��Ĳ�쪺����
		if (currentFood != null)
		{
			// �M�Ť�W�w�����~
			foreach (Transform child in handItemNow.transform)
				Destroy(child.gameObject);

			// �ͦ��ߨ쪺���~�ê��[�� handItemNow
			for (int i = 0; i < holdItemCount; i++)
			{
				GameObject newItem = Instantiate(currentFood, handItemNow.transform.position, Quaternion.identity);
				newItem.transform.SetParent(handItemNow.transform);
				newItem.GetComponent<Collider2D>().enabled = false; // �����I���קK�z�Z
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
	/// �i��e�a���Ʀ�j�u TableConveyorBelt �Ʀ�
	/// - �A���@�� Dash �i�u�b���U���v
	/// - �D loop ����I�۰ʵ���
	/// - �O�d isSlide �P��k�W�٨ѥ~���P�_
	/// </summary>
	private IEnumerator Slide(TableConveyorBelt belt)
	{
		isSlide = true;
		isDashing = false;
		slideCancelRequested = false; // �� �i�J�Ʀ�ɲM���X��
		RumbleManager.Instance.StopRumble();

		// �Ʀ���������P�ୱ BoardCollider ���I���A�קK�d��
		if (belt.BoardCollider != null && playerCollider != null)
			Physics2D.IgnoreCollision(playerCollider, belt.BoardCollider, true);

		// �̪��a��e��m�M�w�_�I�P��V
		belt.DecideStartAndDirection(transform.position, out slideS, out slideDir);

		// �_�l�l��
		if (belt.SnapOnStart)
		{
			Vector3 snap = belt.EvaluatePositionByDistance(slideS);
			rb.position = snap;
			rb.velocity = Vector2.zero;
			rb.angularVelocity = 0f;
		}

		// �� FixedUpdate �`�����i�A�T�O���z�@�P
		WaitForFixedUpdate waiter = new WaitForFixedUpdate();
		bool stop = false;

		while (!stop)
		{
			// �� �b���U���G�����X�С]�b InputDash ���Q�]�� true�^
			if (slideCancelRequested)
			{
				// �u���k�u�p�T�����קK�d��
				Vector3 left = belt.EvaluateLeftNormalByDistance(slideS);
				rb.MovePosition(rb.position + (Vector2)(left * belt.ExitSideOffset));
				break; // ���X�Ʀ�
			}

			if (isSlideAutoPullDish) PullDownDish();

			// ���i s �ò���
			slideS = belt.StepAlong(slideS, slideDir, Time.fixedDeltaTime);
			Vector3 nextPos = belt.EvaluatePositionByDistance(slideS);
			rb.MovePosition(nextPos);

			// ���⭱�V�]�i��^�G�̤��u��V�վ�
			Vector3 tan = belt.EvaluateTangentByDistance(slideS);
			if (tan.x != 0f)
				transform.rotation = (tan.x < 0) ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);

			// �D loop�G����I�N����
			if (!belt.Loop &&
				(Mathf.Approximately(slideS, 0f) || Mathf.Approximately(slideS, belt.TotalLength)))
			{
				stop = true;
			}

			yield return waiter;
		}

		// ������_�I��
		if (belt.BoardCollider != null && playerCollider != null)
			Physics2D.IgnoreCollision(playerCollider, belt.BoardCollider, false);

		isSlide = false;
		moveVelocity = Vector2.zero;
	}

	// ===== �ȤlĲ�o���@ =====
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

	// ===== �ୱ/��e�a��Ĳ���@�]�覡 A�G����I���^=====
	void OnCollisionStay2D(Collision2D collision)
	{
		if (collision.collider.CompareTag("Table"))
		{
			currentTableCollider = collision.collider;

			// ���եѸ� collider �Ψ��������o TableConveyorBelt
			TableConveyorBelt belt = collision.collider.GetComponent<TableConveyorBelt>();
			if (belt == null) belt = collision.collider.GetComponentInParent<TableConveyorBelt>();

			// �ȷ�Ĳ�쪺 collider ���O�ӱa�l�� BoardCollider �ɡA�~�����i�f��
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

	// ===== ��~ API =====
	// ���}��k�G�]�w dashSpeed
	public void SetDashSpeed(float newSpeed)
	{
		dashSpeed = newSpeed;
		dashDuration = dashDistance / dashSpeed;
	}

	// ���}��k�G�]�w dashDistance
	public void SetDashDistance(float newDistance)
	{
		dashDistance = newDistance;
		dashDuration = dashDistance / dashSpeed;
	}

	// ���}��k�G�]�w dashCooldown
	public void SetDashCooldown(float newCooldown)
	{
		dashCooldown = newCooldown;
	}

	public void DestoryFirstItem()
	{
		if (handItemNow.transform.childCount > 0)
			Destroy(handItemNow.transform.GetChild(0).gameObject);
	}

	/// �^�ǷƦ��V�]�H��e���u y �P�_�W/�U�^
	public int GetSlideDirection()
	{
		if (!isSlide || currentBelt == null) return 0;

		Vector3 t = currentBelt.EvaluateTangentByDistance(slideS);
		if (t.y > 0.1f) return 1;    // ���W��
		if (t.y < -0.1f) return -1;  // ���U��
		return 0;                    // �������
	}

	public bool IsPlayerSlide() => isSlide;
	public bool IsPlayerDash() => isDashing;
}
