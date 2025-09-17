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
	[SerializeField] private float thinkOrderTime = 10f;
	[SerializeField] private float maxOrderPatience = 20f;
	[SerializeField] private float maxDishPatience = 25f;

	[Header("-------- Flow Setting --------")]
	[SerializeField] private float stateTransitionDelay = 0.3f; // 狀態切換延遲（秒）

	[Header("-------- Eat / Reorder --------")]
	[SerializeField] private float eatTime = 10f;
	[SerializeField, Range(0f, 1f)] private float reorderProbability = 0.3f;

	[Header("-------- Stuck Retry Setting --------")]
	[SerializeField] private float stuckCheckInterval = 1.5f;
	[SerializeField] private float stuckThreshold = 0.05f;
	[SerializeField] private float retryDelay = 2f;

	[Header("-------- Appearance --------")]
	[SerializeField] private List<GameObject> guestAppearanceList = new List<GameObject>();
	
	[Header("-------- Reference --------")]
	[SerializeField] private Transform barFill;
	[SerializeField] private GameObject patienceBar;
	[SerializeField] private GameObject chatBoxIconObj;
	[SerializeField] private GameObject rawBtnIconObj;
	[SerializeField] private GameObject questionIconObj;
	[SerializeField] private SpriteRenderer foodSpriteRenderer;
	// [SerializeField] private SpriteRenderer guestSpriteRenderer;

	#endregion

	#region Runtime State

	private enum GuestState
	{
		WalkingToChair,
		Thinking,      // 進入階段1：思考點餐
		WaitingOrder,  // 進入階段2：等待玩家點餐（需玩家確認）
		WaitingDish,   // 進入階段3：等待餐點（固定耐心時間）
		Eating,
		Leaving
	}

	private GuestState state = GuestState.WalkingToChair;

	private Transform targetChair;

	// 各階段的計時器
	private float thinkTimeLeft = 0f;
	private float orderPatienceLeft = 0f;
	private float dishPatienceLeft = 0f;

	private bool isSeated;
	private bool isRetrying;
	private bool isLeaving;

	private int spendCoin;
	
	private FoodStatus orderFoodStatus;
	private NavMeshAgent agent;
	private GameObject appearanceObject;
	
	private Transform endPosition;
	private Vector3 lastPosition;
	private float stuckTimer;

	// 客人物件池處理
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
		// 初始化入口與出口
		if (RoundManager.Instance)
		{
			// startPosition = RoundManager.Instance.guestGroupManager.enterPoistion;
			endPosition = RoundManager.Instance.guestGroupManager.exitPoistion;
		}
	}

	private void OnEnable()
	{
		var components = GetComponentsInChildren<GuestAnimationController>(true); // true = 包含未啟用物件
		foreach (var comp in components)
		{
			DestroyImmediate(comp.gameObject); // 編輯器下立刻刪除
		}
		// 隨機外觀
		if (guestAppearanceList != null && guestAppearanceList.Count > 0)
		{
			int idx = Random.Range(0, guestAppearanceList.Count);
			GuestAnimationController previous = GetComponentInChildren<GuestAnimationController>();
			if (previous != null) Destroy(previous.gameObject); // 如果已經有造型，從新選擇
			appearanceObject = Instantiate(guestAppearanceList[idx], transform.position, transform.rotation, transform);
		}

		if (appearanceObject == null)
		{
			Debug.LogWarning("not found animation: " + transform.name);
		}
		
		// 狀態初始化
		isSeated = false;
		isLeaving = false;
		isRetrying = false;
		stuckTimer = 0f;
		state = GuestState.WalkingToChair;

		// 重設計時器
		thinkTimeLeft = 0f;
		orderPatienceLeft = 0f;
		dishPatienceLeft = 0f;

		chatBoxIconObj.SetActive(false);
		rawBtnIconObj.SetActive(false);
		questionIconObj.SetActive(false);
		patienceBar.SetActive(false);
		if (barFill != null) barFill.localScale = new Vector3(1f, 1f, 1f);
		foodSpriteRenderer.sprite = null;
		orderFoodStatus = null;
		spendCoin = 0;
		
		// 找椅子
		if (RoundManager.Instance)
		{
			// startPosition = RoundManager.Instance.guestGroupManager.enterPoistion;
			endPosition = RoundManager.Instance.guestGroupManager.exitPoistion;
			targetChair = RoundManager.Instance.chairGroupManager.FindEmptyChair(this);
		}

		if (targetChair != null)
			agent.SetDestination(targetChair.position);
		else
			Leave(false);

		lastPosition = transform.position;
	}

	private void Update()
	{
		// 椅子被佔走時，重新處理
		if (!isSeated && targetChair != null && targetChair.childCount > 1)
		{
			RoundManager.Instance.chairGroupManager.ReleaseChair(targetChair);
			targetChair = null;
			Leave(false);
		}

		// 抵達椅子
		if (state == GuestState.WalkingToChair && targetChair != null && !agent.pathPending && agent.remainingDistance <= 0.05f)
		{
			ArriveAtChair();
		}

		// 狀態計時與 UI 更新
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

		// 進入「思考點餐」階段
		EnterThinking();
	}

	private void EnterThinking()
	{
		state = GuestState.Thinking;

		chatBoxIconObj.SetActive(false);
		patienceBar.SetActive(false);
		foodSpriteRenderer.sprite = null; // 清除餐點圖示

		// 隨機生成一份點餐需求（UI 稍後才顯示）
		orderFoodStatus = RoundManager.Instance.foodsGroupManager.OrderFoodRandomly();
		thinkTimeLeft = thinkOrderTime;

		if (barFill != null) barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	private void EnterWaitingOrder()
	{
		state = GuestState.WaitingOrder;

		// 顯示互動 UI（例如 Raw/Question）
		rawBtnIconObj.SetActive(false);
		chatBoxIconObj.SetActive(true);
		questionIconObj.SetActive(true); // 玩家需要確認

		// 耐心條 UI
		patienceBar.SetActive(true);
		orderPatienceLeft = maxOrderPatience;
		if (barFill != null) barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	private void EnterWaitingDish()
	{
		state = GuestState.WaitingDish;

		rawBtnIconObj.SetActive(false);
		chatBoxIconObj.SetActive(true);    // 顯示等待圖示
		questionIconObj.SetActive(false);

		// 在對話框顯示餐點圖片
		foodSpriteRenderer.sprite = orderFoodStatus.GetComponent<SpriteRenderer>().sprite;
		RoundManager.Instance.chairGroupManager.AddOrderGuest(this);

		// 設定耐心時間
		patienceBar.SetActive(true);
		dishPatienceLeft = maxDishPatience;
		if (barFill != null) barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	private void EnterEating()
	{
		state = GuestState.Eating;

		chatBoxIconObj.SetActive(false);
		patienceBar.SetActive(false);

		// RoundManager 處理後續（進食完成後進入下一階段）
		RoundManager.Instance.StartCoroutine(EatAndThenDecide());
	}

	private IEnumerator EatAndThenDecide()
	{
		yield return new WaitForSeconds(eatTime);

		// 完成餐點
		RoundManager.Instance.FinishDishSuccess(targetChair, 10);

		// 檢查是否再次點餐
		if (Random.value < reorderProbability)
		{
			EnterThinking();
		}
		else
		{
			Leave(true);
		}
	}

	#endregion

	#region Public Interactions

	/// <summary>
	/// 玩家確認點餐
	/// （需在 WaitingOrder 狀態下才可進入 WaitingDish 狀態）
	/// </summary>
	public bool ConfirmOrderByPlayer()
	{
		if (!isSeated || state != GuestState.WaitingOrder)
			return false;

		Invoke(nameof(EnterWaitingDish), stateTransitionDelay);
		return true;
	}

	/// <summary>
	/// 玩家送餐
	/// （需在 WaitingDish 狀態，且餐點正確）
	/// </summary>
	public bool IsReceiveFood(FoodStatus foods)
	{
		if (!isSeated || state != GuestState.WaitingDish || foods.foodType != orderFoodStatus.foodType)
			return false;

		RoundManager.Instance.chairGroupManager.RemovOrderGuest(this);
		
		spendCoin += orderFoodStatus.price;
		
		EnterEating();
		return true;
	}

	/// <summary>
	/// 顯示/隱藏互動圖示
	/// </summary>
	public void EnableInteractIcon(Sprite food, bool isEnable)
	{
		rawBtnIconObj.SetActive(isEnable);
		if (state == GuestState.WaitingOrder) return;
		if (isSeated && state == GuestState.WaitingDish && food == orderFoodStatus) return;
		
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
					// 思考完成 → 進入等待玩家點餐
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
				break;
		}
	}

	private void UpdatePatienceBar(float remain, float total)
	{
		if (!patienceBar.activeSelf) patienceBar.SetActive(true);
		float ratio = Mathf.Clamp01(Mathf.Max(remain, 0f) / Mathf.Max(total, 0.0001f));
		if (barFill != null) barFill.localScale = new Vector3(ratio, 1f, 1f);
	}

	private void Leave(bool isFinishDish)
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

		if (isFinishDish)
		{
			RoundManager.Instance.coinManager.AddCoin(spendCoin);
		}
		
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

		// 回收物件
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
		// 轉換為暴走客人（若還在椅子上，先釋放椅子）
		if (targetChair != null)
		{
			transform.SetParent(RoundManager.Instance.guestGroupManager.transform);
			RoundManager.Instance.chairGroupManager.ReleaseChair(targetChair);
			RoundManager.Instance.chairGroupManager.ClearChairItem(targetChair);
			targetChair = null;
		}

		// 在當前位置生成 TroubleGuest
		Vector3 pos = transform.position;
		// Sprite face = guestSpriteRenderer != null ? guestSpriteRenderer.sprite : null;

		RoundManager.Instance.guestGroupManager.SpawnTroubleGuestAt(pos, appearanceObject);

		// 回收自身
		if (poolHandler != null) poolHandler.Release();
		else Destroy(gameObject);
	}

	#endregion

	#region Public Properties

	public void ResetPatience()
	{
		orderPatienceLeft = maxOrderPatience;
		dishPatienceLeft = maxDishPatience;
	}

	public FoodStatus GetOrderFood()
	{
		return orderFoodStatus;
	}

	// ===== 新增的狀態判斷 function =====
	public bool IsThinking()     => state == GuestState.Thinking;
	public bool IsEating()       => state == GuestState.Eating;
	public bool IsOrdering()     => state == GuestState.WaitingOrder;
	public bool IsWaitingDish()  => state == GuestState.WaitingDish;
	
	public bool IsMoving() {
		return agent != null && agent.enabled && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.01f;
	}

	public bool IsLeaving()      => state == GuestState.Leaving;

	#endregion

}
