using UnityEngine;

public class FoodsGroupManager : MonoBehaviour
{
	//[Header("-------- Setting ---------")]
	//[SerializeField] private float spawnInerval = 10f;
	//[SerializeField] private int spawnFoodsCount = 10;

	[Header("-------- Dessert Cooldown ---------")]
	[SerializeField, Min(0f)] private float dessertColdownSeconds = 5f; // �N�o���
	[SerializeField] private bool hideBarWhenReady = true;               // �N�o�����۰�����

	[Header("-------- Reference ---------")]
	[SerializeField] private GameObject[] currentfoods;
	[SerializeField] private GameObject[] foodPrefabs;
	[SerializeField] private Transform[] foodsSpawnPosition;
	[SerializeField] private Transform dessertBarFill;
	//[SerializeField] private Transform dishLoadingBar;
	[SerializeField] private Animator dessertAnimator;

	[Header("-------- Highlight ---------")]
	[SerializeField] private GameObject yellowFrame;
	[SerializeField] private Collider2D playerCollider; // ���a�ۤv���I����
	[SerializeField] private LayerMask foodLayerMask;   // ������ Layer

	private Transform currentFoodTarget = null;
	private int[] foodsCount;
	private bool isPlayerInsideTrigger = false;

	private float dessertCdRemain = 0f;  // �Ѿl�N�o�ɶ�
	private bool IsDessertOnCd => dessertCdRemain > 0f;
	void Start()
	{
		foodsCount = new int[currentfoods.Length];
		for (int i = 0; i < foodsCount.Length; i++)
			foodsCount[i] = 10;

		if (yellowFrame != null)
			yellowFrame.SetActive(false);

		// ��l��Ū�����A
		if (dessertBarFill != null) dessertBarFill.gameObject.SetActive(false);
		if (dessertBarFill != null) dessertBarFill.localScale = new Vector3(0f, 1f, 1f); // ��l 0�]���b�N�o�^
	}

	void Update()
	{
		UpdateDesertColdDown();

		if (!isPlayerInsideTrigger)
		{
			if (yellowFrame.activeSelf)
				yellowFrame.SetActive(false);
			currentFoodTarget = null;
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

	private void UpdateDesertColdDown()
	{
		// --- ��s���I�N�o�� ---
		if (IsDessertOnCd)
		{
			dessertCdRemain = Mathf.Max(0f, dessertCdRemain - Time.deltaTime);

			// �i�� = �Ѿl�ɶ� / �`�N�o �� �q 1 ����� 0
			float p = (dessertColdownSeconds <= 0f) ? 0f : (dessertCdRemain / dessertColdownSeconds);
			if (dessertBarFill != null)
				dessertBarFill.localScale = new Vector3(Mathf.Clamp01(p), 1f, 1f);

			// �N�o���� �� ���é���ܪű�
			if (!IsDessertOnCd)
			{
				if (dessertBarFill != null)
					dessertBarFill.localScale = new Vector3(0f, 1f, 1f); // �ܦ���
				if (dessertBarFill != null && hideBarWhenReady)
					dessertBarFill.gameObject.SetActive(false);
			}
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
		foreach (GameObject food in currentfoods)
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
			{
				yellowFrame.SetActive(false);
				currentFoodTarget = null;
			}
		}
	}

	// --- �H���I�\�]���o Sprite�^---
	public Sprite OrderFoodRandomly()
	{
		if (foodPrefabs == null || foodPrefabs.Length == 0)
			return null;

		int randomIndex = Random.Range(0, foodPrefabs.Length);
		GameObject selectedFood = foodPrefabs[randomIndex];

		SpriteRenderer foodSR = selectedFood.GetComponent<SpriteRenderer>();
		return foodSR != null ? foodSR.sprite : null;
	}

	/// �^�ǥثe�Q yellowFrame ��w������ GameObject�A�p�G�S���h�^�� null
	public GameObject GetCurrentDishObject()
	{
		return currentFoodTarget != null ? currentFoodTarget.gameObject : null;
	}

	// �ϥεo�I��
	// �ϥεo�I�ߡ]�a�N�o��Ū���^
	public bool UseDessert()
	{
		// �N�o���N��Ĳ�o
		if (IsDessertOnCd) return false;

		// ����ʵe�]�q�Y�^
		if (dessertAnimator != null)
			dessertAnimator.Play("DessertEffect", -1, 0f);

		RoundManager.Instance.chairGroupManager.ResetAllSetGuestsPatience();

		// �}�l�N�o
		StartDessertColdown();
		return true;
	}

	private void StartDessertColdown()
	{
		if (dessertColdownSeconds <= 0f)
		{
			dessertCdRemain = 0f;
			if (dessertBarFill != null) dessertBarFill.localScale = new Vector3(0f, 1f, 1f); // �@�}�l�N�O��
			if (dessertBarFill != null && hideBarWhenReady)
				dessertBarFill.gameObject.SetActive(false);
			return;
		}

		dessertCdRemain = dessertColdownSeconds;

		// ���Ū���ñq���}�l
		if (dessertBarFill != null) dessertBarFill.gameObject.SetActive(true);
		if (dessertBarFill != null) dessertBarFill.localScale = new Vector3(1f, 1f, 1f);
	}

}
