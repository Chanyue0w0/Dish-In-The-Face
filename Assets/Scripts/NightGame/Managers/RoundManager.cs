using UnityEngine;

public class RoundManager : MonoBehaviour
{
	public static RoundManager Instance { get; private set; } // 單例

	[Header("-------- Setting ---------")]
	[SerializeField] private float finishDishHotPoint = 0.5f;
	[SerializeField] private float attackEnemyHotPoint = 0.5f;
	[SerializeField] private int defeatEnemyrewardCoin = 10;

	[Header("-------- Reference ---------")]
	[Header("Manager")]
	public HotPointManager hotPointManager;
	public FoodsGroupManager foodsGroupManager;
	public GuestGroupManager guestGroupManager;
	public ChairGroupManager chairGroupManager;
	public CoinUIController coinUIController;
	public GlobalLightManager globalLightManager;
	public TimeLimitCounter timeLimitCounter;

	[Header("GameObject")]
	[SerializeField] private GameObject endPane;
	[SerializeField] private GameObject stopPanel;
	public Transform ObstaclesGroup;

	private void Awake()
	{
		// 設定單例
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject); // 如果已經有其他實例，刪掉這個
			return;
		}
		Instance = this;
		// 如果希望跨場景保留，加這行：
		// DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		endPane.SetActive(false);
		stopPanel.SetActive(false);
		GameContinue();
	}

	// 放下餐點成功
	public void PullDownDishSuccess()
	{
		hotPointManager.AddHotPoint(finishDishHotPoint);
	}

	// 完成餐點取得金幣(顧客吃完)
	public void FinishDishSuccess(Transform targetChair, int coinCount)
	{
		chairGroupManager.PullDownCoin(targetChair, coinCount);
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

		timeLimitCounter.PauseCountdown();
		stopPanel.SetActive(true);
		Time.timeScale = 0f;
	}

	public void GameContinue()
	{
		timeLimitCounter.StartCountdown();
		stopPanel.SetActive(false);
		Time.timeScale = 1f;
	}
}
