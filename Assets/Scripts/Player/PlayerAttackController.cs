using UnityEngine;
using FoodsGroup;
using System;
using PrimeTween;
public class PlayerAttackController : MonoBehaviour
{
	public enum AttackMode
	{
		Food,
		Basic
	}

	[Header("--------- Setting -----------")]
	[SerializeField] private AttackMode attackMode = AttackMode.Food;
	[SerializeField] private float attackMoveDistance = 5f;
	[SerializeField] private float attackMoveDuration = 0.5f;
	[SerializeField] private LayerMask enemyLayer; // Enemy layer for detection

	[Header("--------- Reference -----------")]
	[SerializeField] private Transform handItem;
	[SerializeField] private GameObject foodAttackHitBox;
	[SerializeField] private GameObject basicAttackHitBox;
	[SerializeField] private GameObject cakeVFX;
	[SerializeField] private GameObject beerVFX;
	[SerializeField] private PlayerMovement playerMovement;
	[SerializeField] private PlayerSpineAnimationManager animationManager;
	[SerializeField] private Animator weaponUIAnimator;

	private int cakeComboIndex = 0; // 0: use DrumStick1, 1: use DrumStick2
	private bool isSwichWeaponFinish = true;

	//private bool isAttacked = false;
	private void Start()
	{
		isSwichWeaponFinish = true;
		foodAttackHitBox.SetActive(false);

		SetAttackModeUI(attackMode);
		//isAttacked = false;
	}
	public bool IsAttackSuccess()
	{
		animationManager = GetComponent<PlayerSpineAnimationManager>();

		if (attackMode == AttackMode.Basic)
		{
			return BasicAttack();
		}
		if (attackMode == AttackMode.Food)
		{
			if (handItem.childCount == 0) return false;
			return FoodAttacks();
		}

		return false;
	}

	private bool BasicAttack()
	{
		// �����} hitbox
		basicAttackHitBox.SetActive(true);
		VFXPool.Instance.SpawnVFX("BasicAttack", basicAttackHitBox.transform.position, transform.rotation, 1f);
		AudioManager.Instance.PlayOneShot(FMODEvents.Instance.pieAttack, transform.position);
		// 0.5 ���۰�����
		Tween.Delay(0.5f, () => basicAttackHitBox.SetActive(false));

		return true;
	}

	private bool FoodAttacks()
	{
		FoodType foodType = handItem.GetComponentInChildren<FoodStatus>().foodType;

		switch (foodType)
		{
			case FoodType.Beer:
			case FoodType.Drumstick:
				{
					if (animationManager.IsBusy())
						return false;

					// Perform attack
					if (cakeComboIndex > 0) playerMovement.DestoryFirstItem(); // Destroy first item if combo index > 0
					handItem.gameObject.SetActive(false);
					playerMovement.SetEnableMoveControll(false);
					AudioManager.Instance.PlayOneShot(FMODEvents.Instance.pieAttack, transform.position);
					playerMovement.MoveDistance(attackMoveDistance, attackMoveDuration, playerMovement.GetMoveInput());

					// Alternate between DrumStick1 / DrumStick2
					string anim = (cakeComboIndex % 2 == 0)
						? animationManager.drumStick1
						: animationManager.drumStick2;

					// VFX effect name
					string vfxName = (cakeComboIndex % 2 == 0)
						? "DrumStick_NormalAttack_1"
						: "DrumStick_NormalAttack_2";

					//VFXPool.Instance.SpawnVFX(vfxName, attackHitBox.transform.position, Quaternion.identity, 2f);

					// Handle HitBox and VFX through Spine events:
					animationManager.PlayAnimationOnce(anim, foodAttackHitBox, vfxName, () =>
					{

						// After animation: toggle combo index
						cakeComboIndex = 1 - cakeComboIndex;

						// Re-enable handItem
						handItem.gameObject.SetActive(true);
					});

					break;
				}

			default:
				Debug.Log("This food has not attack");
				return false;
		}

		return true;
	}


	public void SetAttackModeUI(AttackMode mode)
	{
		if (!isSwichWeaponFinish) return;

		attackMode = mode;

		switch(attackMode)
		{
			case AttackMode.Basic:
				weaponUIAnimator.SetInteger("WeaponType", 1);
				break;
			case AttackMode.Food:
				weaponUIAnimator.SetInteger("WeaponType", 2);
				break;
			default:
				weaponUIAnimator.SetInteger("WeaponType", 1);
				break;
		}
	}

	public AttackMode GetAttackMode() => attackMode;

	public void SetIsSwichWeaponFinish(bool isFinish) => isSwichWeaponFinish = isFinish;
}
