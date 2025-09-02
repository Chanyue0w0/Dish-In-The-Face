using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FoodsGroup; // FoodType

public class FoodsGroupManager : MonoBehaviour
{
	#region ===== Inspector 設定 =====

	[Header("-------- Food Setting ---------")]
	[SerializeField] private float foodSpawnInterval = 3f; // 單道料理製作秒數

	[Header("-------- Reference ---------")]
	[SerializeField] private GameObject[] foodPrefabs;            // 可製作的餐點 Prefab（每個都需掛 FoodStatus）
	[SerializeField] private Transform[] foodsSpawnPositions;     // 桌上出菜點（每格只能放 1 個子物件）
	[SerializeField] private SpriteRenderer foodSprite;           // 「正在製作中」顯示的食物貼圖
	[SerializeField] private GameObject foodMakingBarFill;        // 製作條（縮放 X 0→1）
	[SerializeField] public DessertController dessertController;  // 保留：甜點系統（目前未用）

	[Header("-------- Highlight ---------")]
	[SerializeField] private GameObject yellowFrame;
	[SerializeField] private Collider2D playerCollider;
	[SerializeField] private LayerMask foodLayerMask;

	#endregion

	#region ===== 欄位與狀態 =====

	// 製作佇列：排隊要做的餐點（使用 Prefab 引用來表示種類）
	private readonly List<GameObject> makingFoodList = new List<GameObject>();

	// 已完成、待上桌：使用 Prefab 引用來表示種類；實際實例在擺上桌時才 Instantiate
	private readonly List<GameObject> finishFoodList = new List<GameObject>();

	private Transform currentFoodTarget;
	private bool isPlayerInsideTrigger;

	private Coroutine makeRoutine;
	private GameObject currentMakingPrefab;   // 目前鍋裡這一道（Prefab 引用）
	private Camera mainCamera;

	#endregion

	#region ===== Unity 生命周期 =====

	private void Awake()
	{
		mainCamera = Camera.main;
	}

	private void Start()
	{
		currentFoodTarget = GetComponent<Transform>();
		isPlayerInsideTrigger = false;

		if (yellowFrame != null) yellowFrame.SetActive(false);
		SetupFoodMakingBar(false, 0f);
		if (foodSprite != null) foodSprite.sprite = null;
	}

	private void Update()
	{
		UpdateSelectFood();
		CheckMakingFoodList();  // 依需求補齊製作佇列並驅動製作
		UpdateFoodOnTable();    // 有空位就上桌
	}

	#endregion

	#region ===== 製作流程 =====

	private IEnumerator MakeFoodRoutine(GameObject prefabToMake)
	{
		currentMakingPrefab = prefabToMake;

		// 顯示「正在製作中」的貼圖與進度條
		if (foodSprite != null)
		{
			var sr = prefabToMake.GetComponent<SpriteRenderer>();
			foodSprite.sprite = sr ? sr.sprite : null;
		}
		SetupFoodMakingBar(true, 0f);

		float t = 0f;
		float duration = Mathf.Max(0f, foodSpawnInterval);

		while (t < duration)
		{
			t += Time.deltaTime;
			SetFoodMakingBarProgress(Mathf.Clamp01(t / duration));
			yield return null;
		}

		// 完成 → 放入 finish 佇列（此時不 Instantiate，等有空位再上桌）
		finishFoodList.Add(prefabToMake);

		// 收尾 UI
		if (foodSprite != null) foodSprite.sprite = null;
		SetupFoodMakingBar(false, 0f);

		makeRoutine = null;
		currentMakingPrefab = null;

		// 若佇列仍有待做，立刻開下一道
		StartNextMakeIfIdle();
	}

	private void StartNextMakeIfIdle()
	{
		if (makeRoutine != null) return;
		if (makingFoodList.Count == 0) return;

		// 先進先出
		var nextPrefab = makingFoodList[0];
		makingFoodList.RemoveAt(0);
		makeRoutine = StartCoroutine(MakeFoodRoutine(nextPrefab));
	}

	private void SetupFoodMakingBar(bool show, float progress)
	{
		if (foodMakingBarFill == null) return;
		foodMakingBarFill.SetActive(show);
		foodMakingBarFill.transform.localScale = new Vector3(Mathf.Clamp01(progress), 1f, 1f);
	}

	private void SetFoodMakingBarProgress(float progress)
	{
		if (foodMakingBarFill == null) return;
		foodMakingBarFill.transform.localScale = new Vector3(Mathf.Clamp01(progress), 1f, 1f);
	}

	/// <summary>
	/// 依目前點單與庫存（桌上/完成/排隊/正在做）計算缺口，補進製作佇列。
	/// </summary>
	private void CheckMakingFoodList()
	{
		// 1) 需求（目前 WaitingDish 的客人）
		Dictionary<FoodType, int> required = CountRequiredFoods();

		// 2) 供給：桌上現貨
		Dictionary<FoodType, int> onTable = CountFoodsOnTable();

		// 3) 供給：完成待上桌
		Dictionary<FoodType, int> finished = CountByType(finishFoodList);

		// 4) 供給：製作排隊中（不含正在做）
		Dictionary<FoodType, int> inQueue = CountByType(makingFoodList);

		// 5) 供給：正在做
		FoodType makingType = currentMakingPrefab ? GetFoodType(currentMakingPrefab) : FoodType.None;

		// 6) 逐類別計算缺口並補入佇列
		foreach (var kv in required)
		{
			var type = kv.Key;
			int need = kv.Value;
			if (type == FoodType.None || need <= 0) continue;

			int supply =
				GetCount(onTable, type) +
				GetCount(finished, type) +
				GetCount(inQueue, type) +
				(makingType == type ? 1 : 0);

			int deficit = need - supply;
			for (int i = 0; i < deficit; i++)
			{
				var prefab = GetPrefab(type);
				if (prefab != null) makingFoodList.Add(prefab);
			}
		}

		// 若目前沒在做，就啟動下一道
		StartNextMakeIfIdle();
	}

	/// <summary>
	/// 把完成清單的餐點擺到桌上空位（依序）。
	/// </summary>
	private void UpdateFoodOnTable()
	{
		if (finishFoodList.Count == 0) return;

		foreach (var pos in foodsSpawnPositions)
		{
			if (pos == null) continue;
			if (pos.childCount > 0) continue; // 這個點已經有菜

			if (finishFoodList.Count == 0) break;

			// 先進先出放一道
			var prefab = finishFoodList[0];
			finishFoodList.RemoveAt(0);

			var obj = Instantiate(prefab, pos);
			obj.transform.localPosition = Vector3.zero;
		}
	}

	#endregion

	#region ===== 食物選取指示（原本就有） =====

	private void UpdateSelectFood()
	{
		if (!isPlayerInsideTrigger)
		{
			if (yellowFrame && yellowFrame.activeSelf) yellowFrame.SetActive(false);
			currentFoodTarget = null;
			return;
		}

		Transform mouseTarget = GetHoveredFoodByMouse();
		if (mouseTarget != null)
		{
			UpdateYellowFrame(mouseTarget);
			return;
		}

		Transform touchedTarget = GetTouchedFoodByPlayer();
		if (touchedTarget != null)
			UpdateYellowFrame(touchedTarget);
		else if (yellowFrame && yellowFrame.activeSelf)
			yellowFrame.SetActive(false);
	}

	private Transform GetHoveredFoodByMouse()
	{
		if (!mainCamera) return null;
		Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
		var hit = Physics2D.OverlapPoint(mouseWorldPos, foodLayerMask);
		return hit ? hit.transform : null;
	}

	private Transform GetTouchedFoodByPlayer()
	{
		foreach (var foodPos in foodsSpawnPositions)
		{
			var col = foodPos.GetComponent<Collider2D>();
			if (col == null || !col.isTrigger || !col.IsTouching(playerCollider)) continue;
			if (foodPos.childCount > 0)
				return foodPos.transform.GetChild(0);
		}
		return null;
	}

	private void UpdateYellowFrame(Transform target)
	{
		currentFoodTarget = target;
		if (yellowFrame && !yellowFrame.activeSelf) yellowFrame.SetActive(true);
		if (yellowFrame) yellowFrame.transform.position = currentFoodTarget.position;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other == playerCollider) isPlayerInsideTrigger = true;
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (other != playerCollider) return;
		isPlayerInsideTrigger = false;
		if (yellowFrame) yellowFrame.SetActive(false);
		currentFoodTarget = null;
	}

	#endregion

	#region ===== 外部取得與流程觸發 =====

	/// <summary>
	/// 隨機抽餐點（只回傳模板作為點單用，不會觸發製作；製作由 CheckMakingFoodList 決定）。
	/// </summary>
	public FoodStatus OrderFoodRandomly()
	{
		if (foodPrefabs == null || foodPrefabs.Length == 0) return null;

		int randomIndex = Random.Range(0, foodPrefabs.Length);
		GameObject selected = foodPrefabs[randomIndex];
		return selected ? selected.GetComponent<FoodStatus>() : null;
	}

	/// <summary>目前選中的餐點 GameObject（可用於互動）</summary>
	public GameObject GetCurrentDishObject()
	{
		return currentFoodTarget ? currentFoodTarget.gameObject : null;
	}

	#endregion

	#region ===== 計數 / 工具 =====

	private Dictionary<FoodType, int> CountRequiredFoods()
	{
		var map = new Dictionary<FoodType, int>();
		// guestsOrderList 由 ChairGroupManager 管（WaitingDish 中的客人會被加入/移除）
		var orderList = RoundManager.Instance.chairGroupManager.GetGuestsOrderList(); // 依據現有介面
		foreach (var guest in orderList)
		{
			if (guest == null) continue;
			var order = guest.GetOrderFood(); // WaitingDish 階段有效
			if (order == null) continue;

			var ft = order.foodType;
			if (ft == FoodType.None) continue;

			map.TryAdd(ft, 0);
			map[ft]++;
		}
		return map;
	}

	private Dictionary<FoodType, int> CountFoodsOnTable()
	{
		var map = new Dictionary<FoodType, int>();
		foreach (var pos in foodsSpawnPositions)
		{
			if (pos == null || pos.childCount == 0) continue;
			var child = pos.GetChild(0);
			var fs = child.GetComponent<FoodStatus>();
			if (fs == null) continue;
			var ft = fs.foodType;
			if (ft == FoodType.None) continue;

			map.TryAdd(ft, 0);
			map[ft]++;
		}
		return map;
	}

	private Dictionary<FoodType, int> CountByType(List<GameObject> prefabList)
	{
		var map = new Dictionary<FoodType, int>();
		foreach (var prefab in prefabList)
		{
			if (prefab == null) continue;
			var ft = GetFoodType(prefab);
			if (ft == FoodType.None) continue;

			map.TryAdd(ft, 0);
			map[ft]++;
		}
		return map;
	}

	private int GetCount(Dictionary<FoodType, int> map, FoodType type)
	{
		return map != null && map.TryGetValue(type, out var v) ? v : 0;
	}

	private GameObject GetPrefab(FoodType type)
	{
		if (type == FoodType.None || foodPrefabs == null) return null;
		return foodPrefabs.FirstOrDefault(p =>
		{
			var fs = p ? p.GetComponent<FoodStatus>() : null;
			return fs != null && fs.foodType == type;
		});
	}

	private FoodType GetFoodType(GameObject prefab)
	{
		var fs = prefab ? prefab.GetComponent<FoodStatus>() : null;
		return fs ? fs.foodType : FoodType.None;
	}

	#endregion
}
