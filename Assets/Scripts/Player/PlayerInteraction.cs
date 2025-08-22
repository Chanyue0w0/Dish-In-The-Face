using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 專責處理玩家的互動行為：撿餐、幫客人點餐、放餐
/// 由 PlayerMovement 在 InputInteract 時呼叫 Interact()
/// </summary>
public class PlayerInteraction : MonoBehaviour
{

	[Header("-------- Setting ---------")]
	[SerializeField] private int holdItemCount = 10;

	[Header("-------- Reference ---------")]
	[SerializeField] private PlayerMovement movement; // 由 Movement 綁定或 Inspector 指定
	[SerializeField] private HandItemUI handItemUI;   // 更新手上物品UI
	[SerializeField] private GameObject handItemRoot; // 放玩家手上物件的父物件

	private List<Collider2D> currentChairTriggers = new List<Collider2D>();


	/// 初始化綁定（可由 PlayerMovement 呼叫）
	public void BindMovement(PlayerMovement mv) => movement = mv;
	public void SetHandItemUI(HandItemUI ui) => handItemUI = ui;
	public void SetHandItemRoot(GameObject root) => handItemRoot = root;

	/// 對外：在 InputInteract 時呼叫
	public void Interact()
	{
		Debug.Log("Interact");
		// 先撿餐，成功即結束
		if (TryGetFood()) return;

		// 幫附近客人確認點餐，成功即結束
		if (TryConfirmOrderForNearbyGuests()) return;

		// 嘗試放餐，成功即結束
		if (TryPullDownDish()) return;
	}

	/// 嘗試從目前可撿取的來源拿餐到手上
	private bool TryGetFood()
	{
		if (handItemUI) handItemUI.ChangeHandItemUI();

		GameObject currentFood = RoundManager.Instance.foodsGroupManager.GetCurrentDishObject();
		if (currentFood != null)
		{
			foreach (Transform child in handItemRoot.transform)
				Destroy(child.gameObject);

			for (int i = 0; i < holdItemCount; i++)
			{
				GameObject newItem = Instantiate(currentFood, handItemRoot.transform.position, Quaternion.identity);
				newItem.transform.SetParent(handItemRoot.transform);
				newItem.GetComponent<Collider2D>().enabled = false;
			}
			return true;
		}

		return false;
	}

	/// 嘗試幫附近座位上的客人確認點餐
	private bool TryConfirmOrderForNearbyGuests()
	{
		Debug.Log("TryConfirmOrderForNearbyGuests");
		// 從目前偵測到的椅子 trigger 找客人（NormalGuestController 是坐下後被設為椅子子物件）
		foreach (var chair in currentChairTriggers)
			if (RoundManager.Instance.chairGroupManager.ConfirmOrderChair(chair.transform))
				return true;

		return false;
	}

	/// 嘗試把手上第一個物品放到附近任一張椅子
	public bool TryPullDownDish()
	{
		Debug.Log("TryPullDownDish");
		if (currentChairTriggers.Count > 0 && handItemRoot.transform.childCount > 0)
		{
			GameObject item = handItemRoot.transform.GetChild(0).gameObject;
			foreach (var chair in currentChairTriggers)
				if (RoundManager.Instance.chairGroupManager.PullDownChairItem(chair.transform, item))
					return true;
		}

		return false;
	}


	// ===== 椅子觸發維護 =====
	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Chair"))
		{
			if (handItemRoot && handItemRoot.transform.childCount > 0)
			{
				GameObject item = handItemRoot.transform.GetChild(0).gameObject;
				RoundManager.Instance.chairGroupManager.EnableInteracSignal(other.transform, item, true);
			}
		}
	}
	void OnTriggerStay2D(Collider2D other)
	{
		if (other.CompareTag("Chair"))
		{
			if (!currentChairTriggers.Contains(other))
				currentChairTriggers.Add(other);
		}
	}
	void OnTriggerExit2D(Collider2D other)
	{
		if (other.CompareTag("Chair"))
		{
			RoundManager.Instance.chairGroupManager.EnableInteracSignal(other.transform, null, false);
			if (currentChairTriggers.Contains(other))
				currentChairTriggers.Remove(other);
		}
	}
}
