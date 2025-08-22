using UnityEngine;
using FoodsGroup;
public class PlayerAttackController : MonoBehaviour
{
	[Header("--------- Setting -----------")]
	//[SerializeField] private float attackMoveDistance = 5f;
	//[SerializeField] private float attackMoveSpeed = 1f;
	[SerializeField] private LayerMask enemyLayer; // ���w�ĤH Layer�A����~�P
	[Header("--------- Reference -----------")]
	[SerializeField] private Transform handItem;
	[SerializeField] private GameObject attackHitBox;
	[SerializeField] private GameObject cakeVFX;
	[SerializeField] private GameObject BeerVFX;
	[SerializeField] private PlayerMovement playerMovement;
	[SerializeField] private PlayerSpineAnimationManager animationManager;

	private int cakeComboIndex = 0; // 0: �� DrumStick1, 1: �� DrumStick2
	
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
					
					// �������\
					handItem.gameObject.SetActive(false);
					playerMovement.SetEnableMoveControll(false);
					AudioManager.Instance.PlayOneShot(FMODEvents.Instance.pieAttack, transform.position);
					//playerMovement.MoveDistance(attackMoveDistance, attackMoveSpeed, Vector2.zero);

					// ������� DrumStick1 / DrumStick2
					string anim = (cakeComboIndex % 2 == 0)
						? animationManager.DrumStick1
						: animationManager.DrumStick2;

					// ����@���FHitBox �P VFX �浹 Spine �ƥ�G
					animationManager.PlayAnimationOnce(anim, attackHitBox, "Cake", () =>
					{
						// �ʵe�����G���� combo index
						cakeComboIndex = 1 - cakeComboIndex;

						// ������A��� handItem
						handItem.gameObject.SetActive(true);
						animationManager.UpdateFromMovement(Vector2.zero);
						playerMovement.SetEnableMoveControll(true);

					});

					break;
				}

			//case FoodType.Beer:
			//	{
			//		// �O����欰�]VFX �ߨ�ͦ� + �ɶ��� HitBox�^
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
