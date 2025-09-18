using UnityEngine;

public class RoundManager : MonoBehaviour
{
	public static RoundManager Instance { get; private set; } // Singleton

	[Header("-------- Setting ---------")]
	[SerializeField] private int enemyReward = 10;

	[Header("-------- Reference ---------")]
	[Header("Manager")]
	public HotPointManager hotPointManager;
	public FoodsGroupManager foodsGroupManager;
	public GuestGroupManager guestGroupManager;
	public ChairGroupManager chairGroupManager;
	public CoinManager coinManager;
	public GlobalLightManager globalLightManager;
	public TimeLimitCounter timeLimitCounter;

	[Header("Public GameObject")]
	public Transform player;
	public Transform obstaclesGroup;

	[Header("GameObject")]
	[SerializeField] private GameObject endPane;
	[SerializeField] private GameObject stopPanel;

	private void Awake()
	{
		// Setup singleton
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject); // Destroy if instance already exists
			return;
		}
		Instance = this;
		// Keep instance across scenes if needed:
		// DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		endPane.SetActive(false);
		stopPanel.SetActive(false);
		GameContinue();
	}

	// Successfully placed dish
	public void PullDownDishSuccess()
	{
		// hotPointManager.DeliverDish();
		hotPointManager.AddHotPoint(1);
	}

	// Dish finished and eaten by guest
	public void FinishDishSuccess(Transform targetChair, int coinCount)
	{
		// chairGroupManager.PullDownCoin(targetChair, coinCount);
	}

	public void DefeatEnemySuccess()
	{
		// hotPointManager.DefeatEnemy();
		hotPointManager.AddHotPoint(1);
		GetCoin(enemyReward);
	}

	public void GetCoin(int coinCount)
	{
		coinManager.AddCoin(coinCount * hotPointManager.GetMoneyMultiplier());
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
