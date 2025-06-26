using UnityEngine;
using System.Collections;

public class PlayerAttackController : MonoBehaviour
{
	[SerializeField] private Transform handItem;
	[SerializeField] private GameObject attackHitBox;
	[SerializeField] private float attackDuration = 0.4f; // 可調整攻擊持續時間
	[SerializeField] private LayerMask enemyLayer; // 指定敵人 Layer，防止誤判

	private void Start()
	{
		attackHitBox.SetActive(false);
	}

	public bool IsAttackSuccess()
	{
		if (handItem.childCount == 0) return false;

		FoodsGroupManager.FoodType foodType = handItem.GetChild(0).GetComponent<FoodStatus>().foodType;

		if (foodType == FoodsGroupManager.FoodType.Pie)
		{
			StartCoroutine(PerformAttack());
		}

		return true;
	}

	private IEnumerator PerformAttack()
	{
		attackHitBox.SetActive(true);

		// 檢測 hitbox 的範圍內有無敵人（可根據 hitbox 範圍調整）
		Collider2D[] hits = Physics2D.OverlapBoxAll(attackHitBox.transform.position,
													attackHitBox.transform.localScale,
													0f);
		Debug.Log("can attack");

		foreach (Collider2D hit in hits)
		{
			if (hit.CompareTag("Enemy"))
			{
				TroubleGusetController enemy = hit.GetComponent<TroubleGusetController>();
				if (enemy != null)
				{
					Debug.Log("attack success");
					enemy.TakeDamage(1);
				}
			}
		}

		yield return new WaitForSeconds(attackDuration);
		attackHitBox.SetActive(false);
	}
}
