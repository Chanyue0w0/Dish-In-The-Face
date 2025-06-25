using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChairGroupManager : MonoBehaviour
{

	[Header("Chair List")]
	[SerializeField] private Transform[] chairList; // 所有椅子位置
	private HashSet<Transform> occupiedChairs = new HashSet<Transform>(); // 正在被使用的椅子集合

	/// 尋找一個空的椅子並標記為已佔用。找不到則回傳 null。
	public Transform FindEmptyChair()
	{
		foreach (Transform chair in chairList)
		{
			if (!occupiedChairs.Contains(chair))
			{
				occupiedChairs.Add(chair);
				return chair;
			}
		}
		return null;
	}

	/// 當客人離席時釋放椅子。
	public void ReleaseChair(Transform targetChair)
	{
		if (occupiedChairs.Contains(targetChair))
			occupiedChairs.Remove(targetChair);
	}
}
