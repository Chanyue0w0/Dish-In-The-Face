using UnityEngine;

/// <summary>
/// Attached to Guest objects, holds the pool key for identification and return to pool
/// </summary>
public class GuestPoolHandler : MonoBehaviour
{
	[SerializeField] private string poolKey; // Used for spawning/recycling, set by Init

	/// <summary>Called by pool manager initialization, tells us which pool we came from (key)</summary>
	public void Init(string key)
	{
		poolKey = key;
	}

	/// <summary>Called by external systems when dying/deactivating, returns self to pool</summary>
	public void Release()
	{
		if (GuestPool.Instance != null && !string.IsNullOrWhiteSpace(poolKey))
			GuestPool.Instance.ReleaseGuest(poolKey, gameObject);
		else
			Destroy(gameObject);
	}
}
