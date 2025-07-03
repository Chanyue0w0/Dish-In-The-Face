using System.Collections.Generic;
using UnityEngine;

public class ChairGroupManager : MonoBehaviour
{
	[Header("Chair List")]
	[SerializeField] private List<Transform> chairList; // �Ҧ��Ȥl��m
	private HashSet<Transform> occupiedChairs = new HashSet<Transform>(); // ���b�Q�ϥΪ��Ȥl���X

	private void Start()
	{
		foreach (Transform obj in chairList)
		{
			ClearChairItem(obj);
		}
	}

	/// �H���M��@�ӪŪ��Ȥl�üаO���w���ΡC�䤣��h�^�� null�C
	public Transform FindEmptyChair()
	{
		// �إߤ@�ӲM��Ӧs��Ҧ��|���Q���Ϊ��Ȥl
		List<Transform> availableChairs = new List<Transform>();

		foreach (Transform chair in chairList)
		{
			if (!occupiedChairs.Contains(chair))
				availableChairs.Add(chair);
		}

		// �Y�S���ŴȤl�A�^�� null
		if (availableChairs.Count == 0)
			return null;

		// �H������䤤�@�i�Ȥl
		Transform selectedChair = availableChairs[Random.Range(0, availableChairs.Count)];

		// �аO���w����
		occupiedChairs.Add(selectedChair);

		return selectedChair;
	}

	/// ��ȤH���u������Ȥl�C
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

		// �^���w�W�\
		if (npc.IsReceiveFood(foodSprite))
		{
			// ��m�\�I
			handItem.transform.SetParent(chairItem.transform);
			handItem.transform.localPosition = Vector3.zero;
		}
	}

	public void ClearChairItem(Transform chair)
	{
		if (chair == null) return;

		Transform parentItem = chair.transform.GetChild(0);

		// �R���Ĥ@�Ӥl����]�p�G���^
		if (parentItem.childCount > 0)
		{
			Destroy(parentItem.GetChild(0).gameObject);
		}
	}
}
