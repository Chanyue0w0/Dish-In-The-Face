using UnityEngine;

public class RoundManager : MonoBehaviour
{
	public static RoundManager Instance { get; private set; } // ���

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
		// �]�w���
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject); // �p�G�w�g����L��ҡA�R���o��
			return;
		}
		Instance = this;
		// �p�G�Ʊ������O�d�A�[�o��G
		// DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		endPane.SetActive(false);
		stopPanel.SetActive(false);
		GameContinue();
	}

	// ��U�\�I���\
	public void PullDownDishSuccess()
	{
		hotPointManager.AddHotPoint(finishDishHotPoint);
	}

	// �����\�I���o����(�U�ȦY��)
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
