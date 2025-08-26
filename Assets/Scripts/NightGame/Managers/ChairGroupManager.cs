using System.Collections.Generic;
using UnityEngine;

public class ChairGroupManager : MonoBehaviour
{
	#region Inspector

	[Header("Chair List")]
	[SerializeField] private List<Transform> chairList;            // 所有椅子位置（會在 Awake 以 Tag=Chair 重新蒐集並排序）
	[SerializeField] private Transform guestEnterPoistion;         // 依距離此點排序（保持命名以免序列化破壞）

	[Header("Reference")]
	[SerializeField] private GameObject coinPrefab;                // 桌上金幣 Prefab

	#endregion


	#region Runtime State / Collections

	private HashSet<Transform> occupiedChairs;                     // 正在被使用的椅子集合
	private List<NormalGuestController> guestsOrderList = new List<NormalGuestController>(); // 已下單（或等待上餐）的客人清單

	#endregion


	#region Unity Events

	private void Awake()
	{
		chairList.Clear();
		occupiedChairs = new HashSet<Transform>();

		// 以 Tag 取得所有椅子
		GameObject[] allChairs = GameObject.FindGameObjectsWithTag("Chair");
		foreach (GameObject chairObj in allChairs)
		{
			chairList.Add(chairObj.transform);
		}

		// 依靠近重生點排序
		SortChairList();
	}

	private void Start()
	{
		// 清空所有椅子桌面物件
		foreach (Transform obj in chairList)
		{
			ClearChairItem(obj);
		}
	}

	#endregion


	#region Public API - 查找 / 釋放椅子

	/// <summary>
	/// 隨機尋找一個空的椅子並標記為已佔用（找不到則回傳 null）。
	/// 會同時把該客人加入 guestsOrderList。
	/// </summary>
	public Transform FindEmptyChair(NormalGuestController normalGuest)
	{
		// 蒐集未佔用椅子
		List<Transform> availableChairs = new List<Transform>();
		foreach (Transform chair in chairList)
		{
			if (!occupiedChairs.Contains(chair))
				availableChairs.Add(chair);
		}

		if (availableChairs.Count == 0)
			return null;

		// 隨機挑選
		Transform selectedChair = availableChairs[Random.Range(0, availableChairs.Count)];

		// 紀錄訂單客人與佔用標記
		occupiedChairs.Add(selectedChair);
		return selectedChair;
	}

	/// <summary>
	/// 客人離席時釋放椅子佔用。
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
	/// 詢問椅子是否已被佔用。
	/// </summary>
	public bool IsChairccupied(Transform chair)
	{
		return chair != null && occupiedChairs.Contains(chair);
	}

	#endregion


	#region Public API - 上餐 / 互動顯示 / 金幣

	/// <summary>
	/// 啟用/關閉椅位上的互動提示圖示（把玩家手上物品的 Sprite 傳給客人顯示）。
	/// </summary>
	public void EnableInteracSignal(Transform chair, GameObject handItem, bool onEnable)
	{
		if (chair == null || chair.childCount < 2) return;

		Sprite foodSprite = handItem?.transform?.GetComponent<SpriteRenderer>()?.sprite;
		NormalGuestController npc = chair.GetChild(1).GetComponent<NormalGuestController>();
		npc?.EnableInteractIcon(foodSprite, onEnable);
	}

	/// <summary>
	/// 嘗試將玩家手上餐點放到椅位桌面上；若客人確認收到正確餐點則回報成功，並觸發熱度等流程。
	/// </summary>
	public bool PullDownChairItem(Transform chair, GameObject handItem)
	{
		if (chair == null || handItem == null) return false;
		if (chair.childCount < 2) return false;

		Transform chairItem = chair.transform.GetChild(0);
		Sprite foodSprite = handItem.transform.GetComponent<SpriteRenderer>()?.sprite;

		NormalGuestController npc = chair.GetComponentInChildren<NormalGuestController>();
		if (npc == null || foodSprite == null) return false;

		// 回報已上餐（判斷是否是該客人要的食物）
		if (npc.IsReceiveFood(foodSprite))
		{
			AudioManager.Instance.PlayOneShot(FMODEvents.Instance.pullDownDish, transform.position);

			// 放置餐點到桌面（把玩家手上的物件移到桌上）
			handItem.transform.SetParent(chairItem.transform);
			handItem.transform.localPosition = Vector3.zero;

			// 從訂單客人清單移除
			RemovOrderGuest(npc);

			// 上餐成功：增加熱度等
			RoundManager.Instance.PullDownDishSuccess();
			return true;
		}

		return false;
	}

	/// <summary>
	/// 幫客人確認點單（僅 WaitingOrder 狀態會成功）。
	/// </summary>
	public bool ConfirmOrderChair(Transform chair)
	{
		if (chair == null) return false;

		NormalGuestController guest = chair.GetComponentInChildren<NormalGuestController>();
		if (guest == null || !guest.isActiveAndEnabled) return false;

		return guest.ConfirmOrderByPlayer();
	}

	/// <summary>
	/// 在椅位桌面放置金幣物件（並設定金幣數量）。
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
	/// 清空椅位桌面上的第一個子物件（若有）。
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


	#region Public API - 客人耐心 / 訂單清單

	/// <summary>
	/// 讓所有椅位上坐著的客人重設耐心值。
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
	/// 取得目前 guestsOrderList（已下單或等待上餐的客人）。
	/// </summary>
	public List<NormalGuestController> GetGuestsOrderList()
	{
		return guestsOrderList;
	}

	/// <summary>
	/// 加入指定客人到 guestsOrderList ；若不存在則輸出警告。
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
	/// 從 guestsOrderList 移除指定客人；若不存在則輸出警告。
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

		//Debug.Log($"要移除的客人 {normalGuest?.name} 不在 guestsOrderList 裡");
	}
	#endregion


	#region Private Helpers

	/// <summary>
	/// 依椅子到 guestEnterPoistion 的距離由近到遠排序。
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
