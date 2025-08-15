using UnityEngine;
using UnityEngine.Pool;

public class GuestPoolHandler : MonoBehaviour
{
	private IObjectPool<GameObject> pool;

	public void Init(IObjectPool<GameObject> pool)
	{
		this.pool = pool;
	}

	// 客人離開或死亡時呼叫
	public void Release()
	{
		if (pool != null)
			pool.Release(gameObject);
		else
			Destroy(gameObject); // 如果沒設定池就直接 Destroy
	}
}
