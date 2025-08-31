using System.Collections.Generic;
using UnityEngine;

public class ChairGroupManager : MonoBehaviour
{
	#region Inspector

	[Header("Chair List")]
	[SerializeField] private List<Transform> chairList;            // 所有椅子位置（會在 Awake 透過 Tag=Chair 搜尋並加入清單）
	[SerializeField] private Transform guestEnterPoistion;         // 客人進場入口位置（用來計算椅子距離排序）

	[Header("Reference")]
	[SerializeField] private GameObject coinPrefab;                // 金幣物件 Prefab

	#endregion


	#region Runtime State / Collections

	private HashSet<Transform> occupiedChairs;                     // 正在被使用的椅子集合
	private List<NormalGuestController> guestsOrderList = new List<NormalGuestController>(); // 已下單（點餐成功）的客人清單

	#endregion


	#region Unity Events

	private void Awake()
	{
		chairList.Clear();
		occupiedChairs = new HashSet<Transform>();

		// 透過 Tag 取得所有椅子
		GameObject[] allChairs = GameObject.FindGameObjectsWithTag("Chair");
		foreach (GameObject chairObj in allChairs)
		{
			chairList.Add(chairObj.transform);
		}

		// 依據距離入口位置排序
		SortChairList();
	}

	private void Start()
	{
		// 清空所有椅子上的物件
		foreach (Transform obj in chairList)
		{
			ClearChairItem(obj);
		}
	}

	#endregion


	#region Public API - 椅子 / 座位相關

	/// <summary>
	/// 隨機尋找一個空的椅子並回傳（若沒有則回傳 null）。
	/// 同時會將該客人加入 guestsOrderList。
	/// </summary>
	public Transform FindEmptyChair(NormalGuestController normalGuest)
	{
		// 篩選尚未被使用的椅子
		List<Transform> availableChairs = new List<Transform>();
		foreach (Transform chair in chairList)
		{
			if (!occupiedChairs.Contains(chair))
				availableChairs.Add(chair);
		}

		if (availableChairs.Count == 0)
			return null;

		// 隨機挑選一張椅子
		Transform selectedChair = availableChairs[Random.Range(0, availableChairs.Count)];

		// 標記為已被使用
		occupiedChairs.Add(selectedChair);
		return selectedChair;
	}

	/// <summary>
	/// 客人離開後釋放椅子。
	/// </summary>
	public void ReleaseChair(Transform targetChair)
	{
		if (targetChair == null) return;
		if (occupiedChairs.Contains(targetChair))
		{
			occupiedChairs.Remove(targetChair);
		}
	}

	/// <summary>
	/// 檢查椅子是否被佔用。
	/// </summary>
	public bool IsChairccupied(Transform chair)
	{
		return chair != null && occupiedChairs.Contains(chair);
	}

	#endregion


	#region Public API - 點餐 / 互動相關 / 金幣

	/// <summary>
	/// 啟用/停用椅子上的互動提示（例如顯示 Raw 食物圖示）。
	/// </summary>
	public void EnableInteracSignal(Transform chair, GameObject handItem, bool onEnable)
	{
		if (chair == null || chair.childCount < 2) return;

		Sprite foodSprite = handItem?.transform?.GetComponent<SpriteRenderer>()?.sprite;
		NormalGuestController npc = chair.GetChild(1).GetComponent<NormalGuestController>();
		npc?.EnableInteractIcon(foodSprite, onEnable);
	}

	/// <summary>
	/// 嘗試將玩家手上的餐點放到椅子上，若客人確認為正確餐點則回傳 true，否則 false。
	/// </summary>
	public bool PullDownChairItem(Transform chair, GameObject handItem)
	{
		if (chair == null || handItem == null) return false;
		if (chair.childCount < 2) return false;

		Transform chairItem = chair.transform.GetChild(0);
		FoodStatus foodStatus = handItem.transform.GetComponent<FoodStatus>();

		NormalGuestController npc = chair.GetComponentInChildren<NormalGuestController>();
		if (npc == null || foodStatus == null) return false;

		// 檢查是否為客人需求的餐點
		if (npc.IsReceiveFood(foodStatus))
		{
			AudioManager.Instance.PlayOneShot(FMODEvents.Instance.pullDownDish, transform.position);

			// 把餐點放到椅子前方的擺放點
			handItem.transform.SetParent(chairItem.transform);
			handItem.transform.localPosition = Vector3.zero;

			// 從點單列表移除該客人
			RemovOrderGuest(npc);

			// 成功送餐 → 更新分數或進度
			RoundManager.Instance.PullDownDishSuccess();
			return true;
		}

		return false;
	}

	/// <summary>
	/// 客人確認點餐（WaitingOrder 狀態下才可確認）。
	/// </summary>
	public bool ConfirmOrderChair(Transform chair)
	{
		if (chair == null) return false;

		NormalGuestController guest = chair.GetComponentInChildren<NormalGuestController>();
		if (guest == null || !guest.isActiveAndEnabled) return false;

		return guest.ConfirmOrderByPlayer();
	}

	/// <summary>
	/// 在椅子前方生成金幣物件，並設定數量。
	/// </summary>
	public void PullDownCoin(Transform chair, int coinCount)
	{
		if (chair == null || coinPrefab == null) return;

		Transform chairItem = chair.transform.GetChild(0);

		GameObject coinObj = Instantiate(coinPrefab);
		coinObj.GetComponent<CoinOnTable>()?.SetCoinCount(coinCount);
		coinObj.transform.localScale = coinPrefab.transform.lossyScale;
		coinObj.transform.SetParent(chairItem.transform);
		coinObj.transform.localPosition = Vector3.zero;
	}

	/// <summary>
	/// 清空椅子擺放點上的第一個物件（例如盤子）。
	/// </summary>
	public void ClearChairItem(Transform chair)
	{
		if (chair == null) return;

		Transform parentItem = chair.transform.GetChild(0);
		if (parentItem.childCount > 0)
		{
			Destroy(parentItem.GetChild(0).gameObject);
		}
	}

	#endregion


	#region Public API - 客人相關 / 訂單管理

	/// <summary>
	/// 重置所有客人的耐心值。
	/// </summary>
	public void ResetAllSetGuestsPatience()
	{
		foreach (Transform chair in chairList)
		{
			NormalGuestController guestController = chair.GetComponentInChildren<NormalGuestController>();
			if (guestController == null) continue;
			guestController.ResetPatience();
		}
	}

	/// <summary>
	/// 取得目前已下單的客人清單。
	/// </summary>
	public List<NormalGuestController> GetGuestsOrderList()
	{
		return guestsOrderList;
	}

	/// <summary>
	/// 將客人加入已下單清單。
	/// </summary>
	public void AddOrderGuest(NormalGuestController normalGuest)
	{
		if (normalGuest == null) return;

		if (guestsOrderList.Contains(normalGuest))
		{
			//Debug.Log($"已存在 {normalGuest.name} 在 guestsOrderList");
			return;
		}
		guestsOrderList.Add(normalGuest);
	}

	/// <summary>
	/// 從已下單清單移除客人。
	/// </summary>
	public void RemovOrderGuest(NormalGuestController normalGuest)
	{
		if (normalGuest == null) return;

		if (guestsOrderList.Contains(normalGuest))
		{
			guestsOrderList.Remove(normalGuest);
			//Debug.Log($"已移除客人 {normalGuest.name} 從 guestsOrderList");
			return;
		}

		//Debug.Log($"欲移除的客人 {normalGuest?.name} 不在 guestsOrderList 中");
	}
	#endregion


	#region Private Helpers

	/// <summary>
	/// 依據椅子到 guestEnterPoistion 的距離進行排序。
	/// </summary>
	private void SortChairList()
	{
		chairList.Sort((a, b) =>
			Vector3.Distance(a.position, guestEnterPoistion.position)
				.CompareTo(Vector3.Distance(b.position, guestEnterPoistion.position))
		);
	}

	#endregion
}
