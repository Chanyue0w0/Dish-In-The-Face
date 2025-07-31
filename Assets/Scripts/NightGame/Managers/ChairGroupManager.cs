using System.Collections.Generic;
using UnityEngine;

public class ChairGroupManager : MonoBehaviour
{
	[Header("Chair List")]
	[SerializeField] private List<Transform> chairList; // �Ҧ��Ȥl��m
	[SerializeField] private Transform guestEnterPoistion;

	[Header("Reference")]
	[SerializeField] private GameObject coinPrefab;
	[SerializeField] private RoundManager roundManager;
	private HashSet<Transform> occupiedChairs = new HashSet<Transform>(); // ���b�Q�ϥΪ��Ȥl���X

	private void Awake()
	{
		chairList.Clear();

		GameObject[] allChairs = GameObject.FindGameObjectsWithTag("Chair");
		foreach (GameObject chairObj in allChairs)
		{
			chairList.Add(chairObj.transform);
		}
		SortChairList(); // �Ȥl�H�a�񭫥��I����
	}


	private void Start()
	{
		foreach (Transform obj in chairList)
		{
			ClearChairItem(obj);
		}
	}

	///// �H���M��@�ӪŪ��Ȥl�üаO���w���ΡC�䤣��h�^�� null�C
	//public Transform FindEmptyChair()
	//{
	//	// �إߤ@�ӲM��Ӧs��Ҧ��|���Q���Ϊ��Ȥl
	//	List<Transform> availableChairs = new List<Transform>();

	//	foreach (Transform chair in chairList)
	//	{
	//		if (!occupiedChairs.Contains(chair))
	//			availableChairs.Add(chair);
	//	}

	//	// �Y�S���ŴȤl�A�^�� null
	//	if (availableChairs.Count == 0)
	//		return null;

	//	// �H������䤤�@�i�Ȥl
	//	Transform selectedChair = availableChairs[Random.Range(0, availableChairs.Count)];

	//	// �аO���w����
	//	occupiedChairs.Add(selectedChair);

	//	return selectedChair;
	//}

	/// �̧ǴM��@�ӪŪ��Ȥl�üаO���w���ΡC�䤣��h�^�� null�C
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

		// �Y�S���ŴȤl�A�^�� null
		return null;
	}

	/// ��ȤH���u������Ȥl�C
	public void ReleaseChair(Transform targetChair)
	{
		if (occupiedChairs.Contains(targetChair))
		{
			occupiedChairs.Remove(targetChair);
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
			//handItem.transform.SetParent(chairItem.transform); // �\�I�q���a��W����l�W
			//handItem.transform.localPosition = Vector3.zero;

			// �ͦ� handItem ���ƻs�~�]Instantiate �@���s����^
			GameObject newItem = Instantiate(handItem); // �ƻs�\�I����l�W�A���a��W�\�I����
			newItem.transform.localScale = handItem.transform.lossyScale;
			newItem.transform.SetParent(chairItem.transform);
			newItem.transform.localPosition = Vector3.zero;

			// �W�\���\�W�[����
			roundManager.PullDownDishSuccess();
		}
	}

	public void PullDownCoin(Transform chair, int coinCount)
	{
		if (chair == null) return;
		Transform chairItem = chair.transform.GetChild(0);

		GameObject coinObj = Instantiate(coinPrefab);
		coinObj.GetComponent<CoinOnTable>().SetCoinCount(coinCount); // �]�w�����ƶq�� 10
		coinObj.transform.localScale = coinPrefab.transform.lossyScale;
		coinObj.transform.SetParent(chairItem.transform);
		coinObj.transform.localPosition = Vector3.zero;
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

	public bool IsChairccupied(Transform chair)
	{
		return occupiedChairs.Contains(chair);
	}

	private void SortChairList()
	{
		// ���ӶZ�� spawnPoistion ������Ƨǡ]�Ѫ�컷�^
		chairList.Sort((a, b) =>
			Vector3.Distance(a.position, guestEnterPoistion.position)
			.CompareTo(Vector3.Distance(b.position, guestEnterPoistion.position))
		);
	}
}
