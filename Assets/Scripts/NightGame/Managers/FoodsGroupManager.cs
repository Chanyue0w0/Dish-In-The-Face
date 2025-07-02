using TMPro;
using UnityEngine;

public class FoodsGroupManager : MonoBehaviour
{
	public enum FoodType
	{
		Beer,
		Cake,
		Pizza,
		Pie,
		Bread
	}


	[Header("-------- Setting ---------")]
	[SerializeField] private float spawnInerval = 10f;      // �C�X��ͦ��]�w�]10��^
	[SerializeField] private int spawnFoodsCount = 10;           // �C���ͦ����ƶq�]�w�]10���^


	[Header("-------- Reference ---------")]
	[SerializeField] private GameObject[] foodsArray;         // ������ prefabs �}�C
	[SerializeField] private TextMeshPro[] foodsCountText;
	[SerializeField] private Transform barFill;
	[SerializeField] private Transform DishLoadingBar;

	private int[] foodsCount; // �����C���\�I�ثe���ƶq
	private float timer = 0f;


	void Start()
	{
		foodsCount = new int[foodsArray.Length];
		for (int i = 0; i < foodsCount.Length; i++)
			foodsCount[i] = 10;
		UpdateAllFoodTexts(); // ��l�Ʈɤ]��s���
	}


	void Update()
	{
		timer += Time.deltaTime;


		// ��sŪ������
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

		UpdateAllFoodTexts(); // �C�����\�ɦ۰ʧ�s���
		//Debug.Log("�ɥR�\�I�����A�C���\�I +" + spawnCount);
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
		// ���� ratio �b 0~1 ����
		ratio = Mathf.Clamp01(ratio);

		// barFill �쥻 scale �� (1, 1, 1)�A�� x �Y�񱱨�i��
		barFill.localScale = new Vector3(ratio, 1f, 1f);
	}


	// ����\�I�ƶq by index
	public void SetFoodCount(int index, int count)
	{
		if (index >= 0 && index < foodsCount.Length)
		{
			foodsCount[index] = count;
		}
	}

	// ����\�I�ƶq by name
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

	// ���C�X��ͦ�
	public void SetRefillInterval(float interval)
	{
		spawnInerval = interval;
	}

	// ���C���ͦ����ƶq
	public void SetRefillAmount(int amount)
	{
		spawnFoodsCount = amount;
	}

	public Sprite OrderFoodRandomly()
	{
		// �T�O���\�I�i�H���
		if (foodsArray == null || foodsArray.Length == 0)
			return null;

		// �H����@�ӯ���
		int randomIndex = Random.Range(0, foodsArray.Length);

		// ���o������ prefab
		GameObject selectedFood = foodsArray[randomIndex].transform.GetChild(0).gameObject;

		// ���]�n�䪺 SpriteRenderer �O�l���󪺤l����
		// �ϥ� GetComponentsInChildren �ò��L�ۤv����
		SpriteRenderer[] sprites = selectedFood.GetComponentsInChildren<SpriteRenderer>(true);

		foreach (var sr in sprites)
		{
			if (sr.gameObject != selectedFood) // �T�O���O prefab �ۤv
				return sr.sprite;
		}

		return null; // �p�G�䤣��N�^�� null
	}

}
