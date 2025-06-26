using UnityEngine;

public class GuestGroupManager : MonoBehaviour
{
	[Header("-------------------- Settings -------------------- ")]
	[SerializeField] private float minSpawnColdTime;
	[SerializeField] private float maxSpawnColdTime;
	[SerializeField] private int minGuestPerWave;
	[SerializeField] private int maxGuestPerWave;

	[Header("-------------------- Reference -------------------- ")]
	[SerializeField] private Transform doorPosition;

	[SerializeField] private GameObject normalGuestPrefab;
	[SerializeField] private GameObject wanderGuestPrefab;
	[SerializeField] private GameObject troubleGuestPrefab;


	private float timer;
	private float nextSpawnTime;

	void Start()
	{
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
		int guestCount = Random.Range(minGuestPerWave, maxGuestPerWave + 1);
		for (int i = 0; i < guestCount; i++)
		{
			int guestType = Random.Range(0, 3); // 0: normal, 1: wander, 2: trouble
			GameObject guest = null;

			switch (guestType)
			{
				case 0:
					guest = Instantiate(normalGuestPrefab, GetSpawnPosition(), Quaternion.identity, gameObject.transform);
					break;
				case 1:

					guest = Instantiate(normalGuestPrefab, GetSpawnPosition(), Quaternion.identity, gameObject.transform);

					//guest = Instantiate(wanderGuestPrefab, GetSpawnPosition(), Quaternion.identity, gameObject.transform);
					//if (Random.value < 0.3f)
					//	GenerateTrash(guest.transform.position);
					break;
				case 2:
					guest = Instantiate(troubleGuestPrefab, GetSpawnPosition(), Quaternion.identity, gameObject.transform);
					break;
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

	void GenerateTrash(Vector3 position)
	{
		Debug.Log("�ͦ��U�� at " + position);
	}

	public void SetSpawnInterval(float min, float max)
	{
		minSpawnColdTime = min;
		maxSpawnColdTime = max;
	}

	public void SetGuestWaveCount(int min, int max)
	{
		minGuestPerWave = min;
		maxGuestPerWave = max;
	}

	
}
