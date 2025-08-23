using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 專責處理玩家的互動行為：撿餐、幫客人點餐、放餐
/// 由 PlayerMovement 在 InputInteract 時呼叫 Interact()
/// </summary>
public class PlayerInteraction : MonoBehaviour
{

	[Header("-------- Setting ---------")]
	[SerializeField] private int getFoodCount = 1;

	[Header("-------- Reference ---------")]
	[SerializeField] private PlayerMovement movement; // 由 Movement 綁定或 Inspector 指定
	[SerializeField] private HandItemUI handItemUI;   // 更新手上物品UI
	[SerializeField] private Transform handItemRoot; // 放玩家手上物件的父物件

	private List<Collider2D> currentChairTriggers;

	private bool isEnableUseDessert = false;


	private void Start()
	{
		currentChairTriggers = new List<Collider2D>();
	}
	/// 對外：在 InputInteract 時呼叫
	public bool Interact()
	{
		//Debug.Log("Interact");
		// 先撿餐，成功即結束
		if (TryGetFood()) return true;

		if (TryUseDessert()) return true;

		// 幫附近客人確認點餐，成功即結束
		if (TryConfirmOrderForNearbyGuests()) return true;

		// 嘗試放餐，成功即結束
		if (TryPullDownDish()) return true;

		return false;
	}

	/// 嘗試從目前可撿取的來源拿餐到手上
	private bool TryGetFood()
	{
		if (handItemUI) handItemUI.ChangeHandItemUI();

		GameObject currentFood = RoundManager.Instance.foodsGroupManager.GetCurrentDishObject();
		if (currentFood == null) return false;

		// 相同疊加至上限
		if (handItemRoot.childCount > 4) return false;

		Vector3 pos = handItemRoot.transform.position + new Vector3(0, -1, 0);
		if (handItemRoot.childCount > 0)
		{
			Sprite handItemSprite = handItemRoot.GetComponentInChildren<SpriteRenderer>().sprite;
			if (currentFood.GetComponent<SpriteRenderer>().sprite != handItemSprite)
			{
				// 手上餐點與選擇餐點不同
				foreach (Transform child in handItemRoot)
					Destroy(child.gameObject);
			}
			else pos = handItemRoot.GetChild(0).transform.position;
		}


		for (int i = 0; i < getFoodCount; i++)
		{
			GameObject newItem = Instantiate(currentFood, handItemRoot.position, Quaternion.identity);
			newItem.transform.SetParent(handItemRoot);
			newItem.GetComponent<Collider2D>().enabled = false;

			newItem.transform.SetAsFirstSibling();
			newItem.transform.position = pos + new Vector3(0, 1, 0);
		}

		if (handItemUI) handItemUI.ChangeHandItemUI();
		return true;
	}

	/// 嘗試幫附近座位上的客人確認點餐
	private bool TryConfirmOrderForNearbyGuests()
	{
		//Debug.Log("TryConfirmOrderForNearbyGuests");
		// 從目前偵測到的椅子 trigger 找客人（NormalGuestController 是坐下後被設為椅子子物件）
		foreach (var chair in currentChairTriggers)
			if (RoundManager.Instance.chairGroupManager.ConfirmOrderChair(chair.transform))
				return true;

		return false;
	}

	/// 嘗試把手上第一個物品放到附近任一張椅子
	public bool TryPullDownDish()
	{
		//Debug.Log("TryPullDownDish");
		if (currentChairTriggers.Count > 0 && handItemRoot.childCount > 0)
		{
			GameObject item = handItemRoot.GetChild(0).gameObject;
			foreach (var chair in currentChairTriggers)
				if (RoundManager.Instance.chairGroupManager.PullDownChairItem(chair.transform, item))
					return true;
		}

		return false;
	}

	private bool TryUseDessert()
	{
		if (!isEnableUseDessert) return false;
		return RoundManager.Instance.foodsGroupManager.UseDessert();
	}

	// ===== 椅子觸發維護 =====
	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Chair"))
		{
			if (handItemRoot && handItemRoot.childCount > 0)
			{
				GameObject item = handItemRoot.GetChild(0).gameObject;
				RoundManager.Instance.chairGroupManager.EnableInteracSignal(other.transform, item, true);
			}
		}
	}
	void OnTriggerStay2D(Collider2D trigger)
	{
		if (trigger.CompareTag("Chair"))
		{
			if (!currentChairTriggers.Contains(trigger))
				currentChairTriggers.Add(trigger);
		}
	}
	void OnTriggerExit2D(Collider2D trigger)
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
}
