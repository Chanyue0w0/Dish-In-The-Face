using UnityEngine;

public class FoodsGroupManager : MonoBehaviour
{
	//[Header("-------- Setting ---------")]
	//[SerializeField] private float spawnInerval = 10f;
	//[SerializeField] private int spawnFoodsCount = 10;

	[Header("-------- Dessert Cooldown ---------")]
	[SerializeField, Min(0f)] private float dessertColdownSeconds = 5f; // 冷卻秒數
	[SerializeField] private bool hideBarWhenReady = true;               // 冷卻完成自動隱藏

	[Header("-------- Reference ---------")]
	[SerializeField] private GameObject[] currentfoods;
	[SerializeField] private GameObject[] foodPrefabs;
	[SerializeField] private Transform[] foodsSpawnPosition;
	[SerializeField] private Transform dessertBarFill;
	//[SerializeField] private Transform dishLoadingBar;
	[SerializeField] private Animator dessertAnimator;

	[Header("-------- Highlight ---------")]
	[SerializeField] private GameObject yellowFrame;
	[SerializeField] private Collider2D playerCollider; // 玩家自己的碰撞器
	[SerializeField] private LayerMask foodLayerMask;   // 食物的 Layer

	private Transform currentFoodTarget = null;
	private int[] foodsCount;
	private bool isPlayerInsideTrigger = false;

	private float dessertCdRemain = 0f;  // 剩餘冷卻時間
	private bool IsDessertOnCd => dessertCdRemain > 0f;
	void Start()
	{
		foodsCount = new int[currentfoods.Length];
		for (int i = 0; i < foodsCount.Length; i++)
			foodsCount[i] = 10;

		if (yellowFrame != null)
			yellowFrame.SetActive(false);

		// 初始化讀條狀態
		if (dessertBarFill != null) dessertBarFill.gameObject.SetActive(false);
		if (dessertBarFill != null) dessertBarFill.localScale = new Vector3(0f, 1f, 1f); // 初始 0（未在冷卻）
	}

	void Update()
	{
		UpdateDesertColdDown();

		if (!isPlayerInsideTrigger)
		{
			if (yellowFrame.activeSelf)
				yellowFrame.SetActive(false);
			currentFoodTarget = null;
			return;
		}

		// 滑鼠優先
		Transform mouseTarget = GetHoveredFoodByMouse();
		if (mouseTarget != null)
		{
			UpdateYellowFrame(mouseTarget);
			return;
		}

		// 玩家碰撞為次要
		Transform touchedTarget = GetTouchedFoodByPlayer();
		if (touchedTarget != null)
		{
			UpdateYellowFrame(touchedTarget);
		}
		else
		{
			if (yellowFrame.activeSelf)
				yellowFrame.SetActive(false);
		}
	}

	private void UpdateDesertColdDown()
	{
		// --- 更新甜點冷卻條 ---
		if (IsDessertOnCd)
		{
			dessertCdRemain = Mathf.Max(0f, dessertCdRemain - Time.deltaTime);

			// 進度 = 剩餘時間 / 總冷卻 → 從 1 漸減到 0
			float p = (dessertColdownSeconds <= 0f) ? 0f : (dessertCdRemain / dessertColdownSeconds);
			if (dessertBarFill != null)
				dessertBarFill.localScale = new Vector3(Mathf.Clamp01(p), 1f, 1f);

			// 冷卻結束 → 隱藏或顯示空條
			if (!IsDessertOnCd)
			{
				if (dessertBarFill != null)
					dessertBarFill.localScale = new Vector3(0f, 1f, 1f); // 變成空
				if (dessertBarFill != null && hideBarWhenReady)
					dessertBarFill.gameObject.SetActive(false);
			}
		}
	}

	// --- 滑鼠 hover 檢查 ---
	private Transform GetHoveredFoodByMouse()
	{
		Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

		// 只搜尋指定 Layer（通常是 Food）
		Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, foodLayerMask);
		if (hit != null)
		{
			return hit.transform;
		}
		return null;
	}

	// --- 玩家碰撞檢查 ---
	private Transform GetTouchedFoodByPlayer()
	{
		foreach (GameObject food in currentfoods)
		{
			if (food == null) continue;

			Collider2D foodCollider = food.GetComponent<Collider2D>();
			if (foodCollider != null && foodCollider.isTrigger && foodCollider.IsTouching(playerCollider))
			{
				return food.transform;
			}
		}
		return null;
	}

	// --- 移動黃框 ---
	private void UpdateYellowFrame(Transform target)
	{
		currentFoodTarget = target;

		if (!yellowFrame.activeSelf)
			yellowFrame.SetActive(true);

		yellowFrame.transform.position = currentFoodTarget.position;
	}

	// --- Trigger 檢查進出 ---
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other == playerCollider)
			isPlayerInsideTrigger = true;
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (other == playerCollider)
		{
			isPlayerInsideTrigger = false;
			if (yellowFrame.activeSelf)
			{
				yellowFrame.SetActive(false);
				currentFoodTarget = null;
			}
		}
	}

	// --- 隨機點餐（取得 Sprite）---
	public Sprite OrderFoodRandomly()
	{
		if (foodPrefabs == null || foodPrefabs.Length == 0)
			return null;

		int randomIndex = Random.Range(0, foodPrefabs.Length);
		GameObject selectedFood = foodPrefabs[randomIndex];

		SpriteRenderer foodSR = selectedFood.GetComponent<SpriteRenderer>();
		return foodSR != null ? foodSR.sprite : null;
	}

	/// 回傳目前被 yellowFrame 鎖定的食物 GameObject，如果沒有則回傳 null
	public GameObject GetCurrentDishObject()
	{
		return currentFoodTarget != null ? currentFoodTarget.gameObject : null;
	}

	// 使用發點心
	// 使用發點心（帶冷卻＆讀條）
	public bool UseDessert()
	{
		// 冷卻中就不觸發
		if (IsDessertOnCd) return false;

		// 播放動畫（從頭）
		if (dessertAnimator != null)
			dessertAnimator.Play("DessertEffect", -1, 0f);

		RoundManager.Instance.chairGroupManager.ResetAllSetGuestsPatience();

		// 開始冷卻
		StartDessertColdown();
		return true;
	}

	private void StartDessertColdown()
	{
		if (dessertColdownSeconds <= 0f)
		{
			dessertCdRemain = 0f;
			if (dessertBarFill != null) dessertBarFill.localScale = new Vector3(0f, 1f, 1f); // 一開始就是空
			if (dessertBarFill != null && hideBarWhenReady)
				dessertBarFill.gameObject.SetActive(false);
			return;
		}

		dessertCdRemain = dessertColdownSeconds;

		// 顯示讀條並從滿開始
		if (dessertBarFill != null) dessertBarFill.gameObject.SetActive(true);
		if (dessertBarFill != null) dessertBarFill.localScale = new Vector3(1f, 1f, 1f);
	}

}
