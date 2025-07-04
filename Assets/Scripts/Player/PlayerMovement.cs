using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
	[Header("-------- Move Setting ---------")]
	[SerializeField] private float moveSpeed = 5f;
	[SerializeField] private int holdItemCount = 10;
	[Header("Dash")]
	[SerializeField] private float dashSpeed = 10f;           // �{�׮ɪ��t��
	[SerializeField] private float dashDistance = 2f;         // �{�׶Z���]�첾�Z�� = dashSpeed * dashDuration�^
	[SerializeField] private float dashCooldown = 0.1f;       // �{�קN�o�ɶ�
	[SerializeField] private List<string> passThroughTags;    // �{�׮ɥi��L�� tag
	[Header("Slide")]
	[SerializeField] private float slideSpeed = 5f; // ���a�ƹL��l���t�סA�i�վ�


	[Header("-------- State ---------")]
	[SerializeField] private bool isDashing = false;
	[SerializeField] private bool isSlide = false;

	[Header("-------- Reference ---------")]
	[Header("Script")]
	[SerializeField] private ChairGroupManager chairGroupManager;
	[SerializeField] private PlayerAttackController attackController;
	[Header("Object")]
	[SerializeField] private GameObject handItemNow; // ���a��W���D�����

	private Collider2D currentFoodTrigger;   // ��e��Ĳ�쪺����
	private Collider2D currentTableCollider; // ��e��Ĳ�쪺��l
	private Collider2D currentChairTrigger; // ��e��Ĳ�쪺�Ȥl

	private Rigidbody2D rb;
	private SpriteRenderer spriteRenderer;
	private Collider2D playerCollider;
	private Animator animator;
	private PlayerInput playerInput;

	private Vector2 moveInput;
	private Vector2 moveVelocity;

	//private float dashTimer = 0f;
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
		// �p�����ɶ� = �]�� dashDistance �һݮɶ�
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
			//Debug.Log("is dashing");
			moveVelocity = moveInput * dashSpeed;
		}
		else
		{
			moveVelocity = moveInput * moveSpeed;
		}


		if (moveX != 0)
		{
			transform.rotation = (moveX < 0) ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);
		}
	}

	void Move()
	{
		rb.velocity = moveVelocity;
	}

	void InputManager_Old()
	{
		HandleMovementInput(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

		// �ߨ����~�Ψϥ�
		if (Input.GetButtonDown("Interact"))
		{
			Interact();
		}

		// �����Χ��Y
		if (Input.GetButtonDown("Fire1"))
		{
			Attack();
		}

		// �{�סG�[�t�ì�L Table
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
		RumbleManager.Instance.RumbleContinuous(0.4f, 0.4f);

		//// �}�l��V���w�� tag
		//Collider2D[] colliders = GameObject.FindObjectsOfType<Collider2D>();
		//foreach (var col in colliders)
		//{
		//	foreach (string tag in passThroughTags)
		//	{
		//		if (col.CompareTag(tag))
		//		{
		//			Physics2D.IgnoreCollision(playerCollider, col, true);
		//		}
		//	}
		//}


		// Dash ����ɶ����O�� dash �t��
		float elapsed = 0f;
		while (elapsed < dashDuration)
		{
			moveVelocity = moveInput * dashSpeed;

			// �����I�����l
			if (currentTableCollider != null)
			{
				yield return StartCoroutine(Slide(currentTableCollider));
				break; // ���� dash
			}

			ShadowPool.instance.GetFormPool();
			yield return null;
			elapsed += Time.deltaTime;
		}



		//// Dash �����A��_�I��
		//foreach (var col in colliders)
		//{
		//	foreach (string tag in passThroughTags)
		//	{
		//		if (col != null && col.CompareTag(tag))
		//		{
		//			Physics2D.IgnoreCollision(playerCollider, col, false);
		//		}
		//	}
		//}

		isDashing = false;
		RumbleManager.Instance.StopRumble();
		moveVelocity = moveInput * moveSpeed;
	}

	// �ߨ����~�Ψϥθ˸m
	void Interact()
	{
		if (currentFoodTrigger != null)
		{
			//Debug.Log("get foood");

			// �M�Ť�W�w�����~
			foreach (Transform child in handItemNow.transform)
			{
				Destroy(child.gameObject);
			}

			// �ͦ��ߨ쪺���~�ê��[�� handItemNow
			for (int i = 0; i < holdItemCount; i++)
			{
				GameObject newItem = Instantiate(currentFoodTrigger.gameObject, handItemNow.transform.position, Quaternion.identity);
				newItem.transform.SetParent(handItemNow.transform);
				newItem.transform.localScale = new Vector3(0.8f, 0.8f, 1);
				newItem.GetComponent<Collider2D>().enabled = false; // �����I���קK�z�Z
			}
		}


		PullDownDish();
	}


	private void PullDownDish()
	{
		if (currentChairTrigger != null && handItemNow.transform.childCount > 0)
		{
			GameObject item = handItemNow.transform.GetChild(0).gameObject;

			// �ǤJ handItemNow ���l���� Table
			chairGroupManager.PullDownChairItem(currentChairTrigger.transform, item);
		}
	}
	private IEnumerator Slide(Collider2D tableCol)
	{
		isSlide = true; // �������Ʀ檬�A
		isDashing = false; // ���� dash ���A
		RumbleManager.Instance.StopRumble(); // ������_��

		// �����P��l���I���A�קK�d��
		Physics2D.IgnoreCollision(playerCollider, tableCol, true);

		// ���o��l���I���ϰ�]Bounds�^
		Bounds bounds = tableCol.bounds;
		Vector2 start, end;

		// �P�_���a�ثe�b��l�W��ΤU��A�M�w�Ʀ��V
		if (transform.position.y >= bounds.center.y)
		{
			start = new Vector2(bounds.center.x, bounds.max.y);
			end = new Vector2(bounds.center.x, bounds.min.y);
		}
		else
		{
			start = new Vector2(bounds.center.x, bounds.min.y);
			end = new Vector2(bounds.center.x, bounds.max.y);
		}

		// ����Ʀ�ʵe�A�q start �ƨ� end
		float t = 0f;
		while (t < 1f)
		{
			PullDownDish();
			t += Time.deltaTime * slideSpeed;
			transform.position = Vector2.Lerp(start, end, t);
			yield return null;
		}

		// �Ʀ浲�����_��l���I��
		Physics2D.IgnoreCollision(playerCollider, tableCol, false);

		isSlide = false; // �Ʀ檬�A����
		moveVelocity = Vector2.zero; // �����
	}



	void UpdateAnimatorStates()
	{
		if (isSlide)
		{
			animator.SetBool("isSlide", true);
			animator.SetBool("isDash", false);
			animator.SetBool("isWalk", false);
		}
		else if (isDashing)
		{
			animator.SetBool("isDash", true);
			animator.SetBool("isWalk", false);
			animator.SetBool("isSlide", false);
		}
		else if (moveInput != Vector2.zero)
		{
			animator.SetBool("isWalk", true);
			animator.SetBool("isDash", false);
			animator.SetBool("isSlide", false);
		}
		else
		{
			animator.SetBool("isWalk", false);
			animator.SetBool("isDash", false);
			animator.SetBool("isSlide", false);
		}
	}

	void OnTriggerStay2D(Collider2D other)
	{
		if (other.CompareTag("Foods"))
		{
			currentFoodTrigger = other;
		}
		else if (other.CompareTag("Chair"))
		{
			currentChairTrigger = other;
		}
	}
	void OnTriggerExit2D(Collider2D other)
	{
		if (other == currentFoodTrigger)
		{
			currentFoodTrigger = null;
		}
		else if (other == currentChairTrigger)
		{
			currentChairTrigger = null;
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

	// ���}��k�G�]�w dashSpeed
	public void SetDashSpeed(float newSpeed)
	{
		dashSpeed = newSpeed;
		dashDuration = dashDistance / dashSpeed; // ��s����ɶ�
	}

	// ���}��k�G�]�w dashDistance
	public void SetDashDistance(float newDistance)
	{
		dashDistance = newDistance;
		dashDuration = dashDistance / dashSpeed; // ��s����ɶ�
	}

	// ���}��k�G�]�w dashCooldown
	public void SetDashCooldown(float newCooldown)
	{
		dashCooldown = newCooldown;
	}

	public void DestoryFirstItem()
	{
		Destroy(handItemNow.transform.GetChild(0).gameObject);
	}
}
