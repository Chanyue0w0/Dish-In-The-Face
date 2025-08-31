using System.Collections.Generic;
using UnityEngine;

public class ChairGroupManager : MonoBehaviour
{
	#region Inspector

	[Header("Chair List")]
	[SerializeField] private List<Transform> chairList;            // �Ҧ��Ȥl��m�]�|�b Awake �H Tag=Chair ���s�`���ñƧǡ^
	[SerializeField] private Transform guestEnterPoistion;         // �̶Z�����I�Ƨǡ]�O���R�W�H�K�ǦC�Ư}�a�^

	[Header("Reference")]
	[SerializeField] private GameObject coinPrefab;                // ��W���� Prefab

	#endregion


	#region Runtime State / Collections

	private HashSet<Transform> occupiedChairs;                     // ���b�Q�ϥΪ��Ȥl���X
	private List<NormalGuestController> guestsOrderList = new List<NormalGuestController>(); // �w�U��]�ε��ݤW�\�^���ȤH�M��

	#endregion


	#region Unity Events

	private void Awake()
	{
		chairList.Clear();
		occupiedChairs = new HashSet<Transform>();

		// �H Tag ���o�Ҧ��Ȥl
		GameObject[] allChairs = GameObject.FindGameObjectsWithTag("Chair");
		foreach (GameObject chairObj in allChairs)
		{
			chairList.Add(chairObj.transform);
		}

		// �̾a�񭫥��I�Ƨ�
		SortChairList();
	}

	private void Start()
	{
		// �M�ũҦ��Ȥl�ୱ����
		foreach (Transform obj in chairList)
		{
			ClearChairItem(obj);
		}
	}

	#endregion


	#region Public API - �d�� / ����Ȥl

	/// <summary>
	/// �H���M��@�ӪŪ��Ȥl�üаO���w���Ρ]�䤣��h�^�� null�^�C
	/// �|�P�ɧ�ӫȤH�[�J guestsOrderList�C
	/// </summary>
	public Transform FindEmptyChair(NormalGuestController normalGuest)
	{
		// �`�������δȤl
		List<Transform> availableChairs = new List<Transform>();
		foreach (Transform chair in chairList)
		{
			if (!occupiedChairs.Contains(chair))
				availableChairs.Add(chair);
		}

		if (availableChairs.Count == 0)
			return null;

		// �H���D��
		Transform selectedChair = availableChairs[Random.Range(0, availableChairs.Count)];

		// �����q��ȤH�P���μаO
		occupiedChairs.Add(selectedChair);
		return selectedChair;
	}

	/// <summary>
	/// �ȤH���u������Ȥl���ΡC
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
	/// �߰ݴȤl�O�_�w�Q���ΡC
	/// </summary>
	public bool IsChairccupied(Transform chair)
	{
		return chair != null && occupiedChairs.Contains(chair);
	}

	#endregion


	#region Public API - �W�\ / ������� / ����

	/// <summary>
	/// �ҥ�/�����Ȧ�W�����ʴ��ܹϥܡ]�⪱�a��W���~�� Sprite �ǵ��ȤH��ܡ^�C
	/// </summary>
	public void EnableInteracSignal(Transform chair, GameObject handItem, bool onEnable)
	{
		if (chair == null || chair.childCount < 2) return;

		Sprite foodSprite = handItem?.transform?.GetComponent<SpriteRenderer>()?.sprite;
		NormalGuestController npc = chair.GetChild(1).GetComponent<NormalGuestController>();
		npc?.EnableInteractIcon(foodSprite, onEnable);
	}

	/// <summary>
	/// ���ձN���a��W�\�I���Ȧ�ୱ�W�F�Y�ȤH�T�{���쥿�T�\�I�h�^�����\�A��Ĳ�o���׵��y�{�C
	/// </summary>
	public bool PullDownChairItem(Transform chair, GameObject handItem)
	{
		if (chair == null || handItem == null) return false;
		if (chair.childCount < 2) return false;

		Transform chairItem = chair.transform.GetChild(0);
		FoodStatus foodStatus = handItem.transform.GetComponent<FoodStatus>();

		NormalGuestController npc = chair.GetComponentInChildren<NormalGuestController>();
		if (npc == null || foodStatus == null) return false;

		// �^���w�W�\�]�P�_�O�_�O�ӫȤH�n�������^
		if (npc.IsReceiveFood(foodStatus))
		{
			AudioManager.Instance.PlayOneShot(FMODEvents.Instance.pullDownDish, transform.position);

			// ��m�\�I��ୱ�]�⪱�a��W�����󲾨��W�^
			handItem.transform.SetParent(chairItem.transform);
			handItem.transform.localPosition = Vector3.zero;

			// �q�q��ȤH�M�沾��
			RemovOrderGuest(npc);

			// �W�\���\�G�W�[���׵�
			RoundManager.Instance.PullDownDishSuccess();
			return true;
		}

		return false;
	}

	/// <summary>
	/// ���ȤH�T�{�I��]�� WaitingOrder ���A�|���\�^�C
	/// </summary>
	public bool ConfirmOrderChair(Transform chair)
	{
		if (chair == null) return false;

		NormalGuestController guest = chair.GetComponentInChildren<NormalGuestController>();
		if (guest == null || !guest.isActiveAndEnabled) return false;

		return guest.ConfirmOrderByPlayer();
	}

	/// <summary>
	/// �b�Ȧ�ୱ��m��������]�ó]�w�����ƶq�^�C
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
	/// �M�ŴȦ�ୱ�W���Ĥ@�Ӥl����]�Y���^�C
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


	#region Public API - �ȤH�@�� / �q��M��

	/// <summary>
	/// ���Ҧ��Ȧ�W���۪��ȤH���]�@�߭ȡC
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
	/// ���o�ثe guestsOrderList�]�w�U��ε��ݤW�\���ȤH�^�C
	/// </summary>
	public List<NormalGuestController> GetGuestsOrderList()
	{
		return guestsOrderList;
	}

	/// <summary>
	/// �[�J���w�ȤH�� guestsOrderList �F�Y���s�b�h��Xĵ�i�C
	/// </summary>
	public void AddOrderGuest(NormalGuestController normalGuest)
	{
		if (normalGuest == null) return;

		if (guestsOrderList.Contains(normalGuest))
		{
			//Debug.Log($"�w�s�b {normalGuest.name} �b guestsOrderList");
			return;
		}
		guestsOrderList.Add(normalGuest);
	}

	/// <summary>
	/// �q guestsOrderList �������w�ȤH�F�Y���s�b�h��Xĵ�i�C
	/// </summary>
	public void RemovOrderGuest(NormalGuestController normalGuest)
	{
		if (normalGuest == null) return;

		if (guestsOrderList.Contains(normalGuest))
		{
			guestsOrderList.Remove(normalGuest);
			//Debug.Log($"�w�����ȤH {normalGuest.name} �q guestsOrderList");
			return;
		}

		//Debug.Log($"�n�������ȤH {normalGuest?.name} ���b guestsOrderList ��");
	}
	#endregion


	#region Private Helpers

	/// <summary>
	/// �̴Ȥl�� guestEnterPoistion ���Z���Ѫ�컷�ƧǡC
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
