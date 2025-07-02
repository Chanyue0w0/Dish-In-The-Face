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
		if (table == null) return;

		Transform parent = table.transform.GetChild(0);

		// �R���Ĥ@�Ӥl����]�p�G���^
		if (parent.childCount > 0)
		{
			Destroy(parent.GetChild(0).gameObject);
		}

		// �����M�椤���Ĥ@�Ӥ����]�p�G���^
		if (npcOntables[table].Count > 0)
		{
			npcOntables[table].RemoveAt(0);
		}
	}

	public void AddCanUseTable(GameObject table)
	{
		tableObjects.Add(table);
	}
}
