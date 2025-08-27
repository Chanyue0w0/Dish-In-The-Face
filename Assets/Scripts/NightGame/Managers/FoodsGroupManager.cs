using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodsGroupManager : MonoBehaviour
{
	#region ===== Inspector 設定 =====

	[Header("-------- Food Setting ---------")]
	[SerializeField] private float foodSpawnInerval = 3f;

	[Header("-------- Dessert Cooldown ---------")]
	[SerializeField, Min(0f)] private float dessertColdownSeconds = 5f;
	[SerializeField] private bool hideBarWhenReady = true;

	[Header("-------- Reference ---------")]
	[SerializeField] private GameObject[] foodPrefabs;
	[SerializeField] private Transform[] foodsSpawnPositions;
	[SerializeField] private SpriteRenderer foodSprite;
	[SerializeField] private GameObject foodMakingBarFill;
	[SerializeField] private Transform dessertBarFill;
	[SerializeField] private Animator dessertAnimator;

	[Header("-------- Highlight ---------")]
	[SerializeField] private GameObject yellowFrame;
	[SerializeField] private Collider2D playerCollider;
	[SerializeField] private LayerMask foodLayerMask;

	#endregion

	#region ===== 欄位與狀態 =====

	private List<GameObject> currentfoods;
	private Transform currentFoodTarget = null;
	private bool isPlayerInsideTrigger = false;

	private float dessertCdRemain = 0f;
	private bool IsDessertOnCd => dessertCdRemain > 0f;

	private Coroutine makeRoutine = null;
	private Sprite makingTargetSprite = null;

	#endregion

	#region ===== Unity 生命周期 =====

	private void Start()
	{
		currentfoods = new List<GameObject>();

		if (yellowFrame != null)
			yellowFrame.SetActive(false);

		if (dessertBarFill != null)
		{
			dessertBarFill.gameObject.SetActive(false);
			dessertBarFill.localScale = new Vector3(0f, 1f, 1f);
		}

		SetupFoodMakingBar(false, 0f);

		if (foodSprite != null)
			foodSprite.sprite = null;
	}

	private void Update()
	{
		UpdateDesertColdDown();
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

	#region ===== 點心技能 =====

	private void UpdateDesertColdDown()
	{
		if (!IsDessertOnCd) return;

		dessertCdRemain = Mathf.Max(0f, dessertCdRemain - Time.deltaTime);
		float p = (dessertColdownSeconds <= 0f) ? 0f : (dessertCdRemain / dessertColdownSeconds);

		if (dessertBarFill != null)
			dessertBarFill.localScale = new Vector3(Mathf.Clamp01(p), 1f, 1f);

		if (!IsDessertOnCd)
		{
			if (dessertBarFill != null)
				dessertBarFill.localScale = new Vector3(0f, 1f, 1f);
			if (dessertBarFill != null && hideBarWhenReady)
				dessertBarFill.gameObject.SetActive(false);
		}
	}

	private void StartDessertColdown()
	{
		if (dessertColdownSeconds <= 0f)
		{
			dessertCdRemain = 0f;
			if (dessertBarFill != null)
				dessertBarFill.localScale = new Vector3(0f, 1f, 1f);
			if (dessertBarFill != null && hideBarWhenReady)
				dessertBarFill.gameObject.SetActive(false);
			return;
		}

		dessertCdRemain = dessertColdownSeconds;

		if (dessertBarFill != null)
		{
			dessertBarFill.gameObject.SetActive(true);
			dessertBarFill.localScale = new Vector3(1f, 1f, 1f);
		}
	}

	public bool UseDessert()
	{
		if (IsDessertOnCd) return false;

		if (dessertAnimator != null)
			dessertAnimator.Play("DessertEffect", -1, 0f);

		RoundManager.Instance.chairGroupManager.ResetAllSetGuestsPatience();
		StartDessertColdown();
		return true;
	}

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

			Sprite needed = guest.GetOrderFood();
			if (!needed)
			{
				ClearChildren(spawn);
				continue;
			}

			Transform child = (spawn.childCount > 0) ? spawn.GetChild(0) : null;
			if (child)
			{
				var sr = child.GetComponent<SpriteRenderer>();
				if (sr && sr.sprite == needed) continue;
				Object.Destroy(child.gameObject);
			}

			GameObject prefab = FindPrefabBySprite(needed);
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

	private GameObject FindPrefabBySprite(Sprite sprite)
	{
		if (sprite == null) return null;
		foreach (var pf in foodPrefabs)
		{
			if (!pf) continue;
			var sr = pf.GetComponent<SpriteRenderer>();
			if (sr && sr.sprite == sprite) return pf;
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
	public Sprite OrderFoodRandomly()
	{
		if (foodPrefabs == null || foodPrefabs.Length == 0) return null;

		int randomIndex = Random.Range(0, foodPrefabs.Length);
		GameObject selected = foodPrefabs[randomIndex];
		Sprite sprite = selected.GetComponent<SpriteRenderer>()?.sprite;

		if (sprite != null && !IsMaking())
		{
			makingTargetSprite = sprite;
			makeRoutine = StartCoroutine(MakeFoodRoutine());
		}

		return sprite;
	}

	/// <summary>目前選中的餐點 GameObject（可用於互動）</summary>
	public GameObject GetCurrentDishObject()
	{
		return currentFoodTarget ? currentFoodTarget.gameObject : null;
	}

	#endregion
}
