using System.Collections;
using UnityEngine;

public class NormalGuestController : MonoBehaviour
{
	[Header("Setting")]
	[SerializeField] private float minPatience = 10f;
	[SerializeField] private float maxPatience = 20f;
	[SerializeField] private float eatTime = 10f;
	[SerializeField] private float moveSpeed = 2f; // 客人移動速度

	[Header("Reference")]
	[SerializeField] private RoundManager roundManager;

	[SerializeField] private Transform startPosition;
	[SerializeField] private GameObject orderIconObject;
	[SerializeField] private SpriteRenderer foodSpriteRenderer;
	[SerializeField] private GameObject patienceBar;
	[SerializeField] private Transform barFill; // 指向 orderIcon 下的 barFill

	private Transform targetChair;
	private float patienceTime;
	private float timer = 0f;
	private bool isSeated = false;
	private bool isEating = false;
	private bool isLeaving = false;
	private Sprite orderFoodSprite = null;

	void Awake()
	{
		patienceTime = Random.Range(minPatience, maxPatience);
		//transform.position = startPosition.position;

		roundManager = GameObject.Find("Rround Manager").GetComponent<RoundManager>();
		startPosition = GameObject.Find("Door Position").GetComponent<Transform>();
		targetChair = roundManager.chairGroupManager.FindEmptyChair();

		orderIconObject.SetActive(false);
		patienceBar.SetActive(false);
		if (targetChair != null)
		{
			StartCoroutine(MoveToChair(targetChair.position));
		}
		else
		{
			Leave(); // 沒椅子就離場
		}
	}

	void Update()
	{
		if (isSeated && !isEating)
		{
			timer += Time.deltaTime;

			// 更新讀條（讀條比例 = 剩餘耐心 / 總耐心）
			float ratio = Mathf.Clamp01(1f - (timer / patienceTime));
			barFill.localScale = new Vector3(ratio, 1f, 1f);

			if (timer >= patienceTime)
			{
				Leave(); // 耐心耗盡
			}
		}
	}

	private IEnumerator MoveToChair(Vector3 chairPos)
	{
		while (Vector3.Distance(transform.position, chairPos) > 0.05f)
		{
			transform.position = Vector3.MoveTowards(transform.position, chairPos, Time.deltaTime * moveSpeed);
			yield return null;
		}

		transform.position = chairPos;
		isSeated = true;

		// 點餐
		OrderDish();
		// 重設讀條長度為滿
		patienceBar.SetActive(true);
		barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	private void OrderDish()
	{
		transform.SetParent(targetChair);
		orderIconObject.SetActive(true);
		orderFoodSprite = roundManager.foodsGroupManager.OrderFoodRandomly();
		foodSpriteRenderer.sprite = orderFoodSprite;
	}

	public bool IsReceiveFood(Sprite foods)
	{
		// 確認點餐中，且是所需餐點
		if (!isSeated || isEating || foods != orderFoodSprite) return false;

		isEating = true;
		orderIconObject.SetActive(false); // 收起點餐圖示
		patienceBar.SetActive(false);
		roundManager.FinishDishSuccess();
		StopAllCoroutines();
		StartCoroutine(EatAndLeave());
		return true;
	}

	private IEnumerator EatAndLeave()
	{
		yield return new WaitForSeconds(eatTime);
		Leave();
	}

	private void Leave()
	{
		if (isLeaving) return;

		isLeaving = true;

		if (targetChair != null)
		{
			roundManager.chairGroupManager.ReleaseChair(targetChair);
			roundManager.chairGroupManager.ClearChairItem(targetChair);
			targetChair = null;
		}
		StartCoroutine(MoveOut());
	}

	private IEnumerator MoveOut()
	{
		Vector3 exitPos = startPosition.position + new Vector3(10f, 0f, 0f);
		while (Vector3.Distance(transform.position, exitPos) > 0.05f)
		{
			transform.position = Vector3.MoveTowards(transform.position, exitPos, Time.deltaTime * moveSpeed);
			yield return null;
		}

		Destroy(gameObject);
	}

}
