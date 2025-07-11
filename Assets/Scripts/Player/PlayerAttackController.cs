using UnityEngine;
using System.Collections;

public class PlayerAttackController : MonoBehaviour
{
	[Header("--------- Setting -----------")]
	[SerializeField] private float attackDuration = 0.4f; // �i�վ��������ɶ�
	[SerializeField] private float beerVFXDuration = 2f;
	[SerializeField] private LayerMask enemyLayer; // ���w�ĤH Layer�A����~�P
	[Header("--------- Reference -----------")]
	[SerializeField] private Transform handItem;
	[SerializeField] private GameObject attackHitBox;
	[SerializeField] private GameObject cakeVFX;
	[SerializeField] private GameObject BeerVFX;
	[SerializeField] private PlayerMovement playerMovement;

	//private bool isAttacked = false;
	private void Start()
	{
		playerMovement = GetComponent<PlayerMovement>();
		attackHitBox.SetActive(false);

		//isAttacked = false;
	}

	public bool IsAttackSuccess()
	{
		if (handItem.childCount == 0) return false;

		FoodsGroupManager.FoodType foodType = handItem.GetChild(0).GetComponent<FoodStatus>().foodType;

		switch(foodType)
		{
			case FoodsGroupManager.FoodType.Pie:
				Instantiate(cakeVFX, attackHitBox.transform.position, Quaternion.identity);
				StartCoroutine(PerformAttack());
				break;
			case FoodsGroupManager.FoodType.Beer:
				GameObject vfx =Instantiate(BeerVFX, attackHitBox.transform.position, Quaternion.identity);
				Destroy(vfx, beerVFXDuration);
				StartCoroutine(PerformAttack());
				break;
			default:
				Debug.Log("This food has not attack");
				break;
		}

		if (foodType == FoodsGroupManager.FoodType.Pie )
		{
			StartCoroutine(PerformAttack());
		}

		return true;
	}

	private IEnumerator PerformAttack()
	{
		attackHitBox.SetActive(true);

		yield return new WaitForSeconds(attackDuration);
		attackHitBox.SetActive(false);
		//isAttacked = false;
	}
}
