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

	// ���ت����
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
			Debug.LogWarning("spawnPosition �����w�A�ϥιw�]��m (0,0,0)");
			return Vector3.zero;
		}
	}

	public GameObject SpawnTroubleGuestAt(Vector3 pos, Sprite copySprite = null)
	{
		// ���X TroubleGuest
		GameObject guest = null;
		if (troubleGuestPool != null)
		{
			guest = troubleGuestPool.Get();
			guest.transform.SetParent(transform);
			guest.transform.position = pos;

			// �j�w������B�z���]�^���Ρ^
			var handler = guest.GetComponent<GuestPoolHandler>();
			if (handler != null) handler.Init(troubleGuestPool);

			// �]�w�~�[�]�Y���F�n�ƻs�� sprite�A�N�u�Ρ^
			var ctrl = guest.GetComponent<TroubleGusetController>();
			if (ctrl != null && copySprite != null)
			{
				// �U���(3)�I�|�� SetSprite �令 public�A�o�̴N�ઽ���I�s
				ctrl.SetSprite(copySprite);
			}
		}
		else
		{
			Debug.LogError("troubleGuestPool �|����l�ơA�L�k�ͦ� TroubleGuest");
		}

		return guest;
	}
}
