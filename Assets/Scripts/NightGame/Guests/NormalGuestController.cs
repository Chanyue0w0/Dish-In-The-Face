using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NormalGuestController : MonoBehaviour
{
	[Header("-------- Setting --------")]
	[SerializeField] private float minPatience = 10f;
	[SerializeField] private float maxPatience = 20f;
	[SerializeField] private float eatTime = 10f;
	[SerializeField] private float moveSpeed = 2f;

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
	[SerializeField] private GameObject orderIconObject;
	[SerializeField] private GameObject rawBtnIcon;
	[SerializeField] private SpriteRenderer foodSpriteRenderer;
	[SerializeField] private SpriteRenderer guestSpriteRenderer;

	private Transform targetChair;
	private float patienceTime;
	private float timer;
	private bool isSeated;
	private bool isEating;
	private bool isLeaving;
	private bool isRetrying;
	private Sprite orderFoodSprite = null;
	private NavMeshAgent agent;

	private Vector3 lastPosition;
	private float stuckTimer;

	// �����
	private GuestPoolHandler poolHandler;

	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
		agent.speed = moveSpeed;
		agent.updateRotation = false;
		agent.updateUpAxis = false;

		poolHandler = GetComponent<GuestPoolHandler>();
	}

	private void OnEnable()
	{
		// �H���~�[
		if (guestAppearanceList != null && guestAppearanceList.Count > 0)
		{
			int idx = Random.Range(0, guestAppearanceList.Count);
			guestSpriteRenderer.sprite = guestAppearanceList[idx];
		}
		rawBtnIcon.SetActive(false);

		// ���m���A
		patienceTime = Random.Range(minPatience, maxPatience);
		isSeated = false;
		isEating = false;
		isLeaving = false;
		isRetrying = false;
		stuckTimer = 0f;
		timer = 0f;

		// �_�I�P�X�f
		startPosition = RoundManager.Instance.guestGroupManager.enterPoistion;
		endPosition = RoundManager.Instance.guestGroupManager.exitPoistion;

		// ���է�y��
		targetChair = RoundManager.Instance.chairGroupManager.FindEmptyChair();
		if (targetChair != null)
			agent.SetDestination(targetChair.position);
		else
			Leave();

		orderIconObject.SetActive(false);
		patienceBar.SetActive(false);

		lastPosition = transform.position;
	}

	private void Update()
	{
		if (!isSeated && targetChair != null && targetChair.childCount > 1)
		{
			RoundManager.Instance.chairGroupManager.ReleaseChair(targetChair);
			targetChair = null;
			Leave();
		}

		if (!isSeated && targetChair != null && !agent.pathPending && agent.remainingDistance <= 0.05f)
		{
			ArriveAtChair();
		}

		if (isSeated && !isEating)
		{
			timer += Time.deltaTime;
			float ratio = Mathf.Clamp01(1f - (timer / patienceTime));
			barFill.localScale = new Vector3(ratio, 1f, 1f);

			if (timer >= patienceTime)
			{
				Leave();
			}
		}

		CheckStuckAndRetry();
		FlipSpriteByVelocity();
	}

	private void ArriveAtChair()
	{
		transform.position = targetChair.position;
		isSeated = true;
		transform.SetParent(targetChair);

		OrderDish();

		patienceBar.SetActive(true);
		barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	private void OrderDish()
	{
		orderIconObject.SetActive(true);
		orderFoodSprite = RoundManager.Instance.foodsGroupManager.OrderFoodRandomly();
		foodSpriteRenderer.sprite = orderFoodSprite;
	}

	public bool IsReceiveFood(Sprite foods)
	{
		if (!isSeated || isEating || foods != orderFoodSprite)
			return false;

		isEating = true;
		orderIconObject.SetActive(false);
		patienceBar.SetActive(false);
		StopAllCoroutines();

		// �� RoundManager �Ұʨ�{�A�קK Inactive �������
		RoundManager.Instance.StartCoroutine(EatAndLeave());

		return true;
	}

	public void EnablePullIcon(Sprite food, bool isEnable)
	{
		if (!isSeated || isEating || food != orderFoodSprite)
			rawBtnIcon.SetActive(false);
		else rawBtnIcon.SetActive(isEnable);
	}

	private IEnumerator EatAndLeave()
	{
		yield return new WaitForSeconds(eatTime);
		RoundManager.Instance.FinishDishSuccess(targetChair, 10);
		Leave();
	}

	private void Leave()
	{
		if (isLeaving) return;
		isLeaving = true;

		orderIconObject.SetActive(false);
		patienceBar.SetActive(false);

		if (targetChair != null)
		{
			transform.SetParent(RoundManager.Instance.guestGroupManager.transform);
			RoundManager.Instance.chairGroupManager.ReleaseChair(targetChair);
			RoundManager.Instance.chairGroupManager.ClearChairItem(targetChair);
			targetChair = null;
		}

		Vector3 exitPos = endPosition.position;
		agent.SetDestination(exitPos);

		// �P�˥� RoundManager �Ұʨ�{
		RoundManager.Instance.StartCoroutine(CheckExitReached(exitPos));
	}

	private IEnumerator CheckExitReached(Vector3 exitPos)
	{
		float waitTime = 0f;
		float timeout = 25f;

		while (Vector2.Distance(transform.position, exitPos) > 2f && waitTime < timeout)
		{
			if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
				break;

			waitTime += Time.deltaTime;
			yield return null;
		}

		// �^���쪫���
		if (poolHandler != null)
			poolHandler.Release();
		else
			gameObject.SetActive(false);
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

		if (!isSeated && targetChair != null)
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

		if (!isSeated && !isLeaving && moved < stuckThreshold)
		{
			if (targetChair != null)
				agent.SetDestination(targetChair.position);
		}

		stuckTimer = 0f;
		isRetrying = false;
	}
}
