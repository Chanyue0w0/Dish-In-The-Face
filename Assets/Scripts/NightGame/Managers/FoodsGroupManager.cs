using UnityEngine;

public class FoodsGroupManager : MonoBehaviour
{
	//[Header("-------- Setting ---------")]
	//[SerializeField] private float spawnInerval = 10f;
	//[SerializeField] private int spawnFoodsCount = 10;

	[Header("-------- Reference ---------")]
	[SerializeField] private GameObject[] foodsArray;
	[SerializeField] private Transform barFill;
	[SerializeField] private Transform DishLoadingBar;

	[Header("-------- Highlight ---------")]
	[SerializeField] private GameObject yellowFrame;
	[SerializeField] private Collider2D playerCollider; // 玩家自己的碰撞器
	[SerializeField] private LayerMask foodLayerMask;   // 食物的 Layer

	private Transform currentFoodTarget = null;
	private int[] foodsCount;
	private bool isPlayerInsideTrigger = false;

	void Start()
	{
		foodsCount = new int[foodsArray.Length];
		for (int i = 0; i < foodsCount.Length; i++)
			foodsCount[i] = 10;

		if (yellowFrame != null)
			yellowFrame.SetActive(false);
	}

	void Update()
	{
		if (!isPlayerInsideTrigger)
		{
			if (yellowFrame.activeSelf)
				yellowFrame.SetActive(false);
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
		foreach (GameObject food in foodsArray)
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
				yellowFrame.SetActive(false);
		}
	}

	// --- 隨機點餐（取得 Sprite）---
	public Sprite OrderFoodRandomly()
	{
		if (foodsArray == null || foodsArray.Length == 0)
			return null;

		int randomIndex = Random.Range(0, foodsArray.Length);
		GameObject selectedFood = foodsArray[randomIndex];

		SpriteRenderer foodSR = selectedFood.GetComponent<SpriteRenderer>();
		return foodSR != null ? foodSR.sprite : null;
	}

	/// 回傳目前被 yellowFrame 鎖定的食物 GameObject，如果沒有則回傳 null
	public GameObject GetCurrentDishObject()
	{
		return currentFoodTarget != null ? currentFoodTarget.gameObject : null;
	}
}
