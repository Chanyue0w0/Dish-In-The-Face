using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FoodsGroupManager : MonoBehaviour
{

	[Header("-------- Setting ---------")]
	[SerializeField] private float spawnInerval = 10f;      // 每幾秒生成（預設10秒）
	[SerializeField] private int spawnFoodsCount = 10;           // 每次生成的數量（預設10份）


	[Header("-------- Reference ---------")]
	[SerializeField] private GameObject[] foodsArray;         // 食物的 prefabs 陣列
	[SerializeField] private TextMeshPro[] foodsCountText;
	[SerializeField] private Transform barFill;
	[SerializeField] private Transform DishLoadingBar;

	private int[] foodsCount; // 對應每種餐點目前的數量
	private float timer = 0f;


	void Start()
	{
		foodsCount = new int[foodsArray.Length];
		for (int i = 0; i < foodsCount.Length; i++)
			foodsCount[i] = 10;
		UpdateAllFoodTexts(); // 初始化時也更新顯示
	}


	void Update()
	{
		timer += Time.deltaTime;


		// 更新讀條長度
		UpdateLoadingBar(timer / spawnInerval);

		if (timer >= spawnInerval)
		{
			RefillFoods();
			UpdateAllFoodTexts();
			timer = 0f;
		}
	}


	private void RefillFoods()
	{
		for (int i = 0; i < foodsCount.Length; i++)
		{
			foodsCount[i] += spawnFoodsCount;
		}

		UpdateAllFoodTexts(); // 每次補餐時自動更新顯示
		//Debug.Log("補充餐點完成，每種餐點 +" + spawnCount);
	}


	private void UpdateAllFoodTexts()
	{
		for (int i = 0; i < foodsCount.Length; i++)
		{
			if (i < foodsCountText.Length)
			{
				foodsCountText[i].text = foodsCount[i].ToString();
			}
		}
	}

	private void UpdateLoadingBar(float ratio)
	{
		// 限制 ratio 在 0~1 之間
		ratio = Mathf.Clamp01(ratio);

		// barFill 原本 scale 為 (1, 1, 1)，用 x 縮放控制進度
		barFill.localScale = new Vector3(ratio, 1f, 1f);
	}


	// 更改餐點數量 by index
	public void SetFoodCount(int index, int count)
	{
		if (index >= 0 && index < foodsCount.Length)
		{
			foodsCount[index] = count;
		}
	}

	// 更改餐點數量 by name
	public void SetFoodCountByName(string name, int count)
	{
		for (int i = 0; i < foodsArray.Length; i++)
		{
			if (foodsArray[i].name == name)
			{
				foodsCount[i] = count;
				foodsCountText[i].text = count.ToString();
				break;
			}
		}
	}

	// 更改每幾秒生成
	public void SetRefillInterval(float interval)
	{
		spawnInerval = interval;
	}

	// 更改每次生成的數量
	public void SetRefillAmount(int amount)
	{
		spawnFoodsCount = amount;
	}
}
