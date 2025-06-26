using UnityEngine;

public class RoundManager : MonoBehaviour
{


	[Header("-------- Setting ---------")]
	[SerializeField] private float finishDishHotPoint = 0.5f;
    [SerializeField] private float attackEnemyHotPoint = 0.5f;

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
    public void FinishDishSuccess(int dishID, int tableIndex)
    {
        hotPointManager.AddHotPoint(finishDishHotPoint);
    }

    public void DefeatEnemySuccess()
    {
        hotPointManager.AddHotPoint(attackEnemyHotPoint);
    }
}
