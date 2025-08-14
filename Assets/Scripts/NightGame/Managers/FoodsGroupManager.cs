using UnityEngine;

public class FoodsGroupManager : MonoBehaviour
{
	//[Header("-------- Setting ---------")]
	//[SerializeField] private float spawnInerval = 10f;
	//[SerializeField] private int spawnFoodsCount = 10;

	[Header("-------- Reference ---------")]
	[SerializeField] private GameObject[] foodsArray;
	[SerializeField] private Transform barFill;
	[SerializeField] private Transform DishLoadingBar;

	[Header("-------- Highlight ---------")]
	[SerializeField] private GameObject yellowFrame;
	[SerializeField] private Collider2D playerCollider; // ���a�ۤv���I����
	[SerializeField] private LayerMask foodLayerMask;   // ������ Layer

	private Transform currentFoodTarget = null;
	private int[] foodsCount;
	private bool isPlayerInsideTrigger = false;

	void Start()
	{
		foodsCount = new int[foodsArray.Length];
		for (int i = 0; i < foodsCount.Length; i++)
			foodsCount[i] = 10;

		if (yellowFrame != null)
			yellowFrame.SetActive(false);
	}

	void Update()
	{
		if (!isPlayerInsideTrigger)
		{
			if (yellowFrame.activeSelf)
				yellowFrame.SetActive(false);
			return;
		}

		// �ƹ��u��
		Transform mouseTarget = GetHoveredFoodByMouse();
		if (mouseTarget != null)
		{
			UpdateYellowFrame(mouseTarget);
			return;
		}

		// ���a�I�������n
		Transform touchedTarget = GetTouchedFoodByPlayer();
		if (touchedTarget != null)
		{
			UpdateYellowFrame(touchedTarget);
		}
		else
		{
			if (yellowFrame.activeSelf)
				yellowFrame.SetActive(false);
		}
	}

	// --- �ƹ� hover �ˬd ---
	private Transform GetHoveredFoodByMouse()
	{
		Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

		// �u�j�M���w Layer�]�q�`�O Food�^
		Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, foodLayerMask);
		if (hit != null)
		{
			return hit.transform;
		}
		return null;
	}

	// --- ���a�I���ˬd ---
	private Transform GetTouchedFoodByPlayer()
	{
		foreach (GameObject food in foodsArray)
		{
			if (food == null) continue;

			Collider2D foodCollider = food.GetComponent<Collider2D>();
			if (foodCollider != null && foodCollider.isTrigger && foodCollider.IsTouching(playerCollider))
			{
				return food.transform;
			}
		}
		return null;
	}

	// --- ���ʶ��� ---
	private void UpdateYellowFrame(Transform target)
	{
		currentFoodTarget = target;

		if (!yellowFrame.activeSelf)
			yellowFrame.SetActive(true);

		yellowFrame.transform.position = currentFoodTarget.position;
	}

	// --- Trigger �ˬd�i�X ---
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other == playerCollider)
			isPlayerInsideTrigger = true;
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (other == playerCollider)
		{
			isPlayerInsideTrigger = false;
			if (yellowFrame.activeSelf)
				yellowFrame.SetActive(false);
		}
	}

	// --- �H���I�\�]���o Sprite�^---
	public Sprite OrderFoodRandomly()
	{
		if (foodsArray == null || foodsArray.Length == 0)
			return null;

		int randomIndex = Random.Range(0, foodsArray.Length);
		GameObject selectedFood = foodsArray[randomIndex];

		SpriteRenderer foodSR = selectedFood.GetComponent<SpriteRenderer>();
		return foodSR != null ? foodSR.sprite : null;
	}

	/// �^�ǥثe�Q yellowFrame ��w������ GameObject�A�p�G�S���h�^�� null
	public GameObject GetCurrentDishObject()
	{
		return currentFoodTarget != null ? currentFoodTarget.gameObject : null;
	}
}
