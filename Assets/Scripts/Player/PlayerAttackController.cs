using UnityEngine;
using FoodsGroup;
public class PlayerAttackController : MonoBehaviour
{
	[Header("--------- Setting -----------")]
	//[SerializeField] private float attackMoveDistance = 5f;
	//[SerializeField] private float attackMoveSpeed = 1f;
	[SerializeField] private LayerMask enemyLayer; // 指定敵人 Layer，防止誤判
	[Header("--------- Reference -----------")]
	[SerializeField] private Transform handItem;
	[SerializeField] private GameObject attackHitBox;
	[SerializeField] private GameObject cakeVFX;
	[SerializeField] private GameObject BeerVFX;
	[SerializeField] private PlayerMovement playerMovement;
	[SerializeField] private PlayerSpineAnimationManager animationManager;

	private int cakeComboIndex = 0; // 0: 播 DrumStick1, 1: 播 DrumStick2
	
	//private bool isAttacked = false;
	private void Start()
	{
		attackHitBox.SetActive(false);
		//isAttacked = false;
	}
	public bool IsAttackSuccess()
	{
		animationManager = GetComponent<PlayerSpineAnimationManager>();
		if (handItem.childCount == 0) return false;

		FoodType foodType = handItem.GetComponentInChildren<FoodStatus>().foodType;

		switch (foodType)
		{
			case FoodType.Beer:
			case FoodType.Pie:
				{
					if (animationManager.IsBusy())
						return false;
					
					// 攻擊成功
					handItem.gameObject.SetActive(false);
					playerMovement.SetEnableMoveControll(false);
					AudioManager.Instance.PlayOneShot(FMODEvents.Instance.pieAttack, transform.position);
					//playerMovement.MoveDistance(attackMoveDistance, attackMoveSpeed, Vector2.zero);

					// 交替播放 DrumStick1 / DrumStick2
					string anim = (cakeComboIndex % 2 == 0)
						? animationManager.DrumStick1
						: animationManager.DrumStick2;

					// 撥放一次；HitBox 與 VFX 交給 Spine 事件：
					animationManager.PlayAnimationOnce(anim, attackHitBox, "Cake", () =>
					{
						// 動畫播完：切換 combo index
						cakeComboIndex = 1 - cakeComboIndex;

						// 播完後再顯示 handItem
						handItem.gameObject.SetActive(true);
						animationManager.UpdateFromMovement(Vector2.zero);
						playerMovement.SetEnableMoveControll(true);

					});

					break;
				}

			//case FoodType.Beer:
			//	{
			//		// 保持原行為（VFX 立刻生成 + 時間窗 HitBox）
			//		VFXPool.Instance.SpawnVFX("Beer", attackHitBox.transform.position, Quaternion.identity, beerVFXDuration);
			//		AudioManager.Instance.PlayOneShot(FMODEvents.Instance.beerAttack, transform.position);
			//		StartCoroutine(PerformAttack());
			//		break;
			//	}

			default:
				Debug.Log("This food has not attack");
				return false;
		}

		return true;
	}


	//private IEnumerator PerformAttack()
	//{
	//	attackHitBox.SetActive(true);

	//	yield return new WaitForSeconds(attackDuration);
	//	attackHitBox.SetActive(false);
	//	//isAttacked = false;
	//}
}
