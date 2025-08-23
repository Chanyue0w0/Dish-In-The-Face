using UnityEngine;

/// <summary>
/// 綁在各個 Guest 上，記住它來自哪個 key 的池，並能主動回收
/// </summary>
public class GuestPoolHandler : MonoBehaviour
{
	[SerializeField] private string poolKey; // 僅用來除錯/可視，實際由 Init 設定

	/// <summary>生成後由管理者呼叫，告訴我我是從哪個池（key）來的</summary>
	public void Init(string key)
	{
		poolKey = key;
	}

	/// <summary>客人離開或死亡時呼叫，將自己放回池內</summary>
	public void Release()
	{
		if (GuestPool.Instance != null && !string.IsNullOrWhiteSpace(poolKey))
			GuestPool.Instance.ReleaseGuest(poolKey, gameObject);
		else
			Destroy(gameObject);
	}
}
