using UnityEngine;
using System.Collections;

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

	private void Start()
	{
		playerMovement = GetComponent<PlayerMovement>();
		attackHitBox.SetActive(false);
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
		playerMovement.DestoryFirstItem();

		// 檢測 hitbox 的範圍內有無敵人（可根據 hitbox 範圍調整）
		Collider2D[] hits = Physics2D.OverlapBoxAll(attackHitBox.transform.position,
													attackHitBox.transform.localScale,
													0f);

		foreach (Collider2D hit in hits)
		{
			if (hit.CompareTag("Enemy"))
			{
				TroubleGusetController enemy = hit.GetComponent<TroubleGusetController>();
				if (enemy != null)
				{
					enemy.TakeDamage(1);
				}
			}
		}

		yield return new WaitForSeconds(attackDuration);
		attackHitBox.SetActive(false);
	}
}
