using UnityEngine;
using System.Collections;
using FoodsGroup;
public class PlayerAttackController : MonoBehaviour
{
	[Header("--------- Setting -----------")]
	[SerializeField] private float attackDuration = 0.4f; // 可調整攻擊持續時間
	[SerializeField] private float beerVFXDuration = 2f;
	[SerializeField] private LayerMask enemyLayer; // 指定敵人 Layer，防止誤判
	[Header("--------- Reference -----------")]
	[SerializeField] private Transform handItem;
	[SerializeField] private GameObject attackHitBox;
	[SerializeField] private GameObject cakeVFX;
	[SerializeField] private GameObject BeerVFX;
	[SerializeField] private PlayerMovement playerMovement;

	//private bool isAttacked = false;
	private void Start()
	{
		attackHitBox.SetActive(false);

		//isAttacked = false;
	}

	public bool IsAttackSuccess()
	{
		if (handItem.childCount == 0) return false;

		FoodType foodType = handItem.GetComponentInChildren<FoodStatus>().foodType;

		switch(foodType)
		{
			case FoodType.Pie:
				VFXPool.Instance.SpawnVFX("Cake", attackHitBox.transform.position, Quaternion.identity, 2f);
				AudioManager.Instance.PlayOneShot(FMODEvents.Instance.pieAttack, transform.position);
				StartCoroutine(PerformAttack());
				break;

			case FoodType.Beer:
				VFXPool.Instance.SpawnVFX("Beer", attackHitBox.transform.position, Quaternion.identity, beerVFXDuration);
				AudioManager.Instance.PlayOneShot(FMODEvents.Instance.beerAttack, transform.position);
				StartCoroutine(PerformAttack());
				break;
			default:
				Debug.Log("This food has not attack");
				break;
		}

		if (foodType == FoodType.Pie )
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
