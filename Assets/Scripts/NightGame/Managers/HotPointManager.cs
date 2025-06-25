using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotPointManager : MonoBehaviour
{
	[SerializeField] private float hotPoint = 0f;          // 0~10 之間
	[Header("-------- Setting ---------")]
	[SerializeField] private float decreaseRate = 0.1f;    // 每秒下降的熱度
	[SerializeField] private float decayDelay = 3f;        // 等待多少秒後開始下降
	[SerializeField] private float maxHotPoint = 10f;


	[Header("-------- Reference ---------")]
	[SerializeField] private Sprite[] hotLevelSprites;     // 索引 0~4 對應 D~S
	[SerializeField] private Image hotPointImage;

	private float lastHotIncreaseTime;                     // 上次熱度增加的時間

	private void Start()
	{
		UpdateHotLevelSprite();
	}

	private void Update()
	{
		// 若超過 decayDelay 秒沒有增加熱度，則開始下降
		if (Time.time - lastHotIncreaseTime > decayDelay)
		{
			if (hotPoint > 0f)
			{
				hotPoint -= decreaseRate * Time.deltaTime;
				hotPoint = Mathf.Clamp(hotPoint, 0f, maxHotPoint);
				UpdateHotLevelSprite();
			}
		}
	}

	/// 增加熱度
	public void AddHotPoint(float value)
	{
		hotPoint += value;
		hotPoint = Mathf.Clamp(hotPoint, 0f, maxHotPoint);
		Debug.Log("hotPoint :" + hotPoint);
		lastHotIncreaseTime = Time.time;
		UpdateHotLevelSprite();
	}

	/// 根據熱度設定對應的圖示
	private void UpdateHotLevelSprite()
	{
		int levelIndex = GetHotLevelIndex();
		if (hotLevelSprites != null && hotLevelSprites.Length > levelIndex)
		{
			hotPointImage.sprite = hotLevelSprites[levelIndex];
		}
	}

	/// 根據熱度取得倍率（1~5 倍）
	public int GetMoneyMultiplier()
	{
		return GetHotLevelIndex() + 1;
	}

	/// 根據熱度取得等級索引 0(D), 1(C), ..., 4(S)
	private int GetHotLevelIndex()
	{
		if (hotPoint >= 8f) return 4; // S
		if (hotPoint >= 6f) return 3; // A
		if (hotPoint >= 4f) return 2; // B
		if (hotPoint >= 2f) return 1; // C
		return 0;                     // D
	}
}
