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
			Debug.LogError("[GuestGroupManager] GuestPool.Instance is null, please set up GuestPool!");
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

		// Initialize return to pool info
		var handler = go.GetComponent<GuestPoolHandler>();
		if (handler != null) handler.Init(key);

		// Additional controller setup can be done here if needed
	}

	private Vector3 GetSpawnPosition()
	{
		if (doorPosition != null) return doorPosition.position;
		Debug.LogWarning("[GuestGroupManager] doorPosition not set, using (0,0,0)");
		return Vector3.zero;
	}

	/// <summary>
	/// Spawn TroubleGuest at specified position (can copy sprite for duplication)
	/// </summary>
	public GameObject SpawnTroubleGuestAt(Vector3 pos, Sprite copySprite = null)
	{
		if (GuestPool.Instance == null)
		{
			Debug.LogError("[GuestGroupManager] GuestPool.Instance is null, cannot spawn TroubleGuest");
			return null;
		}

		var guest = GuestPool.Instance.SpawnGuest(troubleGuestKey, pos, Quaternion.identity);
		if (guest == null) return null;

		var handler = guest.GetComponent<GuestPoolHandler>();
		if (handler != null) handler.Init(troubleGuestKey);

		// If TroubleGusetController has SetSprite(Sprite) method, can add sprite copying here
		var ctrl = guest.GetComponent<TroubleGuestController>();
		if (ctrl != null && copySprite != null)
		{
			ctrl.SetSprite(copySprite);
		}

		return guest;
	}

	/// <summary>
	/// Debug: Reset patience of all NormalGuest on the map
	/// </summary>
	public void ResetAllGuestsPatience()
	{
		foreach (Transform child in transform) // Iterate through children (if Guest is under GuestPool's parent, can iterate through GuestPool's parent)
		{
			var controller = child.GetComponent<NormalGuestController>();
			if (controller == null) continue;
			controller.ResetPatience();
		}
	}
}
