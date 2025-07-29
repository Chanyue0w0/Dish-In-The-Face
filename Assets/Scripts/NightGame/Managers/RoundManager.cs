using UnityEngine;

public class RoundManager : MonoBehaviour
{
	[Header("-------- Setting ---------")]
	[SerializeField] private float finishDishHotPoint = 0.5f;
    [SerializeField] private float attackEnemyHotPoint = 0.5f;
	[SerializeField] private int defeatEnemyrewardCoin = 10;

	[Header("-------- Reference ---------")]
	[SerializeField] public HotPointManager hotPointManager;
    [SerializeField] public FoodsGroupManager foodsGroupManager;
	[SerializeField] public GuestGroupManager guestGroupManager;
    [SerializeField] public ChairGroupManager chairGroupManager;
    [SerializeField] public CoinUIController coinUIController;
	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


	// 放下餐點成功
	public void PullDownDishSuccess()
	{
		Debug.Log("增加熱度!!!!!!!!!!");
		hotPointManager.AddHotPoint(finishDishHotPoint);
	}
	 //完成餐點取得金幣(顧客吃完)
	public void FinishDishSuccess(Transform targetChair, int coinCound)
	{
		chairGroupManager.PullDownCoin(targetChair, coinCound);
	}

	public void DefeatEnemySuccess()
    {
        hotPointManager.AddHotPoint(attackEnemyHotPoint);
		GetCoin(defeatEnemyrewardCoin);
	}

	public void GetCoin(int coinCount)
	{
		coinUIController.AddCoin(coinCount * hotPointManager.GetMoneyMultiplier());
	}

	public void SetFinishDishHotPoint(float point)
    {
		finishDishHotPoint = point;
	}
	public void SetAttackEnemyHotPoint(float point)
	{
		attackEnemyHotPoint = point;
	}
}
