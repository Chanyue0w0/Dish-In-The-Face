using UnityEngine;
using UnityEngine.Pool;

public class GuestPoolHandler : MonoBehaviour
{
	private IObjectPool<GameObject> pool;

	public void Init(IObjectPool<GameObject> pool)
	{
		this.pool = pool;
	}

	// �ȤH���}�Φ��`�ɩI�s
	public void Release()
	{
		if (pool != null)
			pool.Release(gameObject);
		else
			Destroy(gameObject); // �p�G�S�]�w���N���� Destroy
	}
}
