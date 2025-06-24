using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FoodsGroupManager : MonoBehaviour
{
	// -------- Setting --------
	[SerializeField] private float spawnInerval = 10f;      // –碭ネΘ箇砞10
	[SerializeField] private int spawnCount = 10;           // –ΩネΘ计秖箇砞10

	// -------- Reference --------
	[SerializeField] private GameObject[] foodsArray;         //  prefabs 皚
	[SerializeField] private TextMeshPro[] foodsCountText;

	private int[] foodsCount; // 癸莱–贺繺翴ヘ玡计秖
	private float timer = 0f;

	void Start()
	{
		foodsCount = new int[foodsArray.Length];
		for (int i = 0; i < foodsCount.Length; i++)
			foodsCount[i] = 10;
		UpdateAllFoodTexts(); // ﹍て穝陪ボ
	}


	void Update()
	{
		timer += Time.deltaTime;
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
			foodsCount[i] += spawnCount;
		}

		UpdateAllFoodTexts(); // –Ω干繺笆穝陪ボ
		Debug.Log("干繺翴ЧΘ–贺繺翴 +" + spawnCount);
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

	// э繺翴计秖 by index
	public void SetFoodCount(int index, int count)
	{
		if (index >= 0 && index < foodsCount.Length)
		{
			foodsCount[index] = count;
		}
	}

	// э繺翴计秖 by name
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

	// э–碭ネΘ
	public void SetRefillInterval(float interval)
	{
		spawnInerval = interval;
	}

	// э–ΩネΘ计秖
	public void SetRefillAmount(int amount)
	{
		spawnCount = amount;
	}
}
