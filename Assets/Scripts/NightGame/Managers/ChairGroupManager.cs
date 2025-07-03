using System.Collections.Generic;
using UnityEngine;

public class ChairGroupManager : MonoBehaviour
{
	[Header("Chair List")]
	[SerializeField] private List<Transform> chairList; // 所有椅子位置
	private HashSet<Transform> occupiedChairs = new HashSet<Transform>(); // 正在被使用的椅子集合

	private void Start()
	{
		foreach (Transform obj in chairList)
		{
			ClearChairItem(obj);
		}
	}

	/// 隨機尋找一個空的椅子並標記為已佔用。找不到則回傳 null。
	public Transform FindEmptyChair()
	{
		// 建立一個清單來存放所有尚未被佔用的椅子
		List<Transform> availableChairs = new List<Transform>();

		foreach (Transform chair in chairList)
		{
			if (!occupiedChairs.Contains(chair))
				availableChairs.Add(chair);
		}

		// 若沒有空椅子，回傳 null
		if (availableChairs.Count == 0)
			return null;

		// 隨機選取其中一張椅子
		Transform selectedChair = availableChairs[Random.Range(0, availableChairs.Count)];

		// 標記為已佔用
		occupiedChairs.Add(selectedChair);

		return selectedChair;
	}

	/// 當客人離席時釋放椅子。
	public void ReleaseChair(Transform targetChair)
	{
		if (occupiedChairs.Contains(targetChair))
		{
			occupiedChairs.Remove(targetChair);
			ClearChairItem(targetChair);
		}
	}

	public void PullDownChairItem(Transform chair, GameObject handItem)
	{
		Transform chairItem = chair.transform.GetChild(0);
		Sprite foodSprite = handItem.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;

		if (chair.childCount < 2) return;
		NormalGuestController npc = chair.GetChild(1).GetComponent<NormalGuestController>();

		// 回報已上餐
		if (npc.IsReceiveFood(foodSprite))
		{
			// 放置餐點
			handItem.transform.SetParent(chairItem.transform);
			handItem.transform.localPosition = Vector3.zero;
		}
	}

	public void ClearChairItem(Transform chair)
	{
		if (chair == null) return;

		Transform parentItem = chair.transform.GetChild(0);

		// 刪除第一個子物件（如果有）
		if (parentItem.childCount > 0)
		{
			Destroy(parentItem.GetChild(0).gameObject);
		}
	}
}
