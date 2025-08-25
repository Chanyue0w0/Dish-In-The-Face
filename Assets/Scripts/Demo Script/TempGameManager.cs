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


	// �x�s�ثe���D���\�I��T
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

			// �H���ู (1~6)
			int tableNumber = Random.Range(1, 7);

			// �]�w TextMeshPro ��ܮู
			TextMeshProUGUI text = dish.GetComponentInChildren<TextMeshProUGUI>();
			if (text != null)
			{
				text.text = tableNumber.ToString();
			}

			// �[�J active dishes list
			string dishName = dishPrefabs[randIndex].name;
			activeDishes.Add(new DishEntry
			{
				dishName = dishName,
				tableNumber = tableNumber,
				dishObject = dish
			});
		}
	}

	//  �ھ��\�I�W�٩M�ู�R�� dish�]���\�u�R���@�ӡ^
	public void FinishDish(string dishName, int tableNumber)
	{

		Debug.Log($"Table: {tableNumber} served!!!!!");
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
				Debug.Log($"Finished dish: {dishName}, Table: {tableNumber}");
				return;
			}
		}

		Debug.Log($"Dish not found: {dishName}, Table: {tableNumber}");
	}

	// �\�I��Ƶ��c
	private class DishEntry
	{
		public string dishName;
		public int tableNumber;
		public GameObject dishObject;
	}
}
