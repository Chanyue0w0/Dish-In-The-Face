using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �M�d�B�z���a�����ʦ欰�G���\�B���ȤH�I�\�B���\
/// �� PlayerMovement �b InputInteract �ɩI�s Interact()
/// </summary>
public class PlayerInteraction : MonoBehaviour
{

	[Header("-------- Setting ---------")]
	[SerializeField] private int getFoodCount = 1;

	[Header("-------- Reference ---------")]
	[SerializeField] private PlayerMovement movement; // �� Movement �j�w�� Inspector ���w
	[SerializeField] private HandItemUI handItemUI;   // ��s��W���~UI
	[SerializeField] private Transform handItemRoot; // �񪱮a��W���󪺤�����

	private List<Collider2D> currentChairTriggers = new List<Collider2D>();

	/// ��~�G�b InputInteract �ɩI�s
	public bool Interact()
	{
		//Debug.Log("Interact");
		// �����\�A���\�Y����
		if (TryGetFood()) return true;

		// ������ȤH�T�{�I�\�A���\�Y����
		if (TryConfirmOrderForNearbyGuests()) return true;

		// ���թ��\�A���\�Y����
		if (TryPullDownDish()) return true;
		
		return false;
	}

	/// ���ձq�ثe�i�ߨ����ӷ����\���W
	private bool TryGetFood()
	{
		if (handItemUI) handItemUI.ChangeHandItemUI();

		GameObject currentFood = RoundManager.Instance.foodsGroupManager.GetCurrentDishObject();
		if (currentFood != null)
		{
			if (handItemRoot.childCount > 0)
			{
				Sprite handItemSprite = handItemRoot.GetComponentInChildren<SpriteRenderer>().sprite;
				if (currentFood.GetComponent<SpriteRenderer>().sprite != handItemSprite)
				{
					// ��W�\�I�P����\�I���P
					foreach (Transform child in handItemRoot)
						Destroy(child.gameObject);
				}
			}

			if (handItemRoot.childCount > 4)
			{
				// �ۦP�|�[�ܤW��
				//Debug.Log("hand item max limit!!!!!!!!!");
				return false;
			}

			for (int i = 0; i < getFoodCount; i++)
			{
				GameObject newItem = Instantiate(currentFood, handItemRoot.position, Quaternion.identity);
				newItem.transform.SetParent(handItemRoot);
				newItem.GetComponent<Collider2D>().enabled = false;

				newItem.transform.SetAsFirstSibling();
			}

			if (handItemUI) handItemUI.ChangeHandItemUI();
			return true;
		}

		return false;
	}

	/// ����������y��W���ȤH�T�{�I�\
	private bool TryConfirmOrderForNearbyGuests()
	{
		//Debug.Log("TryConfirmOrderForNearbyGuests");
		// �q�ثe�����쪺�Ȥl trigger ��ȤH�]NormalGuestController �O���U��Q�]���Ȥl�l����^
		foreach (var chair in currentChairTriggers)
			if (RoundManager.Instance.chairGroupManager.ConfirmOrderChair(chair.transform))
				return true;

		return false;
	}

	/// ���է��W�Ĥ@�Ӫ��~��������@�i�Ȥl
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


	// ===== �ȤlĲ�o���@ =====
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
