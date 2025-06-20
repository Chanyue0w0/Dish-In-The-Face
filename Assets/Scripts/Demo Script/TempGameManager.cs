using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TempGameManager : MonoBehaviour
{
	[Header("Enemy")]
	[SerializeField] private GameObject enemyPrefab;
	[SerializeField] private float enemySpawnInterval = 4f;

	[Header("Dish")]
	[SerializeField] private GameObject dishListObject;
	[SerializeField] private List<GameObject> dishPrefabs;
	[SerializeField] private float dishSpawnInterval = 5f;


	// 儲存目前活躍的餐點資訊
	private List<DishEntry> activeDishes = new List<DishEntry>();

	private void Start()
	{
		StartCoroutine(SpawnEnemyRoutine());
		StartCoroutine(SpawnDishRoutine());
	}

	private IEnumerator SpawnEnemyRoutine()
	{
		while (true)
		{
			Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
			yield return new WaitForSeconds(enemySpawnInterval);
		}
	}

	private IEnumerator SpawnDishRoutine()
	{
		while (true)
		{
			yield return new WaitForSeconds(dishSpawnInterval);

			if (dishPrefabs.Count == 0 || dishListObject == null) continue;

			int randIndex = Random.Range(0, dishPrefabs.Count);
			GameObject dish = Instantiate(dishPrefabs[randIndex], dishListObject.transform);

			// 隨機桌號 (1~6)
			int tableNumber = Random.Range(1, 7);

			// 設定 TextMeshPro 顯示桌號
			TextMeshProUGUI text = dish.GetComponentInChildren<TextMeshProUGUI>();
			if (text != null)
			{
				text.text = tableNumber.ToString();
			}

			// 加入 active dishes list
			string dishName = dishPrefabs[randIndex].name;
			activeDishes.Add(new DishEntry
			{
				dishName = dishName,
				tableNumber = tableNumber,
				dishObject = dish
			});
		}
	}

	//  根據餐點名稱和桌號刪除 dish（成功只刪除一個）
	public void FinishDish(string dishName, int tableNumber)
	{

		Debug.Log($"桌號：{tableNumber} 上菜!!!!!");
		for (int i = 0; i < activeDishes.Count; i++)
		{
			var entry = activeDishes[i];
			if (entry.dishName == dishName && entry.tableNumber-1 == tableNumber)
			{
				if (entry.dishObject != null)
				{
					Destroy(entry.dishObject);
				}
				activeDishes.RemoveAt(i);
				Debug.Log($"完成餐點：{dishName}，桌號：{tableNumber}");
				return;
			}
		}

		Debug.Log($"找不到餐點：{dishName}，桌號：{tableNumber}");
	}

	// 餐點資料結構
	private class DishEntry
	{
		public string dishName;
		public int tableNumber;
		public GameObject dishObject;
	}
}
