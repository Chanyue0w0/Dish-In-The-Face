using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NormalGuestController : MonoBehaviour
{
	[Header("-------- Setting --------")]
	[SerializeField] private float minPatience = 10f;     // 最短耐心秒數
	[SerializeField] private float maxPatience = 20f;     // 最長耐心秒數
	[SerializeField] private float eatTime = 10f;         // 吃飯所需時間
	[SerializeField] private float moveSpeed = 2f;        // 移動速度

	[Header("-------- Stuck Retry Setting --------")]
	[SerializeField] private float stuckCheckInterval = 1.5f; // 幾秒內都沒移動視為卡住
	[SerializeField] private float stuckThreshold = 0.05f;    // 認定卡住的最小移動距離
	[SerializeField] private float retryDelay = 1f;           // 卡住後延遲幾秒才重新 SetDestination

	[Header("-------- Reference --------")]
	[SerializeField] private RoundManager roundManager;
	[SerializeField] private Transform startPosition;             // 起點位置（通常是大門）
	[SerializeField] private GameObject orderIconObject;          // 點餐圖示
	[SerializeField] private SpriteRenderer foodSpriteRenderer;   // 顯示餐點用
	[SerializeField] private GameObject patienceBar;              // 耐心條物件
	[SerializeField] private Transform barFill;                   // 耐心條內部填色

	private Transform targetChair;          // 目標椅子
	private float patienceTime;             // 耐心秒數
	private float timer = 0f;               // 耐心倒數計時器
	private bool isSeated = false;          // 是否已坐下
	private bool isEating = false;          // 是否正在吃飯
	private bool isLeaving = false;         // 是否正在離場
	private bool isRetrying = false;        // 是否正在等待重試
	private Sprite orderFoodSprite = null;  // 點的餐點
	private NavMeshAgent agent;

	// 卡住偵測用
	private Vector3 lastPosition;
	private float stuckTimer = 0f;

	void Awake()
	{
		// 初始化 NavMeshAgent
		agent = GetComponent<NavMeshAgent>();
		agent.speed = moveSpeed;
		agent.updateRotation = false;
		agent.updateUpAxis = false;

		// 設定耐心秒數
		patienceTime = Random.Range(minPatience, maxPatience);

		// 找起點和管理器
		roundManager = GameObject.Find("Rround Manager").GetComponent<RoundManager>();
		startPosition = GameObject.Find("Door Position").transform;

		// 嘗試找椅子
		targetChair = roundManager.chairGroupManager.FindEmptyChair();
		if (targetChair != null)
		{
			agent.SetDestination(targetChair.position);
		}
		else
		{
			Leave(); // 沒椅子就離場
		}

		// 關閉圖示與耐心條
		orderIconObject.SetActive(false);
		patienceBar.SetActive(false);

		// 初始化位置
		lastPosition = transform.position;
	}

	void Update()
	{
		// 如果椅子突然被佔用，離場
		if (!isSeated && targetChair != null && targetChair.childCount > 1)
		{
			roundManager.chairGroupManager.ReleaseChair(targetChair);
			targetChair = null;
			Leave();
		}

		// 如果到達椅子，執行點餐邏輯
		if (!isSeated && targetChair != null && !agent.pathPending && agent.remainingDistance <= 0.05f)
		{
			ArriveAtChair();
		}

		// 坐下後，開始耐心倒數
		if (isSeated && !isEating)
		{
			timer += Time.deltaTime;
			float ratio = Mathf.Clamp01(1f - (timer / patienceTime));
			barFill.localScale = new Vector3(ratio, 1f, 1f);

			if (timer >= patienceTime)
			{
				Leave(); // 耐心耗盡
			}
		}

		// 卡住檢查
		CheckStuckAndRetry();

		// 翻轉角色方向
		FlipSpriteByVelocity();
	}

	// 到達椅子，進行點餐與耐心條開啟
	private void ArriveAtChair()
	{
		transform.position = targetChair.position;
		isSeated = true;
		transform.SetParent(targetChair);

		OrderDish();

		patienceBar.SetActive(true);
		barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	// 設定隨機餐點圖示
	private void OrderDish()
	{
		orderIconObject.SetActive(true);
		orderFoodSprite = roundManager.foodsGroupManager.OrderFoodRandomly();
		foodSpriteRenderer.sprite = orderFoodSprite;
	}

	// 嘗試上菜，並判斷是否正確
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

	// 吃飯後自動離場
	private IEnumerator EatAndLeave()
	{
		yield return new WaitForSeconds(eatTime);
		Leave();
	}

	// 離場處理：釋放椅子、走向出口並刪除物件
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

		// 直接走回大門（startPosition）作為離場點
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
		Destroy(gameObject); // 到門口後銷毀 NPC
	}


	// 根據 NavMeshAgent velocity.x 翻轉角色左右方向
	private void FlipSpriteByVelocity()
	{
		if (agent.velocity.x > 0.01f)
			transform.rotation = Quaternion.Euler(0, 0, 0);
		else if (agent.velocity.x < -0.01f)
			transform.rotation = Quaternion.Euler(0, 180, 0);
	}

	// 卡住偵測：持續沒移動則啟動延遲重試
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

	// 延遲 retryDelay 秒後，如果還卡住，就重新呼叫 SetDestination
	private IEnumerator RetryPathAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);

		float moved = Vector3.Distance(transform.position, lastPosition);
		lastPosition = transform.position;

		if (!isSeated && !isLeaving && moved < stuckThreshold)
		{
			if (targetChair != null)
			{
				agent.SetDestination(targetChair.position); // 重新嘗試導航
				Debug.Log($"{gameObject.name} 卡住重導航");
			}
		}

		stuckTimer = 0f;
		isRetrying = false;
	}
}
