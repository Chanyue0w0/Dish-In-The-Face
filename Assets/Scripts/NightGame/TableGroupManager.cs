using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

public class TableGroupManager : MonoBehaviour
{
	[Header("-------- Setting ---------")]
	[SerializeField] private float clearDelay = 3f; // 可調整：物品維持時間（秒）
	[Header("-------- Reference ---------")]
	[SerializeField] private List<GameObject> tableObjects;
	[SerializeField] private List<GameObject> itemOntables;
	[SerializeField] private RoundManager roundManager;
	//[SerializeField] TempGameManager tempGameManager;


													// Start is called before the first frame update
	void Start()
	{
		foreach (GameObject obj in tableObjects)
		{
			ClearTableItem(obj);
		}
	}

	// Update is called once per frame
	void Update()
	{

	}

	public void SetTableItem(GameObject table, GameObject handItem)
	{
		// 放置餐點
		GameObject tableItem = table.transform.GetChild(0).gameObject;
		handItem.transform.SetParent(tableItem.transform);
		handItem.transform.localPosition = Vector3.zero;

		// 回報已上餐
		//int tableIndex = 0;
		//foreach (GameObject t in tableObjects)
		//{
		//	if (t == table) break;
		//	tableIndex++;
		//}
		roundManager.FinishDish(1, 1);


		// 啟動延遲清除
		StartCoroutine(ClearTableAfterDelay(table, clearDelay));
	}

	public void ClearTableItem(GameObject table)
	{
		foreach (Transform child in table.transform.GetChild(0))
		{
			Destroy(child.gameObject);
		}
	}

	private IEnumerator ClearTableAfterDelay(GameObject table, float delay)
	{
		yield return new WaitForSeconds(delay);
		ClearTableItem(table);
	}
}
