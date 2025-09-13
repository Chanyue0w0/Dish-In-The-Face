using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles player interactions: picking up food, serving guests, placing dishes
/// Called by PlayerMovement when InputInteract is triggered
/// </summary>
public class PlayerInteraction : MonoBehaviour
{

	[Header("-------- Setting ---------")]
	[SerializeField] private int getFoodCount = 1;
	// [SerializeField] private bool isGetOneFood = true;

	[Header("-------- Reference ---------")]
	[SerializeField] private PlayerMovement movement; // Reference to Movement component
	[SerializeField] private HandItemUI handItemUI;   // Updates hand item UI
	[SerializeField] private Transform handItemRoot; // Parent for held items

	private List<Collider2D> currentChairTriggers;

	private bool isEnableUseDessert ;

	private void Start()
	{
		isEnableUseDessert = true;
		currentChairTriggers = new List<Collider2D>();

		// 開始時依照「由上到下」把手上物品排好
		ArrangeHandItemsTopToBottom();
	}

	/// External: Called on InputInteract
	public bool Interact()
	{
		//Debug.Log("Interact");
		// Try to pick up food first
		if (TryGetFood()) return true;

		if (TryUseDessert()) return true;

		// Try to confirm orders for nearby guests
		if (TryConfirmOrderForNearbyGuests()) return true;

		// Try to place dish on table
		if (TryPullDownDish()) return true;

		return false;
	}

	/// Try to pick up food from current serving area
	private bool TryGetFood()
	{
		GameObject currentFood = RoundManager.Instance.foodsGroupManager.GetCurrentDishObject();
		if (currentFood == null) return false;

		// Max 4 items on hand
		if (handItemRoot.childCount > 4) return false;

		Vector3 pos = handItemRoot.transform.position + new Vector3(0, -1, 0);
		if (handItemRoot.childCount > 0)
		{
			Sprite handItemSprite = handItemRoot.GetComponentInChildren<SpriteRenderer>().sprite;
			if (currentFood.GetComponent<SpriteRenderer>().sprite != handItemSprite)
			{
				// 手上物件和要拿的食物類型不同 → 清空手上物件
				foreach (Transform child in handItemRoot)
					Destroy(child.gameObject);
			}
			else
			{
				// 類型相同 → 以目前第一個物件的位置當成基準
				pos = handItemRoot.GetChild(0).transform.position;
			}
		}

		for (var i = 0; i < getFoodCount; i++)
		{
			// GameObject newItem = isGetOneFood ? currentFood : Instantiate(currentFood, handItemRoot.position, Quaternion.identity);
			GameObject newItem = currentFood;
			newItem.transform.SetParent(handItemRoot);
			newItem.GetComponent<Collider2D>().enabled = false;

			newItem.transform.SetAsFirstSibling();
			newItem.transform.position = pos + new Vector3(0, 1, 0);
		}

		// 重新依「由上到下」排一次
		ArrangeHandItemsTopToBottom();

		if (handItemUI) handItemUI.ChangeHandItemUI();
		return true;
	}

	/// Confirm orders for nearby guests
	private bool TryConfirmOrderForNearbyGuests()
	{
		//Debug.Log("TryConfirmOrderForNearbyGuests");
		// Check current chair triggers for guests
		foreach (var chair in currentChairTriggers)
			if (RoundManager.Instance.chairGroupManager.ConfirmOrderChair(chair.transform))
				return true;

		return false;
	}

	/// Place first item on a nearby chair
	public bool TryPullDownDish()
	{
		//Debug.Log("TryPullDownDish");
		if (currentChairTriggers.Count <= 0 || handItemRoot.childCount <= 0) return false;
		
		GameObject item = handItemRoot.GetChild(0).gameObject;
		foreach (var chair in currentChairTriggers)
			if (RoundManager.Instance.chairGroupManager.PullDownChairItem(chair.transform, item))
				return true;

		return false;
	}

	private bool TryUseDessert()
	{
		if (!isEnableUseDessert) return false;
		return RoundManager.Instance.foodsGroupManager.dessertController.UseDessert();
	}

	// ===== Chair trigger handling =====
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Chair"))
		{
			GameObject item = null;
			if (handItemRoot && handItemRoot.childCount > 0)
				item = handItemRoot.GetChild(0).gameObject;
			RoundManager.Instance.chairGroupManager.EnableInteracSignal(other.transform, item, true);
		}
	}

	private void OnTriggerStay2D(Collider2D trigger)
	{
		if (trigger.CompareTag("Chair"))
		{
			if (!currentChairTriggers.Contains(trigger))
				currentChairTriggers.Add(trigger);
		}
	}

	private void OnTriggerExit2D(Collider2D trigger)
	{
		if (trigger.CompareTag("Chair"))
		{
			RoundManager.Instance.chairGroupManager.EnableInteracSignal(trigger.transform, null, false);
			if (currentChairTriggers.Contains(trigger))
				currentChairTriggers.Remove(trigger);
		}
	}

	private void OnCollisionStay2D(Collision2D collision)
	{
		if (collision.collider.CompareTag("Dessert"))
		{
			isEnableUseDessert = true;
		}
	}
	private void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.collider.CompareTag("Dessert"))
		{
			isEnableUseDessert = false;
		}
	}

	/// <summary>
	/// 依照「由上到下」把手上物件排列：
	/// 以 handItemRoot 的上方為起點，等距往下排。child index 小的在上面。
	/// </summary>
	private void ArrangeHandItemsTopToBottom()
	{
		if (handItemRoot == null) return;

		int childCount = handItemRoot.childCount;
		if (childCount == 0) return;

		const float verticalStep = 1f;

		// 最上方位置 = root 往上 (childCount - 1) * step
		Vector3 topPos = handItemRoot.position + new Vector3(0f, (childCount - 1) * verticalStep, 0f);

		for (int i = 0; i < childCount; i++)
		{
			Transform child = handItemRoot.GetChild(i);

			// 依序往下排，最後一個剛好落在 root
			Vector3 targetPos = topPos + new Vector3(0f, -i * verticalStep, 0f);
			child.position = targetPos;

			// 保持 Hierarchy 中 index 與排列一致
			child.SetSiblingIndex(i);
		}
	}
}
