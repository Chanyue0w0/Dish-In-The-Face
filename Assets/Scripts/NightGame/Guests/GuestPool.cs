using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 鍵值式的 Guest 物件池（像 VFXPool 那樣）
/// 1) 在 Inspector 的 Prefabs 清單中設定 key 與對應的 Guest Prefab
/// 2) 透過 SpawnGuest(key, pos, rot) 取得物件；ReleaseGuest(key, obj) 回收物件
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

		[Tooltip("對應的 Guest Prefab（需含 GuestPoolHandler 組件）")]
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

		// 建立各 key 的物件池
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
	/// 生成指定 key 的 Guest（自動 SetActive(true)）
	/// 生成後別忘了對 GuestPoolHandler.Init(key)
	/// </summary>
	public GameObject SpawnGuest(string key, Vector3 position, Quaternion rotation)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			Debug.LogWarning("[GuestPool] SpawnGuest: key 是空的");
			return null;
		}

		key = key.Trim();
		if (!pools.TryGetValue(key, out var pool))
		{
			Debug.LogWarning($"[GuestPool] 未找到 key = {key} 的物件池");
			return null;
		}

		var obj = pool.Get();
		obj.transform.SetPositionAndRotation(position, rotation);
		return obj;
	}

	/// <summary>
	/// 回收指定 key 的 Guest
	/// </summary>
	public void ReleaseGuest(string key, GameObject obj)
	{
		if (obj == null) return;

		if (string.IsNullOrWhiteSpace(key))
		{
			Debug.LogWarning("[GuestPool] ReleaseGuest: key 是空的，改為直接 Destroy");
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
			Debug.LogWarning($"[GuestPool] 未找到 key = {key} 的物件池，改為直接 Destroy");
			Destroy(obj);
		}
	}
}
