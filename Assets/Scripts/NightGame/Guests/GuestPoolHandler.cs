using UnityEngine;

/// <summary>
/// �j�b�U�� Guest �W�A�O���Ӧۭ��� key �����A�ï�D�ʦ^��
/// </summary>
public class GuestPoolHandler : MonoBehaviour
{
	[SerializeField] private string poolKey; // �ȥΨӰ���/�i���A��ڥ� Init �]�w

	/// <summary>�ͦ���Ѻ޲z�̩I�s�A�i�D�ڧڬO�q���Ӧ��]key�^�Ӫ�</summary>
	public void Init(string key)
	{
		poolKey = key;
	}

	/// <summary>�ȤH���}�Φ��`�ɩI�s�A�N�ۤv��^����</summary>
	public void Release()
	{
		if (GuestPool.Instance != null && !string.IsNullOrWhiteSpace(poolKey))
			GuestPool.Instance.ReleaseGuest(poolKey, gameObject);
		else
			Destroy(gameObject);
	}
}
