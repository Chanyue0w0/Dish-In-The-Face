using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TempGameManager : MonoBehaviour
{
	[Header("Enemy")]
	public GameObject enemyPrefab;
	public float enemySpawnInterval = 4f;

	[Header("Dish")]
	public GameObject dishListObject;
	public List<GameObject> dishPrefabs;
	public float dishSpawnInterval = 5f;

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

			// �]�w�ู��r�]1~6�^
			int tableNumber = Random.Range(1, 7); // �]�t 6

			// ���o TextMeshPro �M�]�w�ู
			TextMeshProUGUI text = dish.GetComponentInChildren<TextMeshProUGUI>();
			if (text != null)
			{
				text.text = "�ู " + tableNumber.ToString();
			}
		}
	}
}
