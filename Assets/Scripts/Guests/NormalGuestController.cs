using System.Collections;
using UnityEngine;

public class NormalGuestController : MonoBehaviour
{
	[Header("Setting")]
	[SerializeField] private float minPatience = 10f;
	[SerializeField] private float maxPatience = 20f;
	[SerializeField] private float eatTime = 10f;
	[SerializeField] private float moveSpeed = 2f; // �ȤH���ʳt��

	[Header("Reference")]
	[SerializeField] private Transform startPosition;
	[SerializeField] private GuestGroupManager guestGroupManager;
	[SerializeField] private FoodsGroupManager foodsGroupManager;
	[SerializeField] private GameObject orderIconObject;
	[SerializeField] private SpriteRenderer foodSpriteRenderer;

	[SerializeField] private Transform barFill; // ���V orderIcon �U�� barFill

	private Transform targetChair;
	private float patienceTime;
	private float timer = 0f;
	private bool isSeated = false;
	private bool isEating = false;
	private bool isLeaving = false;

	void Start()
	{
		patienceTime = Random.Range(minPatience, maxPatience);
		transform.position = startPosition.position;
		targetChair = guestGroupManager.FindEmptyChair();

		orderIconObject.SetActive(false);

		if (targetChair != null)
		{
			StartCoroutine(MoveToChair(targetChair.position));
		}
		else
		{
			Leave(); // �S�Ȥl�N����
		}
	}

	void Update()
	{
		if (isSeated && !isEating)
		{
			timer += Time.deltaTime;

			// ��sŪ���]Ū����� = �Ѿl�@�� / �`�@�ߡ^
			float ratio = Mathf.Clamp01(1f - (timer / patienceTime));
			if (barFill != null)
			{
				barFill.localScale = new Vector3(ratio, 1f, 1f);
			}

			if (timer >= patienceTime)
			{
				Leave(); // �@�߯Ӻ�
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

		// ����I�\�P����
		orderIconObject.SetActive(true);
		Sprite foodSprite = foodsGroupManager.OrderFoodRandomly();
		foodSpriteRenderer.sprite = foodSprite;

		// ���]Ū�����׬���
		if (barFill != null)
			barFill.localScale = new Vector3(1f, 1f, 1f);
	}

	public void ReceiveFood()
	{
		if (!isSeated || isEating) return;

		isEating = true;
		orderIconObject.SetActive(false); // ���_�I�\�ϥ�
		StopAllCoroutines();
		StartCoroutine(EatAndLeave());
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
		guestGroupManager.ReleaseChair(targetChair);
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
