using System.Collections;
using System.Collections.Generic;
using FoodsGroup;
using UnityEngine;

public class FoodsGroupManager : MonoBehaviour
{
	#region ===== Inspector 設定 =====

	[Header("-------- Food Setting ---------")]
	[SerializeField] private float foodSpawnInerval = 3f;

	[Header("-------- Reference ---------")]
	[SerializeField] private GameObject[] foodPrefabs;
	[SerializeField] private Transform[] foodsSpawnPositions;
	[SerializeField] private SpriteRenderer foodSprite;
	[SerializeField] private GameObject foodMakingBarFill;
	[SerializeField] public DessertController dessertController;
	
	[Header("-------- Highlight ---------")]
	[SerializeField] private GameObject yellowFrame;
	[SerializeField] private Collider2D playerCollider;
	[SerializeField] private LayerMask foodLayerMask;

	#endregion

	#region ===== 欄位與狀態 =====

	private List<GameObject> currentfoods;
	private Transform currentFoodTarget = null;
	private bool isPlayerInsideTrigger;

	private Coroutine makeRoutine = null;
	private Sprite makingTargetSprite = null;

	#endregion

	#region ===== Unity 生命周期 =====

	private void Start()
	{
		isPlayerInsideTrigger = false;
		
		currentfoods = new List<GameObject>();

		if (yellowFrame != null)
			yellowFrame.SetActive(false);

		SetupFoodMakingBar(false, 0f);

		if (foodSprite != null)
			foodSprite.sprite = null;
	}

	private void Update()
	{
		UpdateSelectFood();
		UpdateFoodOnTable();
	}

	#endregion

	#region ===== 製作流程 =====

	private IEnumerator MakeFoodRoutine()
	{
		if (foodSprite != null)
			foodSprite.sprite = makingTargetSprite;

		SetupFoodMakingBar(true, 0f);

		float t = 0f;
		float duration = Mathf.Max(0f, foodSpawnInerval);

		while (t < duration)
		{
			t += Time.deltaTime;
			float p = Mathf.Clamp01(t / duration);
			SetFoodMakingBarProgress(p);
			yield return null;
		}

		if (foodSprite != null)
			foodSprite.sprite = null;

		SetupFoodMakingBar(false, 0f);
		makeRoutine = null;
		makingTargetSprite = null;
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

	private bool IsMaking() => makeRoutine != null;

	#endregion

	#region ===== 食物擺盤邏輯 =====

	private void UpdateFoodOnTable()
	{
		List<NormalGuestController> guestList = RoundManager.Instance.chairGroupManager.GetGuestsOrderList();
		if (guestList == null || guestList.Count == 0)
		{
			foreach (var pos in foodsSpawnPositions)
				ClearChildren(pos);
			return;
		}

		int len = Mathf.Min(guestList.Count, foodsSpawnPositions.Length);
		for (int i = 0; i < len; i++)
		{
			Transform spawn = foodsSpawnPositions[i];
			NormalGuestController guest = guestList[i];
			if (!spawn || !guest)
			{
				if (spawn) ClearChildren(spawn);
				continue;
			}

			FoodStatus needed = guest.GetOrderFood();
			if (!needed)
			{
				ClearChildren(spawn);
				continue;
			}

			Transform child = (spawn.childCount > 0) ? spawn.GetChild(0) : null;
			if (child)
			{
				FoodStatus foodStatus = child.GetComponent<FoodStatus>();
				if (foodStatus.foodType == needed.foodType) continue;
				Object.Destroy(child.gameObject);
			}

			GameObject prefab = FindPrefabByFoodType(needed.foodType);
			if (prefab)
			{
				GameObject go = Instantiate(prefab, spawn);
				go.transform.localPosition = Vector3.zero;
				currentfoods.Add(go);
			}
			else
			{
#if UNITY_EDITOR
				Debug.LogWarning($"[FoodsGroupManager] 找不到對應的餐點 Prefab：{needed.name}");
#endif
				ClearChildren(spawn);
			}
		}

		for (int i = len; i < foodsSpawnPositions.Length; i++)
			ClearChildren(foodsSpawnPositions[i]);
	}

	private GameObject FindPrefabByFoodType(FoodType type)
	{
		foreach (GameObject pf in foodPrefabs)
		{
			if (!pf) continue;
			FoodType t = pf.GetComponent<FoodStatus>().foodType;
			if (t == type) return pf;
		}
		return null;
	}

	private void ClearChildren(Transform parent)
	{
		if (!parent) return;
		for (int i = parent.childCount - 1; i >= 0; i--)
			Destroy(parent.GetChild(i).gameObject);
	}

	#endregion

	#region ===== 食物選取指示 =====

	private void UpdateSelectFood()
	{
		if (!isPlayerInsideTrigger)
		{
			if (yellowFrame.activeSelf) yellowFrame.SetActive(false);
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
		else if (yellowFrame.activeSelf)
			yellowFrame.SetActive(false);
	}

	private Transform GetHoveredFoodByMouse()
	{
		Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, foodLayerMask);
		return hit ? hit.transform : null;
	}

	private Transform GetTouchedFoodByPlayer()
	{
		foreach (var food in currentfoods)
		{
			if (!food) continue;
			var col = food.GetComponent<Collider2D>();
			if (col != null && col.isTrigger && col.IsTouching(playerCollider))
				return food.transform;
		}
		return null;
	}

	private void UpdateYellowFrame(Transform target)
	{
		currentFoodTarget = target;
		if (!yellowFrame.activeSelf)
			yellowFrame.SetActive(true);
		yellowFrame.transform.position = currentFoodTarget.position;
	}

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

	#endregion

	#region ===== 外部取得與流程觸發 =====

	/// <summary>隨機抽餐點並開始製作</summary>
	public FoodStatus OrderFoodRandomly()
	{
		if (foodPrefabs == null || foodPrefabs.Length == 0) return null;

		int randomIndex = Random.Range(0, foodPrefabs.Length);
		GameObject selected = foodPrefabs[randomIndex];
		FoodStatus food = selected.GetComponent<FoodStatus>();

		if (food != null && !IsMaking())
		{
			makingTargetSprite = food.GetComponent<SpriteRenderer>().sprite;
			makeRoutine = StartCoroutine(MakeFoodRoutine());
		}

		return food;
	}

	/// <summary>目前選中的餐點 GameObject（可用於互動）</summary>
	public GameObject GetCurrentDishObject()
	{
		return currentFoodTarget ? currentFoodTarget.gameObject : null;
	}

	#endregion
}
