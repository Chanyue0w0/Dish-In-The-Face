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
			Debug.LogError("[GuestGroupManager] GuestPool.Instance ���šA�Х���m�ó]�w GuestPool�I");
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

		// �j�w�^����T
		var handler = go.GetComponent<GuestPoolHandler>();
		if (handler != null) handler.Init(key);

		// �o�̦p�ݪ�l�� controller ���A�B�i���ɯ赥�A�]�i�b���]�w
	}

	private Vector3 GetSpawnPosition()
	{
		if (doorPosition != null) return doorPosition.position;
		Debug.LogWarning("[GuestGroupManager] doorPosition �����w�A�ϥ� (0,0,0)");
		return Vector3.zero;
	}

	/// <summary>
	/// �b���w��m�ͦ� TroubleGuest�]�i��ܽƻs�~�[ sprite�^
	/// </summary>
	public GameObject SpawnTroubleGuestAt(Vector3 pos, Sprite copySprite = null)
	{
		if (GuestPool.Instance == null)
		{
			Debug.LogError("[GuestGroupManager] GuestPool.Instance ���šA�L�k�ͦ� TroubleGuest");
			return null;
		}

		var guest = GuestPool.Instance.SpawnGuest(troubleGuestKey, pos, Quaternion.identity);
		if (guest == null) return null;

		var handler = guest.GetComponent<GuestPoolHandler>();
		if (handler != null) handler.Init(troubleGuestKey);

		// �Y�A�� TroubleGusetController �� SetSprite(Sprite) �����}��k�A�i�b�o�̰��~�[�ƻs
		var ctrl = guest.GetComponent<TroubleGusetController>();
		if (ctrl != null && copySprite != null)
		{
			ctrl.SetSprite(copySprite);
		}

		return guest;
	}

	/// <summary>
	/// �d�ҡG���m���W�Ҧ� NormalGuest ���@��
	/// </summary>
	public void ResetAllGuestsPatience()
	{
		foreach (Transform child in transform) // ���A���h�ŵ��c�өw�]�p�G Guest ����b GuestPool �� parent �U�A�i�ﱽ GuestPool �� parent�^
		{
			var controller = child.GetComponent<NormalGuestController>();
			if (controller == null) continue;
			controller.ResetPatience();
		}
	}
}
