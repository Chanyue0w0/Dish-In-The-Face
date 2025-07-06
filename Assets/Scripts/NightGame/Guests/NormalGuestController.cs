using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NormalGuestController : MonoBehaviour
{
	[Header("-------- Setting --------")]
	[SerializeField] private float minPatience = 10f;     // �̵u�@�߬��
	[SerializeField] private float maxPatience = 20f;     // �̪��@�߬��
	[SerializeField] private float eatTime = 10f;         // �Y���һݮɶ�
	[SerializeField] private float moveSpeed = 2f;        // ���ʳt��

	[Header("-------- Stuck Retry Setting --------")]
	[SerializeField] private float stuckCheckInterval = 1.5f; // �X�����S���ʵ����d��
	[SerializeField] private float stuckThreshold = 0.05f;    // �{�w�d���̤p���ʶZ��
	[SerializeField] private float retryDelay = 1f;           // �d��᩵��X��~���s SetDestination

	[Header("-------- Reference --------")]
	[SerializeField] private RoundManager roundManager;
	[SerializeField] private Transform startPosition;             // �_�I��m�]�q�`�O�j���^
	[SerializeField] private GameObject orderIconObject;          // �I�\�ϥ�
	[SerializeField] private SpriteRenderer foodSpriteRenderer;   // ����\�I��
	[SerializeField] private GameObject patienceBar;              // �@�߱�����
	[SerializeField] private Transform barFill;                   // �@�߱��������

	private Transform targetChair;          // �ؼдȤl
	private float patienceTime;             // �@�߬��
	private float timer = 0f;               // �@�߭˼ƭp�ɾ�
	private bool isSeated = false;          // �O�_�w���U
	private bool isEating = false;          // �O�_���b�Y��
	private bool isLeaving = false;         // �O�_���b����
	private bool isRetrying = false;        // �O�_���b���ݭ���
	private Sprite orderFoodSprite = null;  // �I���\�I
	private NavMeshAgent agent;

	// �d������
	private Vector3 lastPosition;
	private float stuckTimer = 0f;

	void Awake()
	{
		// ��l�� NavMeshAgent
		agent = GetComponent<NavMeshAgent>();
		agent.speed = moveSpeed;
		agent.updateRotation = false;
		agent.updateUpAxis = false;

		// �]�w�@�߬��
		patienceTime = Random.Range(minPatience, maxPatience);

		// ��_�I�M�޲z��
		roundManager = GameObject.Find("Rround Manager").GetComponent<RoundManager>();
		startPosition = GameObject.Find("Door Position").transform;

		// ���է�Ȥl
		targetChair = roundManager.chairGroupManager.FindEmptyChair();
		if (targetChair != null)
		{
			agent.SetDestination(targetChair.position);
		}
		else
		{
			Leave(); // �S�Ȥl�N����
		}

		// �����ϥܻP�@�߱�
		orderIconObject.SetActive(false);
		patienceBar.SetActive(false);

		// ��l�Ʀ�m
		lastPosition = transform.position;
	}

	void Update()
	{
		// �p�G�Ȥl��M�Q���ΡA����
		if (!isSeated && targetChair != null && targetChair.childCount > 1)
		{
			roundManager.chairGroupManager.ReleaseChair(targetChair);
			targetChair = null;
			Leave();
		}

		// �p�G��F�Ȥl�A�����I�\�޿�
		if (!isSeated && targetChair != null && !agent.pathPending && agent.remainingDistance <= 0.05f)
		{
			ArriveAtChair();
		}

		// ���U��A�}�l�@�߭˼�
		if (isSeated && !isEating)
		{
			timer += Time.deltaTime;
			float ratio = Mathf.Clamp01(1f - (timer / patienceTime));
			barFill.localScale = new Vector3(ratio, 1f, 1f);

			if (timer >= patienceTime)
			{
				Leave(); // �@�߯Ӻ�
			}
		}

		// �d���ˬd
		CheckStuckAndRetry();

		// ½�ਤ���V
		FlipSpriteByVelocity();
	}

	// ��F�Ȥl�A�i���I�\�P�@�߱��}��
	private void ArriveAtChair()
	{
		transform.position = targetChair.position;
		isSeated = true;
		transform.SetParent(targetChair);

		OrderDish();

		patienceBar.SetActive(true);
		barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	// �]�w�H���\�I�ϥ�
	private void OrderDish()
	{
		orderIconObject.SetActive(true);
		orderFoodSprite = roundManager.foodsGroupManager.OrderFoodRandomly();
		foodSpriteRenderer.sprite = orderFoodSprite;
	}

	// ���դW��A�çP�_�O�_���T
	public bool IsReceiveFood(Sprite foods)
	{
		if (!isSeated || isEating || foods != orderFoodSprite)
			return false;

		isEating = true;
		orderIconObject.SetActive(false);
		patienceBar.SetActive(false);
		roundManager.FinishDishSuccess();
		StopAllCoroutines();
		StartCoroutine(EatAndLeave());
		return true;
	}

	// �Y����۰�����
	private IEnumerator EatAndLeave()
	{
		yield return new WaitForSeconds(eatTime);
		Leave();
	}

	// �����B�z�G����Ȥl�B���V�X�f�çR������
	private void Leave()
	{
		if (isLeaving) return;
		isLeaving = true;

		if (targetChair != null)
		{
			roundManager.chairGroupManager.ReleaseChair(targetChair);
			roundManager.chairGroupManager.ClearChairItem(targetChair);
			targetChair = null;
		}

		// �������^�j���]startPosition�^�@�������I
		Vector3 exitPos = startPosition.position;
		agent.SetDestination(exitPos);
		StartCoroutine(CheckExitReached(exitPos));
	}

	private IEnumerator CheckExitReached(Vector3 exitPos)
	{
		while (Vector3.Distance(transform.position, exitPos) > 0.05f)
		{
			yield return null;
		}
		Destroy(gameObject); // ����f��P�� NPC
	}


	// �ھ� NavMeshAgent velocity.x ½�ਤ�⥪�k��V
	private void FlipSpriteByVelocity()
	{
		if (agent.velocity.x > 0.01f)
			transform.rotation = Quaternion.Euler(0, 0, 0);
		else if (agent.velocity.x < -0.01f)
			transform.rotation = Quaternion.Euler(0, 180, 0);
	}

	// �d�����G����S���ʫh�Ұʩ��𭫸�
	private void CheckStuckAndRetry()
	{
		float moved = Vector3.Distance(transform.position, lastPosition);
		lastPosition = transform.position;

		if (!isSeated && targetChair != null)
		{
			if (moved < stuckThreshold)
			{
				stuckTimer += Time.deltaTime;
				if (stuckTimer >= stuckCheckInterval && !isRetrying)
				{
					isRetrying = true;
					StartCoroutine(RetryPathAfterDelay(retryDelay));
				}
			}
			else
			{
				stuckTimer = 0f;
				isRetrying = false;
			}
		}
	}

	// ���� retryDelay ���A�p�G�٥d��A�N���s�I�s SetDestination
	private IEnumerator RetryPathAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);

		float moved = Vector3.Distance(transform.position, lastPosition);
		lastPosition = transform.position;

		if (!isSeated && !isLeaving && moved < stuckThreshold)
		{
			if (targetChair != null)
			{
				agent.SetDestination(targetChair.position); // ���s���վɯ�
				Debug.Log($"{gameObject.name} �d���ɯ�");
			}
		}

		stuckTimer = 0f;
		isRetrying = false;
	}
}
