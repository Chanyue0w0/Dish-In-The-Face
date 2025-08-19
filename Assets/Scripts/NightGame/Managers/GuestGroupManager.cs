using UnityEngine;
using UnityEngine.Pool;

public class GuestGroupManager : MonoBehaviour
{
	[Header("-------------------- Settings --------------------")]
	[SerializeField] private float minSpawnColdTime;
	[SerializeField] private float maxSpawnColdTime;

	[Header("Normal Guest Settings")]
	[SerializeField] private bool isSpawnNormalGuest = true;
	[SerializeField] private int minNormalGuests = 1;
	[SerializeField] private int maxNormalGuests = 3;

	[Header("Wander Guest Settings")]
	[SerializeField] private bool isSpawnWanderGuest = true;
	[SerializeField] private int minWanderGuests = 0;
	[SerializeField] private int maxWanderGuests = 2;

	[Header("Trouble Guest Settings")]
	[SerializeField] private bool isSpawnTroubleGuest = true;
	[SerializeField] private int minTroubleGuests = 0;
	[SerializeField] private int maxTroubleGuests = 1;

	[Header("-------------------- Reference --------------------")]
	[SerializeField] private Transform doorPosition;
	[SerializeField] private GameObject normalGuestPrefab;
	[SerializeField] private GameObject wanderGuestPrefab;
	[SerializeField] private GameObject troubleGuestPrefab;
	public Transform enterPoistion;
	public Transform exitPoistion;

	private float timer;
	private float nextSpawnTime;

	// 內建物件池
	private ObjectPool<GameObject> normalGuestPool;
	private ObjectPool<GameObject> wanderGuestPool;
	private ObjectPool<GameObject> troubleGuestPool;

	void Start()
	{
		normalGuestPool = new ObjectPool<GameObject>(
			createFunc: () => Instantiate(normalGuestPrefab, transform),
			actionOnGet: obj => obj.SetActive(true),
			actionOnRelease: obj => obj.SetActive(false),
			actionOnDestroy: obj => Destroy(obj),
			collectionCheck: false,
			defaultCapacity: 20,
			maxSize: 200
		);

		wanderGuestPool = new ObjectPool<GameObject>(
			createFunc: () => Instantiate(wanderGuestPrefab, transform),
			actionOnGet: obj => obj.SetActive(true),
			actionOnRelease: obj => obj.SetActive(false),
			actionOnDestroy: obj => Destroy(obj),
			collectionCheck: false,
			defaultCapacity: 20,
			maxSize: 200
		);

		troubleGuestPool = new ObjectPool<GameObject>(
			createFunc: () => Instantiate(troubleGuestPrefab, transform),
			actionOnGet: obj => obj.SetActive(true),
			actionOnRelease: obj => obj.SetActive(false),
			actionOnDestroy: obj => Destroy(obj),
			collectionCheck: false,
			defaultCapacity: 20,
			maxSize: 200
		);

		SetNextSpawnTime();
	}

	void Update()
	{
		timer += Time.deltaTime;
		if (timer >= nextSpawnTime)
		{
			SpawnGuestWave();
			timer = 0f;
			SetNextSpawnTime();
		}
	}

	void SetNextSpawnTime()
	{
		nextSpawnTime = Random.Range(minSpawnColdTime, maxSpawnColdTime);
	}

	void SpawnGuestWave()
	{
		if (isSpawnNormalGuest)
		{
			int count = Random.Range(minNormalGuests, maxNormalGuests + 1);
			for (int i = 0; i < count; i++)
			{
				GameObject guest = normalGuestPool.Get();
				guest.transform.position = GetSpawnPosition();
				guest.GetComponent<GuestPoolHandler>().Init(normalGuestPool);
			}
		}

		if (isSpawnWanderGuest)
		{
			int count = Random.Range(minWanderGuests, maxWanderGuests + 1);
			for (int i = 0; i < count; i++)
			{
				GameObject guest = wanderGuestPool.Get();
				guest.transform.position = GetSpawnPosition();
				guest.GetComponent<GuestPoolHandler>().Init(wanderGuestPool);
			}
		}

		if (isSpawnTroubleGuest)
		{
			int count = Random.Range(minTroubleGuests, maxTroubleGuests + 1);
			for (int i = 0; i < count; i++)
			{
				GameObject guest = troubleGuestPool.Get();
				guest.transform.position = GetSpawnPosition();
				guest.GetComponent<GuestPoolHandler>().Init(troubleGuestPool);
			}
		}
	}

	Vector3 GetSpawnPosition()
	{
		if (doorPosition != null)
			return doorPosition.position;
		else
		{
			Debug.LogWarning("spawnPosition 未指定，使用預設位置 (0,0,0)");
			return Vector3.zero;
		}
	}

	public GameObject SpawnTroubleGuestAt(Vector3 pos, Sprite copySprite = null)
	{
		// 取出 TroubleGuest
		GameObject guest = null;
		if (troubleGuestPool != null)
		{
			guest = troubleGuestPool.Get();
			guest.transform.SetParent(transform);
			guest.transform.position = pos;

			// 綁定物件池處理器（回收用）
			var handler = guest.GetComponent<GuestPoolHandler>();
			if (handler != null) handler.Init(troubleGuestPool);

			// 設定外觀（若給了要複製的 sprite，就沿用）
			var ctrl = guest.GetComponent<TroubleGusetController>();
			if (ctrl != null && copySprite != null)
			{
				// 下方第(3)點會把 SetSprite 改成 public，這裡就能直接呼叫
				ctrl.SetSprite(copySprite);
			}
		}
		else
		{
			Debug.LogError("troubleGuestPool 尚未初始化，無法生成 TroubleGuest");
		}

		return guest;
	}
}
