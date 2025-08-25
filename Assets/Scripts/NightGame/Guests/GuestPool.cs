using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Guest object pool (similar to VFXPool)
/// 1) Set up key and corresponding Guest Prefab in Inspector Prefabs list
/// 2) Get objects via SpawnGuest(key, pos, rot), return via ReleaseGuest(key, obj)
/// </summary>
public class GuestPool : MonoBehaviour
{
	[Header("Guest Parent Group")]
	[SerializeField] private Transform guestGroupParent;

	public static GuestPool Instance { get; private set; }

	[System.Serializable]
	public class GuestPrefabData
	{
		public string key;

		[Tooltip("Corresponding Guest Prefab (should contain GuestPoolHandler component)")]
		public GameObject prefab;

		[Min(0)] public int defaultCapacity = 20;
		[Min(1)] public int maxSize = 200;
	}

	[Header("Guest Prefabs")]
	[SerializeField] private List<GuestPrefabData> guestPrefabs = new List<GuestPrefabData>();

	private readonly Dictionary<string, ObjectPool<GameObject>> pools = new Dictionary<string, ObjectPool<GameObject>>();

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;

		// Create pools for each key
		foreach (var data in guestPrefabs)
		{
			if (data.prefab == null || string.IsNullOrWhiteSpace(data.key))
				continue;

			var key = data.key.Trim();

			ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
				createFunc: () =>
				{
					var obj = Instantiate(data.prefab, guestGroupParent != null ? guestGroupParent : transform);
					obj.SetActive(false);
					return obj;
				},
				actionOnGet: (obj) => { obj.SetActive(true); },
				actionOnRelease: (obj) => { obj.SetActive(false); },
				actionOnDestroy: (obj) => { Destroy(obj); },
				collectionCheck: false,
				defaultCapacity: data.defaultCapacity,
				maxSize: data.maxSize
			);

			pools[key] = pool;
		}
	}

	/// <summary>
	/// �ͦ����w key �� Guest�]�۰� SetActive(true)�^
	/// �ͦ���O�ѤF�� GuestPoolHandler.Init(key)
	/// </summary>
	public GameObject SpawnGuest(string key, Vector3 position, Quaternion rotation)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			Debug.LogWarning("[GuestPool] SpawnGuest: key �O�Ū�");
			return null;
		}

		key = key.Trim();
		if (!pools.TryGetValue(key, out var pool))
		{
			Debug.LogWarning($"[GuestPool] ����� key = {key} �������");
			return null;
		}

		var obj = pool.Get();
		obj.transform.SetPositionAndRotation(position, rotation);
		return obj;
	}

	/// <summary>
	/// �^�����w key �� Guest
	/// </summary>
	public void ReleaseGuest(string key, GameObject obj)
	{
		if (obj == null) return;

		if (string.IsNullOrWhiteSpace(key))
		{
			Debug.LogWarning("[GuestPool] ReleaseGuest: key �O�Ū��A�אּ���� Destroy");
			Destroy(obj);
			return;
		}

		key = key.Trim();
		if (pools.TryGetValue(key, out var pool))
		{
			pool.Release(obj);
		}
		else
		{
			Debug.LogWarning($"[GuestPool] ����� key = {key} ��������A�אּ���� Destroy");
			Destroy(obj);
		}
	}
}
