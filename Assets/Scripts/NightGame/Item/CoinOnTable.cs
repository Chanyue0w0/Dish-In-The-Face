using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinOnTable : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private float vfxDuration = 1.0f; // 特效持續時間（秒）

	[Header("Reference")]
	[SerializeField] private GameObject coinVFXPrefab; // 爆金幣特效 prefab

	[SerializeField] private RoundManager roundManager;
	private bool isCollected = false; // 防止多次觸發

	private int coinCount;

	public void Awake()
	{
		roundManager = GameObject.Find("Rround Manager").GetComponent<RoundManager>();
	}

	/// 在生成後設定金幣數量
	public void SetCoinCount(int value)
	{
		coinCount = value;
	}
	/// 生成爆金幣特效，特效結束後刪除本物件
	public void GetCoin()
	{
		if (isCollected) return;
		isCollected = true;

		if (coinVFXPrefab != null)
		{
			// 生成特效
			GameObject vfx = Instantiate(coinVFXPrefab, transform.position, Quaternion.identity);

			// 播完後一起刪除（建議 VFX prefab 本身設計成會自動刪除，也可以這樣處理）
			ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
			if (ps != null)
			{
				vfxDuration = ps.main.duration;
			}

			Destroy(vfx, vfxDuration);
		}

		roundManager.GetCoin(coinCount);
		// 刪除本物件
		Destroy(gameObject);
	}

	// 當 Player 滑行觸發碰撞時
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

			//	// 判斷是否已通過金幣 Y 中心
			//	if ((slideDir == 1 && playerPos.y > coinPos.y) ||    // 往上滑，已通過
			//		(slideDir == 0 && playerPos.y < coinPos.y))     // 往下滑，已通過
			//	{
			//		GetCoin();
			//	}
			//}
		}

	}

	
}
