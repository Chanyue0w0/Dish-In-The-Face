using UnityEngine;
using FoodsGroup;
public class PlayerAttackController : MonoBehaviour
{
	[Header("--------- Setting -----------")]
	[SerializeField] private float attackMoveDistance = 5f;
	[SerializeField] private float attackMoveDuration = 0.5f;
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
			case FoodType.Drumstick:
				{
					if (animationManager.IsBusy())
						return false;

					// �������\
					if (cakeComboIndex > 0) playerMovement.DestoryFirstItem(); // �ϥα��Ĥ@�ӭ���
					handItem.gameObject.SetActive(false);
					playerMovement.SetEnableMoveControll(false);
					AudioManager.Instance.PlayOneShot(FMODEvents.Instance.pieAttack, transform.position);
					playerMovement.MoveDistance(attackMoveDistance, attackMoveDuration, playerMovement.GetMoveInput());

					// ������� DrumStick1 / DrumStick2
					string anim = (cakeComboIndex % 2 == 0)
						? animationManager.DrumStick1
						: animationManager.DrumStick2;

					// �����S�ĦW��
					string vfxName = (cakeComboIndex % 2 == 0)
						? "DrumStick_NormalAttack_1"
						: "DrumStick_NormalAttack_2";

					//VFXPool.Instance.SpawnVFX(vfxName, attackHitBox.transform.position, Quaternion.identity, 2f);

					// ����@���FHitBox �P VFX �浹 Spine �ƥ�G
					animationManager.PlayAnimationOnce(anim, attackHitBox, vfxName, () =>
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
