using UnityEngine;

public class GuestGroupManager : MonoBehaviour
{
	[Header("-------------------- Settings --------------------")]
	[SerializeField] private float minSpawnColdTime = 3f;
	[SerializeField] private float maxSpawnColdTime = 6f;

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
	public Transform enterPoistion;
	public Transform exitPoistion;

	[Header("-------------------- Guest Pool Keys --------------------")]
	[SerializeField] private string normalGuestKey = "NormalGuest";
	[SerializeField] private string wanderGuestKey = "WanderGuest";
	[SerializeField] private string troubleGuestKey = "TroubleGuest";

	private float timer;
	private float nextSpawnTime;

	private void Start()
	{
		SetNextSpawnTime();
	}

	private void Update()
	{
		timer += Time.deltaTime;
		if (timer >= nextSpawnTime)
		{
			SpawnGuestWave();
			timer = 0f;
			SetNextSpawnTime();
		}
	}

	private void SetNextSpawnTime()
	{
		nextSpawnTime = Random.Range(minSpawnColdTime, maxSpawnColdTime);
	}

	private void SpawnGuestWave()
	{
		if (GuestPool.Instance == null)
		{
			Debug.LogError("[GuestGroupManager] GuestPool.Instance 為空，請先放置並設定 GuestPool！");
			return;
		}

		Vector3 pos = GetSpawnPosition();

		if (isSpawnNormalGuest)
		{
			int count = Random.Range(minNormalGuests, maxNormalGuests + 1);
			for (int i = 0; i < count; i++)
				SpawnOne(normalGuestKey, pos);
		}

		if (isSpawnWanderGuest)
		{
			int count = Random.Range(minWanderGuests, maxWanderGuests + 1);
			for (int i = 0; i < count; i++)
				SpawnOne(wanderGuestKey, pos);
		}

		if (isSpawnTroubleGuest)
		{
			int count = Random.Range(minTroubleGuests, maxTroubleGuests + 1);
			for (int i = 0; i < count; i++)
				SpawnOne(troubleGuestKey, pos);
		}
	}

	private void SpawnOne(string key, Vector3 pos)
	{
		var go = GuestPool.Instance.SpawnGuest(key, pos, Quaternion.identity);
		if (go == null) return;

		// 綁定回收資訊
		var handler = go.GetComponent<GuestPoolHandler>();
		if (handler != null) handler.Init(key);

		// 這裡如需初始化 controller 狀態、進場導航等，也可在此設定
	}

	private Vector3 GetSpawnPosition()
	{
		if (doorPosition != null) return doorPosition.position;
		Debug.LogWarning("[GuestGroupManager] doorPosition 未指定，使用 (0,0,0)");
		return Vector3.zero;
	}

	/// <summary>
	/// 在指定位置生成 TroubleGuest（可選擇複製外觀 sprite）
	/// </summary>
	public GameObject SpawnTroubleGuestAt(Vector3 pos, Sprite copySprite = null)
	{
		if (GuestPool.Instance == null)
		{
			Debug.LogError("[GuestGroupManager] GuestPool.Instance 為空，無法生成 TroubleGuest");
			return null;
		}

		var guest = GuestPool.Instance.SpawnGuest(troubleGuestKey, pos, Quaternion.identity);
		if (guest == null) return null;

		var handler = guest.GetComponent<GuestPoolHandler>();
		if (handler != null) handler.Init(troubleGuestKey);

		// 若你的 TroubleGusetController 有 SetSprite(Sprite) 的公開方法，可在這裡做外觀複製
		var ctrl = guest.GetComponent<TroubleGusetController>();
		if (ctrl != null && copySprite != null)
		{
			ctrl.SetSprite(copySprite);
		}

		return guest;
	}

	/// <summary>
	/// 範例：重置場上所有 NormalGuest 的耐心
	/// </summary>
	public void ResetAllGuestsPatience()
	{
		foreach (Transform child in transform) // 視你的層級結構而定（如果 Guest 都放在 GuestPool 的 parent 下，可改掃 GuestPool 的 parent）
		{
			var controller = child.GetComponent<NormalGuestController>();
			if (controller == null) continue;
			controller.ResetPatience();
		}
	}
}
