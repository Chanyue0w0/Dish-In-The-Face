using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinOnTable : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private float vfxDuration = 1.0f; // �S�ī���ɶ��]��^

	[Header("Reference")]
	[SerializeField] private GameObject coinVFXPrefab; // �z�����S�� prefab

	[SerializeField] private RoundManager roundManager;
	private bool isCollected = false; // ����h��Ĳ�o

	private int coinCount;

	public void Awake()
	{
		roundManager = GameObject.Find("Rround Manager").GetComponent<RoundManager>();
	}

	/// �b�ͦ���]�w�����ƶq
	public void SetCoinCount(int value)
	{
		coinCount = value;
	}
	/// �ͦ��z�����S�ġA�S�ĵ�����R��������
	public void GetCoin()
	{
		if (isCollected) return;
		isCollected = true;

		if (coinVFXPrefab != null)
		{
			// �ͦ��S��
			GameObject vfx = Instantiate(coinVFXPrefab, transform.position, Quaternion.identity);

			// ������@�_�R���]��ĳ VFX prefab �����]�p���|�۰ʧR���A�]�i�H�o�˳B�z�^
			ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
			if (ps != null)
			{
				vfxDuration = ps.main.duration;
			}

			Destroy(vfx, vfxDuration);
		}

		roundManager.GetCoin(coinCount);
		// �R��������
		Destroy(gameObject);
	}

	// �� Player �Ʀ�Ĳ�o�I����
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (isCollected) return;

		if (collision.CompareTag("Player"))
		{
			PlayerMovement player = collision.GetComponent<PlayerMovement>();
			if (player != null && player.IsPlayerSlide())
			{
				GetCoin();
			}
			//if (player != null && player.GetSlideDirection() != 0)
			//{
			//	Vector2 playerPos = player.transform.position;
			//	Vector2 coinPos = transform.position;
			//	int slideDir = player.GetSlideDirection();

			//	// �P�_�O�_�w�q�L���� Y ����
			//	if ((slideDir == 1 && playerPos.y > coinPos.y) ||    // ���W�ơA�w�q�L
			//		(slideDir == 0 && playerPos.y < coinPos.y))     // ���U�ơA�w�q�L
			//	{
			//		GetCoin();
			//	}
			//}
		}

	}

	
}
