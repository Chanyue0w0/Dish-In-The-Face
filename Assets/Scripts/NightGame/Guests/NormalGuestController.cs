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
	[Tooltip("步驟1：思考點餐時間（秒）")]
	[SerializeField] private float thinkOrderTime = 10f;

	[Tooltip("步驟2：等待玩家來點餐的耐心時間（秒）")]
	[SerializeField] private float maxOrderPatience = 20f;

	[Tooltip("步驟3：等待餐點的固定耐心（秒）")]
	[SerializeField] private float maxDishPatience = 25f;

	[Header("-------- Flow Setting --------")]
	[SerializeField] private float stateTransitionDelay = 0.3f; // 狀態切換延遲（秒）

	[Header("-------- Eat / Reorder --------")]
	[Tooltip("吃飯時間（秒）")]
	[SerializeField] private float eatTime = 10f;

	[Tooltip("吃完後，再次點餐的機率（0~1）")]
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
		Thinking,      // 步驟1：思考點餐
		WaitingOrder,  // 步驟2：等待玩家來點餐（按下互動確認）
		WaitingDish,   // 步驟3：等待餐點（固定耐心）
		Eating,
		Leaving
	}

	private GuestState state = GuestState.WalkingToChair;

	private Transform targetChair;

	// 取代舊 timer 的三個剩餘時間變數
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

	// 物件池
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
		// 起點與出口
		if (RoundManager.Instance)
		{
			startPosition = RoundManager.Instance.guestGroupManager.enterPoistion;
			endPosition = RoundManager.Instance.guestGroupManager.exitPoistion;
			// 找座位
		}
	}

	private void OnEnable()
	{
		// 隨機外觀
		if (guestAppearanceList != null && guestAppearanceList.Count > 0)
		{
			int idx = Random.Range(0, guestAppearanceList.Count);
			guestSpriteRenderer.sprite = guestAppearanceList[idx];
		}

		// 重置狀態
		isSeated = false;
		isLeaving = false;
		isRetrying = false;
		stuckTimer = 0f;
		state = GuestState.WalkingToChair;

		// 重置各剩餘時間
		thinkTimeLeft = 0f;
		orderPatienceLeft = 0f;
		dishPatienceLeft = 0f;

		chatBoxIconObj.SetActive(false);
		rawBtnIconObj.SetActive(false);
		questionIconObj.SetActive(false);
		patienceBar.SetActive(false);
		if (barFill != null) barFill.localScale = new Vector3(1f, 1f, 1f);
		foodSpriteRenderer.sprite = null;

		// 起點與出口
		if (RoundManager.Instance)
		{
			startPosition = RoundManager.Instance.guestGroupManager.enterPoistion;
			endPosition = RoundManager.Instance.guestGroupManager.exitPoistion;
			// 找座位
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
		// 椅子被搶走就離開
		if (!isSeated && targetChair != null && targetChair.childCount > 1)
		{
			RoundManager.Instance.chairGroupManager.ReleaseChair(targetChair);
			targetChair = null;
			Leave();
		}

		// 抵達座位
		if (state == GuestState.WalkingToChair && targetChair != null && !agent.pathPending && agent.remainingDistance <= 0.05f)
		{
			ArriveAtChair();
		}

		// 狀態驅動的計時與 UI
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

		// 進入「思考點餐」
		EnterThinking();
	}

	private void EnterThinking()
	{
		state = GuestState.Thinking;

		chatBoxIconObj.SetActive(false);
		patienceBar.SetActive(false);
		foodSpriteRenderer.sprite = null; // 思考時不顯示食物

		// 思考完「決定」餐點，但尚未讓玩家看到；等玩家來互動才顯示訂單圖示
		orderFoodSprite = RoundManager.Instance.foodsGroupManager.OrderFoodRandomly();
		thinkTimeLeft = thinkOrderTime;

		if (barFill != null) barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	private void EnterWaitingOrder()
	{
		state = GuestState.WaitingOrder;

		// 顯示玩家可互動之提示（例如 X/Raw 按鍵）
		rawBtnIconObj.SetActive(false);
		chatBoxIconObj.SetActive(true);
		questionIconObj.SetActive(true);// 問號可互動（確認點單）

		// 用相同耐心值 UI
		patienceBar.SetActive(true);
		orderPatienceLeft = maxOrderPatience;
		if (barFill != null) barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	private void EnterWaitingDish()
	{
		state = GuestState.WaitingDish;

		rawBtnIconObj.SetActive(false);
		chatBoxIconObj.SetActive(true);    // 顯示餐點訊息框
		questionIconObj.SetActive(false);

		// 在對話框上展示餐點圖
		foodSpriteRenderer.sprite = orderFoodSprite;

		// 固定耐心時間
		patienceBar.SetActive(true);
		dishPatienceLeft = maxDishPatience;
		if (barFill != null) barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	private void EnterEating()
	{
		state = GuestState.Eating;

		chatBoxIconObj.SetActive(false);
		patienceBar.SetActive(false);

		// 由 RoundManager 啟動協程，避免 Inactive 物件報錯
		RoundManager.Instance.StartCoroutine(EatAndThenDecide());
	}

	private IEnumerator EatAndThenDecide()
	{
		yield return new WaitForSeconds(eatTime);

		// 結帳（一次餐點）
		RoundManager.Instance.FinishDishSuccess(targetChair, 10);

		// 是否再點一次？
		if (Random.value < reorderProbability)
		{
			// 回到思考 → 等待玩家點餐 → 等待餐點
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
	/// 供玩家呼叫：確認客人的訂單。
	/// 僅在 WaitingOrder 狀態有效。成功會切至 WaitingDish 並開始固定耐心倒數。
	/// </summary>
	/// <returns>是否確認成功</returns>
	public bool ConfirmOrderByPlayer()
	{
		if (!isSeated || state != GuestState.WaitingOrder)
			return false;

		Invoke(nameof(EnterWaitingDish), stateTransitionDelay);
		return true;
	}

	/// <summary>
	/// 玩家送餐。僅在 WaitingDish 狀態且餐點正確時成立，並進入 Eating。
	/// </summary>
	public bool IsReceiveFood(Sprite foods)
	{
		if (!isSeated || state != GuestState.WaitingDish || foods != orderFoodSprite)
			return false;

		EnterEating();
		return true;
	}

	/// <summary>
	/// WaitingOrder直接顯示。在 WaitingDish 階段，若玩家手上是正確餐點才顯示互動提示。
	/// </summary>
	public void EnableInteractIcon(Sprite food, bool isEnable)
	{
		rawBtnIconObj.SetActive(isEnable);
		// 若是等待玩家來點餐 rawBtn 顯示
		if (state == GuestState.WaitingOrder) return;
		// 僅在等待餐點、且食物正確時顯示可交付提示
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
					// 思考完成，進入「等待玩家來點餐」
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

		// 回收到物件池
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
		// 先處理座位釋放與桌面清理（如果此時在座位上）
		if (targetChair != null)
		{
			// 脫離椅子父子關係，避免 TroubleGuest 繼承到
			transform.SetParent(RoundManager.Instance.guestGroupManager.transform);

			// 釋放椅子與清桌
			RoundManager.Instance.chairGroupManager.ReleaseChair(targetChair);
			RoundManager.Instance.chairGroupManager.ClearChairItem(targetChair);
			targetChair = null;
		}

		// 記下現在位置與外觀
		Vector3 pos = transform.position;
		Sprite face = guestSpriteRenderer != null ? guestSpriteRenderer.sprite : null;

		// 叫 GuestGroupManager 生成 TroubleGuest，沿用臉
		RoundManager.Instance.guestGroupManager.SpawnTroubleGuestAt(pos, face);

		// 回收自己
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
