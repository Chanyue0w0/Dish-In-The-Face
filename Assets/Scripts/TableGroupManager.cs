using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableGroupManager : MonoBehaviour
{
	[SerializeField] List<GameObject> tebleObjects;
	[SerializeField] List<GameObject> itemOntables;
	// Start is called before the first frame update
	void Start()
    {
        foreach (GameObject obj in tebleObjects)
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

    }

    public void ClearTableItem(GameObject table)
    {
        table.transform.GetChild(0).gameObject.SetActive(false);
    }
}
