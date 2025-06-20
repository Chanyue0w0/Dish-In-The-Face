using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempPlayerController : MonoBehaviour
{
	[Header("-------- Move Setting ---------")]
	[SerializeField] private float moveSpeed = 5f;

	[Header("Dash")]
	[SerializeField] private float dashSpeed = 10f;           // �{�׮ɪ��t��
	[SerializeField] private float dashDistance = 2f;         // �{�׶Z���]�첾�Z�� = dashSpeed * dashDuration�^
	[SerializeField] private float dashCooldown = 0.1f;       // �{�קN�o�ɶ�
	[SerializeField] private List<string> passThroughTags = new List<string> { "Table", "Enemy" };// �{�׮ɥi��L�� tag


	[Header("Attack")]
	[SerializeField] private float attackHitBoxDuration = 0.1f; // �i�վ㪺�����P�w����ɶ�
	[Header("-------- State ---------")]
	[SerializeField] private bool isDashing = false;


	[Header("-------- Reference ---------")]
	[Header("Script")]
	[SerializeField] private TableGroupManager tableGroupManager;
	[Header("Object")]
	[SerializeField] private GameObject handItemNow; // ���a��W���D�����
	[SerializeField] private GameObject attackHitBox;

	private Collider2D currentFoodTrigger;   // ��e��Ĳ�쪺����Ĳ�o��
	private Collider2D currentTableCollider; // ��e��Ĳ�쪺��l�I����

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

		// �p�����ɶ� = �]�� dashDistance �һݮɶ�
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
		// �ߨ����~�Ψϥ�
		if (Input.GetKeyDown(KeyCode.E))
		{
			Interact();
		}

		// �����Χ��Y
		if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
		{
			BasicAttack();
		}

		// �{�סG�[�t�ì�L Table
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

		// �קK�S����J��V�y���L�k����
		if (moveInput == Vector2.zero)
			moveInput = Vector2.right;

		PullDownDish();

		// �}�l��V�a�� Table tag ������
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


		// TODO: ���ӳo�̥i�[�W����{�׵L�ġ]�Ҧp0.5��^
		// StartCoroutine(Invincibility(0.5f));
	}

	void EndDash()
	{
		isDashing = false;

		// ��_�I��
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

	// �Ũ禡�G�ߨ����~�Ψϥθ˸m
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
				Debug.Log("�ߨ����~����ܦb��W");
			}
		}

		PullDownDish();
	}


	// �Ũ禡�G���q�����Χ��Y�\�I
	void BasicAttack()
	{
		// TODO: �����Χ��Y�欰
		//Debug.Log("Ĳ�o�����Χ��Y");
		attackHitBox.SetActive(true);
		StartCoroutine(DisableAttackHitBoxAfterDelay(attackHitBoxDuration));
	}
	// ��{�G�������� attackHitBox
	private IEnumerator DisableAttackHitBoxAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		attackHitBox.SetActive(false);
	}

	private void PullDownDish()
	{
		// �p�G���I�� Table �B��������~ �� ��U
		if (currentTableCollider != null && handItemNow.activeSelf)
		{
			SpriteRenderer handSprite = handItemNow.GetComponent<SpriteRenderer>();
			tableGroupManager.SetTableItem(currentTableCollider.gameObject, handItemNow);
			Debug.Log($"���~ {handItemNow.GetComponent<SpriteRenderer>().sprite.name} ��^�� {currentTableCollider.name} �W");

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
