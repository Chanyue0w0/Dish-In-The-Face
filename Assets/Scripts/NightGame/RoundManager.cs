using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{


	[Header("-------- Setting ---------")]
	[SerializeField] private float finishDishHotPoint = 0.1f;


	[Header("-------- Reference ---------")]
	[SerializeField] private HotPointManager hotPointManager;

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    // §¹¦¨À\ÂI
    public void FinishDish(int dishID, int tableIndex)
    {
        hotPointManager.AddHotPoint(finishDishHotPoint);
    }
}
