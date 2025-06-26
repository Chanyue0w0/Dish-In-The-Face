using System.Collections.Generic;
using UnityEngine;

public class TableGroupManager : MonoBehaviour
{
	//[Header("-------- Setting ---------")]
	//[SerializeField] private float clearDelay = 3f; // �i�վ�G���~�����ɶ��]��^
	[Header("-------- Reference ---------")]
	[SerializeField] private List<GameObject> tableObjects;
	[SerializeField] private RoundManager roundManager;

	//private Dictionary<GameObject, List<Sprite>> orderDishOntables;
	private Dictionary<GameObject, List<NormalGuestController>> npcOntables;
	void Start()
	{
		npcOntables = new Dictionary<GameObject, List<NormalGuestController>>();

		foreach (GameObject table in tableObjects)
		{
			npcOntables[table] = new List<NormalGuestController>();
		}

		foreach (GameObject obj in tableObjects)
		{
			ClearTableItem(obj);
		}
	}

	// Update is called once per frame
	void Update()
	{

	}

	public void PullDownTableItem(GameObject table, GameObject handItem)
	{
		GameObject tableItem = table.transform.GetChild(0).gameObject;
		Sprite foodSprite = handItem.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;
		//if (orderDishOntables[table].Find(x => x == foodSprite) == null) return; // ��W�\�I�O�һ��\�I

		//�O�_�� NPC �I���\�I
		foreach (NormalGuestController npc in npcOntables[table])
		{
			// �^���w�W�\
			if (npc.IsReceiveFood(foodSprite))
			{
				// ��m�\�I
				handItem.transform.SetParent(tableItem.transform);
				handItem.transform.localPosition = Vector3.zero;
			}
		}
	}

	public void SetOrderDishOnTable(GameObject table , GameObject npc)
	{
		//orderDishOntables[table].Add(foods);
		npcOntables[table].Add(npc.GetComponent<NormalGuestController>());
		return;
	}

	public void ClearTableItem(GameObject table)
	{
		foreach (Transform child in table.transform.GetChild(0))
		{
			Destroy(child.gameObject);
			//orderDishOntables[table].Clear();
			npcOntables[table].Clear();
		}
	}
}
