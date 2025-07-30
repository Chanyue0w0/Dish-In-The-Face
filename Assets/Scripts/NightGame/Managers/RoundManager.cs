using UnityEngine;

public class RoundManager : MonoBehaviour
{
	[Header("-------- Setting ---------")]
	[SerializeField] private float finishDishHotPoint = 0.5f;
	[SerializeField] private float attackEnemyHotPoint = 0.5f;
	[SerializeField] private int defeatEnemyrewardCoin = 10;

	[Header("-------- Reference ---------")]
	[Header("Manager")]
	[SerializeField] public HotPointManager hotPointManager;
	[SerializeField] public FoodsGroupManager foodsGroupManager;
	[SerializeField] public GuestGroupManager guestGroupManager;
	[SerializeField] public ChairGroupManager chairGroupManager;
	[SerializeField] public CoinUIController coinUIController;
	[SerializeField] public GlobalLightManager globalLightManager;

	[Header("GameObject")]
	[SerializeField] private GameObject endPane;
	[SerializeField] GameObject stopPanel;

	// Start is called before the first frame update
	void Start()
	{
		endPane.SetActive(false);
		stopPanel.SetActive(false);
		GameContinue();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			GameStop();
		}
	}

	// 放下餐點成功
	public void PullDownDishSuccess()
	{
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

	public void GameOver()
	{
		endPane.SetActive(true);
		Time.timeScale = 0f;
	}

	public void GameStop()
	{
		if (stopPanel.activeSelf)
		{
			GameContinue();
			return;
		}

		stopPanel.SetActive(true);
		Time.timeScale = 0f;
	}

	public void GameContinue()
	{
		stopPanel.SetActive(false);
		Time.timeScale = 1f;
	}
}
