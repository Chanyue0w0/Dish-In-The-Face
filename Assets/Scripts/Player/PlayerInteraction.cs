using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �M�d�B�z���a�����ʦ欰�G���\�B���ȤH�I�\�B���\
/// �� PlayerMovement �b InputInteract �ɩI�s Interact()
/// </summary>
public class PlayerInteraction : MonoBehaviour
{

	[Header("-------- Setting ---------")]
	[SerializeField] private int holdItemCount = 10;

	[Header("-------- Reference ---------")]
	[SerializeField] private PlayerMovement movement; // �� Movement �j�w�� Inspector ���w
	[SerializeField] private HandItemUI handItemUI;   // ��s��W���~UI
	[SerializeField] private GameObject handItemRoot; // �񪱮a��W���󪺤�����

	private List<Collider2D> currentChairTriggers = new List<Collider2D>();


	/// ��l�Ƹj�w�]�i�� PlayerMovement �I�s�^
	public void BindMovement(PlayerMovement mv) => movement = mv;
	public void SetHandItemUI(HandItemUI ui) => handItemUI = ui;
	public void SetHandItemRoot(GameObject root) => handItemRoot = root;

	/// ��~�G�b InputInteract �ɩI�s
	public void Interact()
	{
		Debug.Log("Interact");
		// �����\�A���\�Y����
		if (TryGetFood()) return;

		// ������ȤH�T�{�I�\�A���\�Y����
		if (TryConfirmOrderForNearbyGuests()) return;

		// ���թ��\�A���\�Y����
		if (TryPullDownDish()) return;
	}

	/// ���ձq�ثe�i�ߨ����ӷ����\���W
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

	/// ����������y��W���ȤH�T�{�I�\
	private bool TryConfirmOrderForNearbyGuests()
	{
		Debug.Log("TryConfirmOrderForNearbyGuests");
		// �q�ثe�����쪺�Ȥl trigger ��ȤH�]NormalGuestController �O���U��Q�]���Ȥl�l����^
		foreach (var chair in currentChairTriggers)
			if (RoundManager.Instance.chairGroupManager.ConfirmOrderChair(chair.transform))
				return true;

		return false;
	}

	/// ���է��W�Ĥ@�Ӫ��~��������@�i�Ȥl
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


	// ===== �ȤlĲ�o���@ =====
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
