using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NormalGuestController : MonoBehaviour
{
	#region Inspector

	[Header("-------- Base Setting --------")]
	[SerializeField] private float moveSpeed = 2f;

	[Header("-------- Patience / Timing --------")]
	[Tooltip("階段1：思考點餐時間（秒）")]
	[SerializeField] private float thinkOrderTime = 10f;

	[Tooltip("階段2：等待玩家來點餐的耐心時間（秒）")]
	[SerializeField] private float maxOrderPatience = 20f;

	[Tooltip("階段3：等待餐點送達耐心（秒）")]
	[SerializeField] private float maxDishPatience = 25f;

	[Header("-------- Flow Setting --------")]
	[SerializeField] private float stateTransitionDelay = 0.3f; // ���A��������]���^

	[Header("-------- Eat / Reorder --------")]
	[Tooltip("�Y���ɶ��]���^")]
	[SerializeField] private float eatTime = 10f;

	[Tooltip("吃完後，再點餐的機率（0~1）")]
	[SerializeField, Range(0f, 1f)] private float reorderProbability = 0.3f;

	[Header("-------- Stuck Retry Setting --------")]
	[SerializeField] private float stuckCheckInterval = 1.5f;
	[SerializeField] private float stuckThreshold = 0.05f;
	[SerializeField] private float retryDelay = 2f;

	[Header("-------- Appearance --------")]
	[SerializeField] private List<Sprite> guestAppearanceList = new List<Sprite>();

	[Header("-------- Reference --------")]
	[SerializeField] private Transform startPosition;
	[SerializeField] private Transform endPosition;
	[SerializeField] private Transform barFill;
	[SerializeField] private GameObject patienceBar;
	[SerializeField] private GameObject chatBoxIconObj;
	[SerializeField] private GameObject rawBtnIconObj;
	[SerializeField] private GameObject questionIconObj;
	[SerializeField] private SpriteRenderer foodSpriteRenderer;
	[SerializeField] private SpriteRenderer guestSpriteRenderer;

	#endregion

	#region Runtime State

	private enum GuestState
	{
		WalkingToChair,
		Thinking,      // �B�J1�G����I�\
		WaitingOrder,  // �B�J2�G���ݪ��a���I�\�]���U���ʽT�{�^
		WaitingDish,   // �B�J3�G�����\�I�]�T�w�@�ߡ^
		Eating,
		Leaving
	}

	private GuestState state = GuestState.WalkingToChair;

	private Transform targetChair;

	// ���N�� timer ���T�ӳѾl�ɶ��ܼ�
	private float thinkTimeLeft = 0f;
	private float orderPatienceLeft = 0f;
	private float dishPatienceLeft = 0f;

	private bool isSeated;
	private bool isRetrying;
	private bool isLeaving;

	private Sprite orderFoodSprite = null;
	private NavMeshAgent agent;

	private Vector3 lastPosition;
	private float stuckTimer;

	// �����
	private GuestPoolHandler poolHandler;

	#endregion

	#region Unity Lifecycle

	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		agent.speed = moveSpeed;
		agent.updateRotation = false;
		agent.updateUpAxis = false;

		poolHandler = GetComponent<GuestPoolHandler>();
	}

	private void Start()
	{
		// �_�I�P�X�f
		if (RoundManager.Instance)
		{
			startPosition = RoundManager.Instance.guestGroupManager.enterPoistion;
			endPosition = RoundManager.Instance.guestGroupManager.exitPoistion;
			targetChair = RoundManager.Instance.chairGroupManager.FindEmptyChair(this);
		}
	}

	private void OnEnable()
	{
		// �H���~�[
		if (guestAppearanceList != null && guestAppearanceList.Count > 0)
		{
			int idx = Random.Range(0, guestAppearanceList.Count);
			guestSpriteRenderer.sprite = guestAppearanceList[idx];
		}

		// ���m���A
		isSeated = false;
		isLeaving = false;
		isRetrying = false;
		stuckTimer = 0f;
		state = GuestState.WalkingToChair;

		// ���m�U�Ѿl�ɶ�
		thinkTimeLeft = 0f;
		orderPatienceLeft = 0f;
		dishPatienceLeft = 0f;

		chatBoxIconObj.SetActive(false);
		rawBtnIconObj.SetActive(false);
		questionIconObj.SetActive(false);
		patienceBar.SetActive(false);
		if (barFill != null) barFill.localScale = new Vector3(1f, 1f, 1f);
		foodSpriteRenderer.sprite = null;

		// �_�I�P�X�f
		if (RoundManager.Instance)
		{
			startPosition = RoundManager.Instance.guestGroupManager.enterPoistion;
			endPosition = RoundManager.Instance.guestGroupManager.exitPoistion;
			// ��y��
			targetChair = RoundManager.Instance.chairGroupManager.FindEmptyChair(this);
		}

		if (targetChair != null)
			agent.SetDestination(targetChair.position);
		else
			Leave();
		
		lastPosition = transform.position;
	}

	private void Update()
	{
		// �Ȥl�Q�m���N���}
		if (!isSeated && targetChair != null && targetChair.childCount > 1)
		{
			RoundManager.Instance.chairGroupManager.ReleaseChair(targetChair);
			targetChair = null;
			Leave();
		}

		// ��F�y��
		if (state == GuestState.WalkingToChair && targetChair != null && !agent.pathPending && agent.remainingDistance <= 0.05f)
		{
			ArriveAtChair();
		}

		// ���A�X�ʪ��p�ɻP UI
		TickStateTimers();

		CheckStuckAndRetry();
		FlipSpriteByVelocity();
	}

	#endregion

	#region Flow States

	private void ArriveAtChair()
	{
		transform.position = targetChair.position;
		isSeated = true;
		transform.SetParent(targetChair);

		// �i�J�u����I�\�v
		EnterThinking();
	}

	private void EnterThinking()
	{
		state = GuestState.Thinking;

		chatBoxIconObj.SetActive(false);
		patienceBar.SetActive(false);
		foodSpriteRenderer.sprite = null; // ��Үɤ���ܭ���

		// ��ҧ��u�M�w�v�\�I�A���|�������a�ݨ�F�����a�Ӥ��ʤ~��ܭq��ϥ�
		orderFoodSprite = RoundManager.Instance.foodsGroupManager.OrderFoodRandomly();
		thinkTimeLeft = thinkOrderTime;

		if (barFill != null) barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	private void EnterWaitingOrder()
	{
		state = GuestState.WaitingOrder;

		// ��ܪ��a�i���ʤ����ܡ]�Ҧp X/Raw ����^
		rawBtnIconObj.SetActive(false);
		chatBoxIconObj.SetActive(true);
		questionIconObj.SetActive(true);// �ݸ��i���ʡ]�T�{�I��^

		// �άۦP�@�߭� UI
		patienceBar.SetActive(true);
		orderPatienceLeft = maxOrderPatience;
		if (barFill != null) barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	private void EnterWaitingDish()
	{
		state = GuestState.WaitingDish;

		rawBtnIconObj.SetActive(false);
		chatBoxIconObj.SetActive(true);    // ����\�I�T����
		questionIconObj.SetActive(false);

		// �b��ܮؤW�i���\�I��
		foodSpriteRenderer.sprite = orderFoodSprite;
		RoundManager.Instance.chairGroupManager.AddOrderGuest(this);

		// �T�w�@�߮ɶ�
		patienceBar.SetActive(true);
		dishPatienceLeft = maxDishPatience;
		if (barFill != null) barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	private void EnterEating()
	{
		state = GuestState.Eating;

		chatBoxIconObj.SetActive(false);
		patienceBar.SetActive(false);

		// �� RoundManager �Ұʨ�{�A�קK Inactive �������
		RoundManager.Instance.StartCoroutine(EatAndThenDecide());
	}

	private IEnumerator EatAndThenDecide()
	{
		yield return new WaitForSeconds(eatTime);

		// ���b�]�@���\�I�^
		RoundManager.Instance.FinishDishSuccess(targetChair, 10);

		// �O�_�A�I�@���H
		if (Random.value < reorderProbability)
		{
			// �^���� �� ���ݪ��a�I�\ �� �����\�I
			EnterThinking();
		}
		else
		{
			Leave();
		}
	}

	#endregion

	#region Public Interactions

	/// <summary>
	/// �Ѫ��a�I�s�G�T�{�ȤH���q��C
	/// �Ȧb WaitingOrder ���A���ġC���\�|���� WaitingDish �ö}�l�T�w�@�߭˼ơC
	/// </summary>
	/// <returns>�O�_�T�{���\</returns>
	public bool ConfirmOrderByPlayer()
	{
		if (!isSeated || state != GuestState.WaitingOrder)
			return false;

		Invoke(nameof(EnterWaitingDish), stateTransitionDelay);
		return true;
	}

	/// <summary>
	/// ���a�e�\�C�Ȧb WaitingDish ���A�B�\�I���T�ɦ��ߡA�öi�J Eating�C
	/// </summary>
	public bool IsReceiveFood(Sprite foods)
	{
		if (!isSeated || state != GuestState.WaitingDish || foods != orderFoodSprite)
			return false;

		RoundManager.Instance.chairGroupManager.RemovOrderGuest(this);
		EnterEating();
		return true;
	}

	/// <summary>
	/// WaitingOrder������ܡC�b WaitingDish ���q�A�Y���a��W�O���T�\�I�~��ܤ��ʴ��ܡC
	/// </summary>
	public void EnableInteractIcon(Sprite food, bool isEnable)
	{
		rawBtnIconObj.SetActive(isEnable);
		// �Y�O���ݪ��a���I�\ rawBtn ���
		if (state == GuestState.WaitingOrder) return;
		// �Ȧb�����\�I�B�B�������T����ܥi��I����
		if (isSeated && state == GuestState.WaitingDish && food == orderFoodSprite) return;
		
		
		rawBtnIconObj.SetActive(false);
	}

	#endregion

	#region Utilities

	private void TickStateTimers()
	{
		switch (state)
		{
			case GuestState.Thinking:
				thinkTimeLeft -= Time.deltaTime;
				if (thinkTimeLeft <= 0f)
				{
					// ��ҧ����A�i�J�u���ݪ��a���I�\�v
					EnterWaitingOrder();
				}
				break;

			case GuestState.WaitingOrder:
				orderPatienceLeft -= Time.deltaTime;
				UpdatePatienceBar(orderPatienceLeft, maxOrderPatience);

				if (orderPatienceLeft <= 0f)
				{
					BecomeTroubleGuest();
				}
				break;

			case GuestState.WaitingDish:
				dishPatienceLeft -= Time.deltaTime;
				UpdatePatienceBar(dishPatienceLeft, maxDishPatience);

				if (dishPatienceLeft <= 0f)
				{
					BecomeTroubleGuest();
				}
				break;

			case GuestState.WalkingToChair:
			case GuestState.Eating:
			case GuestState.Leaving:
			default:
				// no-op
				break;
		}
	}

	private void UpdatePatienceBar(float remain, float total)
	{
		if (!patienceBar.activeSelf) patienceBar.SetActive(true);
		float ratio = Mathf.Clamp01(Mathf.Max(remain, 0f) / Mathf.Max(total, 0.0001f));
		if (barFill != null) barFill.localScale = new Vector3(ratio, 1f, 1f);
	}

	private void Leave()
	{
		if (isLeaving) return;
		isLeaving = true;
		state = GuestState.Leaving;

		chatBoxIconObj.SetActive(false);

		if (targetChair != null)
		{
			transform.SetParent(RoundManager.Instance.guestGroupManager.transform);
			RoundManager.Instance.chairGroupManager.ReleaseChair(targetChair);
			RoundManager.Instance.chairGroupManager.ClearChairItem(targetChair);
			targetChair = null;
		}

		Vector3 exitPos = endPosition.position;
		agent.SetDestination(exitPos);

		RoundManager.Instance.StartCoroutine(CheckExitReached(exitPos));
	}

	private IEnumerator CheckExitReached(Vector3 exitPos)
	{
		float waitTime = 0f;
		const float timeout = 25f;

		while (Vector2.Distance(transform.position, exitPos) > 2f && waitTime < timeout)
		{
			if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
				break;

			waitTime += Time.deltaTime;
			yield return null;
		}

		// �^���쪫���
		if (poolHandler != null) poolHandler.Release();
		else gameObject.SetActive(false);
	}

	private void FlipSpriteByVelocity()
	{
		if (agent.velocity.x > 0.01f)
			transform.rotation = Quaternion.Euler(0, 0, 0);
		else if (agent.velocity.x < -0.01f)
			transform.rotation = Quaternion.Euler(0, 180, 0);
	}

	private void CheckStuckAndRetry()
	{
		float moved = Vector3.Distance(transform.position, lastPosition);
		lastPosition = transform.position;

		if (state == GuestState.WalkingToChair && targetChair != null)
		{
			if (moved < stuckThreshold)
			{
				stuckTimer += Time.deltaTime;
				if (stuckTimer >= stuckCheckInterval && !isRetrying)
				{
					isRetrying = true;
					RoundManager.Instance.StartCoroutine(RetryPathAfterDelay(retryDelay));
				}
			}
			else
			{
				stuckTimer = 0f;
				isRetrying = false;
			}
		}
	}

	private IEnumerator RetryPathAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);

		float moved = Vector3.Distance(transform.position, lastPosition);
		lastPosition = transform.position;

		if (state == GuestState.WalkingToChair && !isLeaving && moved < stuckThreshold)
		{
			if (targetChair != null)
				agent.SetDestination(targetChair.position);
		}

		stuckTimer = 0f;
		isRetrying = false;
	}

	private void BecomeTroubleGuest()
	{
		RoundManager.Instance.chairGroupManager.RemovOrderGuest(this);
		// ���B�z�y������P�ୱ�M�z�]�p�G���ɦb�y��W�^
		if (targetChair != null)
		{
			// �����Ȥl���l���Y�A�קK TroubleGuest �~�Ө�
			transform.SetParent(RoundManager.Instance.guestGroupManager.transform);

			// ����Ȥl�P�M��
			RoundManager.Instance.chairGroupManager.ReleaseChair(targetChair);
			RoundManager.Instance.chairGroupManager.ClearChairItem(targetChair);
			targetChair = null;
		}

		// �O�U�{�b��m�P�~�[
		Vector3 pos = transform.position;
		Sprite face = guestSpriteRenderer != null ? guestSpriteRenderer.sprite : null;

		// �s GuestGroupManager �ͦ� TroubleGuest�A�u���y
		RoundManager.Instance.guestGroupManager.SpawnTroubleGuestAt(pos, face);

		// �^���ۤv
		if (poolHandler != null) poolHandler.Release();
		else Destroy(gameObject);
	}

	#endregion

	#region Public Properties

	// NormalGuestController.cs
	public void ResetPatience()
	{
		orderPatienceLeft = maxOrderPatience;
		dishPatienceLeft = maxDishPatience;
	}

	public Sprite GetOrderFood()
	{
		return foodSpriteRenderer.sprite;
	}
	#endregion

}
