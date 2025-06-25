using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuestGroupManager : MonoBehaviour
{
	[Header("-------------------- Settings -------------------- ")]
	[SerializeField] private float minSpawnInterval = 10f;
	[SerializeField] private float maxSpawnInterval = 20f;
	[SerializeField] private int minGuestPerWave = 3;
	[SerializeField] private int maxGuestPerWave = 5;

	[Header("-------------------- Reference -------------------- ")]
	[SerializeField] private Transform doorPosition;

	[SerializeField] private GameObject normalGuestPrefab;
	[SerializeField] private GameObject wanderGuestPrefab;
	[SerializeField] private GameObject troubleGuestPrefab;

	[Header("Chair List")]
	[SerializeField] private Transform[] chairList; // �Ҧ��Ȥl��m
	private HashSet<Transform> occupiedChairs = new HashSet<Transform>(); // ���b�Q�ϥΪ��Ȥl���X

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
		nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
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
				//case 1:
				//	guest = Instantiate(wanderGuestPrefab, GetSpawnPosition(), Quaternion.identity, gameObject.transform);
				//	if (Random.value < 0.3f)
				//		GenerateTrash(guest.transform.position);
				//	break;
				//case 2:
				//	guest = Instantiate(troubleGuestPrefab, GetSpawnPosition(), Quaternion.identity, gameObject.transform);
				//	break;
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
		minSpawnInterval = min;
		maxSpawnInterval = max;
	}

	public void SetGuestWaveCount(int min, int max)
	{
		minGuestPerWave = min;
		maxGuestPerWave = max;
	}

	// ===========================
	// �Ȥl�޲z�\��
	// ===========================

	/// �M��@�ӪŪ��Ȥl�üаO���w���ΡC�䤣��h�^�� null�C
	public Transform FindEmptyChair()
	{
		foreach (Transform chair in chairList)
		{
			if (!occupiedChairs.Contains(chair))
			{
				occupiedChairs.Add(chair);
				return chair;
			}
		}
		return null;
	}

	/// ��ȤH���u������Ȥl�C
	public void ReleaseChair(Transform targetChair)
	{
		if (occupiedChairs.Contains(targetChair))
			occupiedChairs.Remove(targetChair);
	}
}
