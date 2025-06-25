using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{


	[Header("-------- Setting ---------")]
	[SerializeField] private float finishDishHotPoint = 0.1f;


	[Header("-------- Reference ---------")]
	[SerializeField] public HotPointManager hotPointManager;
    [SerializeField] public FoodsGroupManager foodsGroupManager;
    [SerializeField] public TableGroupManager tableGroupManager;
	[SerializeField] public GuestGroupManager guestGroupManager;
    [SerializeField] public ChairGroupManager chairGroupManager;

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
