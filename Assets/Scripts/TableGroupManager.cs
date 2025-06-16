using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableGroupManager : MonoBehaviour
{
	[SerializeField] List<GameObject> tableObjects;
	[SerializeField] List<GameObject> itemOntables;

    [SerializeField] TempGameManager tempGameManager;

    
    [SerializeField] private float clearDelay = 3f; // 可調整：物品維持時間（秒）
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
        GameObject item = table.transform.GetChild(0).gameObject;
        item.GetComponent<SpriteRenderer>().sprite = handItem.GetComponent<SpriteRenderer>().sprite;
        item.SetActive(true);

        int tableIndex = 0;
        foreach (GameObject t in tableObjects)
        {
            if (t == table) break;
            tableIndex ++;
        }

        tempGameManager.FinishDish(handItem.GetComponent<SpriteRenderer>().sprite.name, tableIndex);


		// 啟動延遲清除
		StartCoroutine(ClearTableAfterDelay(table, clearDelay));
	}

    public void ClearTableItem(GameObject table)
    {
        table.transform.GetChild(0).gameObject.SetActive(false);
    }

	private IEnumerator ClearTableAfterDelay(GameObject table, float delay)
	{
		yield return new WaitForSeconds(delay);
		ClearTableItem(table);
	}
}
