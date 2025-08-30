using UnityEngine;
using FoodsGroup;
using PrimeTween;
using System.Collections.Generic;

public enum AttackMode { Food, Basic }

public class PlayerAttackController : MonoBehaviour
{
	#region ===== Inspector：一般設定 =====
	[Header("--------- Setting -----------")]
	[SerializeField] private AttackMode attackMode = AttackMode.Food;

	[Tooltip("食物攻擊時，玩家向前位移的距離（一般攻擊）。")]
	[SerializeField] private float attackMoveDistance = 5f;

	[Tooltip("食物攻擊時，玩家向前位移花費的時間（一般攻擊）。")]
	[SerializeField] private float attackMoveDuration = 0.5f;

	#endregion

	#region ===== Inspector：蓄力/重攻擊設定與 UI =====
	[Header("--------- Power Attack ---------")]
	[Tooltip("觸發重攻擊的最短蓄力秒數（達到此值以上就算重攻擊）。")]
	[SerializeField, Min(0f)] private float heavyAttackThreshold = 0.35f;

	[Tooltip("可蓄力的最大秒數（UI 進度條會以此作為 100%）。")]
	[SerializeField, Min(0.01f)] private float maxChargeTime = 2f;

	[Header("--------- Power Attack UI ---------")]
	[Tooltip("蓄力條外框。開始蓄力時顯示；未蓄力/沒有餐點(食物模式)/結束時關閉。")]
	[SerializeField] private GameObject powerAttackBar;

	[Tooltip("蓄力條填滿物件：依 currentChargeTime / maxChargeTime 縮放 X（0→1）。")]
	[SerializeField] private Transform powerAttackBarFill;

	[Tooltip("多個可抓目標時，挑最近者。")]
	[SerializeField] private bool grabPickClosest = true;

	[Header("----- Grab Search (Position-based Gizmo) -----")]
	
	[Tooltip("Gizmos 顯示：抓取檢測盒中心沿著面向方向的位移量（視覺化用）。")]
	[SerializeField, Min(0f)] private float grabForwardOffset = 0.9f;

	[Tooltip("Gizmos 顯示：抓取檢測盒的寬x高（視覺化用）。")]
	[SerializeField] private Vector2 grabBoxSize = new Vector2(1.2f, 1.0f);
	[Header("----- Throw Settings -----")]
	[Tooltip("把頭上的子物件往面向方向丟出的距離")]
	[SerializeField, Min(0f)] private float throwDistance = 3f;

	[Tooltip("丟出移動所花時間")]
	[SerializeField, Min(0.01f)] private float throwDuration = 0.25f;
	
	#endregion

	#region ===== Inspector：參考物件 =====
	[Header("--------- Reference -----------")]
	[SerializeField] private Transform handItem;           // 手上物件群組
	[SerializeField] private Collider2D grabOverHeadItem;  // 抓到頭上的「抓取區域」碰撞器（建議Trigger）
	[SerializeField] private GameObject foodAttackHitBox;  // 食物攻擊 HitBox
	[SerializeField] private GameObject basicAttackHitBox; // 基礎攻擊 HitBox
	[SerializeField] private PlayerMovement playerMovement;
	[SerializeField] private PlayerSpineAnimationManager animationManager;
	[SerializeField] private Animator weaponUIAnimator;
	#endregion

	#region ===== 私有狀態 =====
	private int cakeComboIndex = 0;          // DrumStick1/2 切換
	private bool isSwichWeaponFinish = true; // 切武器動畫是否完成

	// 蓄力狀態
	private bool isCharging = false;
	private float currentChargeTime = 0f;
	#endregion

	#region ===== Unity =====
	private void Start()
	{
		isSwichWeaponFinish = true;

		// 保險關閉 hitbox
		SafeSetActive(foodAttackHitBox, false);
		SafeSetActive(basicAttackHitBox, false);

		// 一開始就關 UI
		SetPowerBarVisible(false);
		UpdatePowerBarFill(0f);

		SetAttackModeUI(attackMode);
	}

	private void Update()
	{
		// 累積蓄力並更新 UI（如果 UI 正在顯示）
		if (isCharging)
		{
			currentChargeTime += Time.deltaTime;
			if (currentChargeTime > maxChargeTime) currentChargeTime = maxChargeTime;
			UpdatePowerBarFill(currentChargeTime / maxChargeTime);
		}
	}
	#endregion

	#region ===== 對外：蓄力控制 =====
	/// <summary> 開始蓄力（按下攻擊鍵）。Food 模式沒餐點就不顯示 UI。 </summary>
	public void BeginCharge()
	{
		if (playerMovement == null) return;
		if (playerMovement.IsPlayerDash() || playerMovement.IsPlayerSlide()) return;

		isCharging = true;
		currentChargeTime = 0f;

		// 只有 Basic，或 Food 且有餐點 才顯示條
		if (ShouldShowChargeBar())
		{
			SetPowerBarVisible(true);
			UpdatePowerBarFill(0f);
		}
		else
		{
			SetPowerBarVisible(false);
		}
	}

	/// <summary>
	/// 放開攻擊鍵：依蓄力結算 普攻/重攻擊。滿條一定是重攻；或超過 heavyAttackThreshold 也算重攻。
	/// </summary>
	public void ReleaseChargeAndAttack()
	{
		if (playerMovement == null) return;

		// 未蓄力：視為點按普攻
		if (!isCharging)
		{
			playerMovement.PerformAttack(false);
			return;
		}

		bool isPower =
			(currentChargeTime >= maxChargeTime) ||           // 滿條＝重攻
			(currentChargeTime >= heavyAttackThreshold);       // 達門檻＝重攻

		isCharging = false;
		SetPowerBarVisible(false);

		playerMovement.PerformAttack(isPower);
	}

	/// <summary> 取消蓄力（如 Dash/Slide），不觸發攻擊並關 UI。 </summary>
	public void CancelChargeIfAny()
	{
		if (!isCharging) return;
		isCharging = false;
		currentChargeTime = 0f;
		SetPowerBarVisible(false);
		UpdatePowerBarFill(0f);
	}
	#endregion

	#region ===== 對外攻擊入口 =====
	/// <summary> PlayerMovement.PerformAttack() 會回呼到這裡。 </summary>
	public bool IsAttackSuccess(bool isPowerAttack)
	{
		EnsureAnimationManager();

		if (attackMode == AttackMode.Basic)
		{
			return BasicAttack(isPowerAttack);
		}

		if (attackMode == AttackMode.Food)
		{
			// Food 模式沒餐點就不能打
			if (!HasFoodInHand()) return false;
			return FoodAttacks(isPowerAttack);
		}

		return false;
	}
	#endregion

	#region ===== Basic 模式：普/重/抓取/丟出 =====
	private bool BasicAttack(bool isPowerAttack)
	{
		// 若頭上已有被抓的物件：優先丟出，然後就結束這次 Basic 攻擊
		if (grabOverHeadItem != null && grabOverHeadItem.transform.childCount > 0)
		{
			Transform carried = grabOverHeadItem.transform.GetChild(0);
			return ThrowCarriedObject(carried);
		}

		// 重攻：先嘗試抓取，抓不到就當強化普攻
		if (isPowerAttack)
		{
			bool grabbed = TryGrabEnemyOverHead();
			if (grabbed) return true;
			return DoBasicHitboxSlash("BasicAttack", 0.5f, true);
		}
		// 普攻
		return DoBasicHitboxSlash("BasicAttack", 0.5f, true);
	}

	private bool DoBasicHitboxSlash(string vfxKey, float autoOffDelay, bool sfxPie)
	{
		SafeSetActive(basicAttackHitBox, true);

		VFXPool.Instance?.SpawnVFX(vfxKey, basicAttackHitBox.transform.position, transform.rotation, 1f);

		if (sfxPie)
			AudioManager.Instance?.PlayOneShot(FMODEvents.Instance.pieAttack, transform.position);

		Tween.Delay(autoOffDelay, () => SafeSetActive(basicAttackHitBox, false));
		return true;
	}

	/// <summary>
	/// 使用 grabOverHeadItem 區域來搜尋可抓取的敵人：
	/// 條件：tag=="Enemy" 或 "enemy"；且具 BeGrabByPlayer 並允許被抓。
	/// 成功後將物件變成 grabOverHeadItem.transform 子物件，local 位置/旋轉歸零。
	/// </summary>
	private bool TryGrabEnemyOverHead()
	{
		if (grabOverHeadItem == null)
		{
			Debug.LogWarning("Grab failed: grabOverHeadItem is null.");
			return false;
		}

		// 收集與抓取區域重疊的碰撞器
		var results = new List<Collider2D>(16);
		ContactFilter2D filter = new ContactFilter2D
		{
			useTriggers = true,
			useLayerMask = false // 不使用 Layer，改由 Tag 篩選
		};
		int count = grabOverHeadItem.OverlapCollider(filter, results);

		if (count <= 0)
		{
			Debug.Log("Grab null");
			return false;
		}

		// 從重疊清單中挑選符合條件者
		Collider2D best = null;
		float bestDist = float.PositiveInfinity;
		Vector2 center = grabOverHeadItem.bounds.center;

		foreach (var c in results)
		{
			if (c == null) continue;
			var grab = c.GetComponent<BeGrabByPlayer>();
			if (grab == null || !grab.GetIsCanBeGrabByPlayer()) continue;

			if (!grabPickClosest) { best = c; break; }

			float d = ((Vector2)c.bounds.center - center).sqrMagnitude;
			if (d < bestDist)
			{
				bestDist = d;
				best = c;
			}
		}

		if (best == null)
		{
			Debug.Log("Grab null");
		 return false;
		}

		// 真的抓起來：改父物件→放頭上→維持世界尺度
		best.GetComponent<BeGrabByPlayer>().SetIsOnBeGrabing(true);
		Transform t = best.transform;
		Vector3 worldScale = t.lossyScale;// 記住目前的世界尺度
		// 設為子物件但保留世界座標/旋轉/尺度
		t.SetParent(grabOverHeadItem.transform, true);

		// 移到頭上（不影響尺度）
		t.localPosition = Vector3.zero;
		t.localRotation = Quaternion.identity;

		// （可選保險）重新套回世界尺度，以避免浮點誤差
		Vector3 pScale = grabOverHeadItem.transform.lossyScale;
		t.localScale = new Vector3(
			worldScale.x / (pScale.x == 0 ? 1 : pScale.x),
			worldScale.y / (pScale.y == 0 ? 1 : pScale.y),
			worldScale.z / (pScale.z == 0 ? 1 : pScale.z)
		);

		// 可選：若有剛體，讓它在頭上穩定
		if (best.attachedRigidbody is Rigidbody2D rb)
		{
			rb.velocity = Vector2.zero;
			rb.angularVelocity = 0f;
			rb.isKinematic = true;
			rb.gravityScale = 0f;
		}

		Debug.Log("Grab success");
		return true;
	}
	
	/// <summary>
	/// 將頭上的子物件往目前面向方向丟出一段距離；維持世界尺度不變。
	/// </summary>
	private bool ThrowCarriedObject(Transform t)
	{
		if (t == null) return false;

		// 從頭上取下：保留世界座標/旋轉/尺度
		Vector3 worldScale = t.lossyScale;
		t.SetParent(null, true);
		t.localScale = worldScale;

		// 若有可抓取腳本，解除「被抓」狀態
		if (t.TryGetComponent<BeGrabByPlayer>(out var grab))
		{
			grab.SetIsOnBeGrabing(false);
		}

		// 若有剛體：先暫時設為運動學避免與 tween 打架
		Rigidbody2D rb = t.GetComponent<Rigidbody2D>();
		if (rb != null)
		{
			rb.velocity = Vector2.zero;
			rb.angularVelocity = 0f;
			rb.isKinematic = true;
			// 丟完再恢復重力（避免丟的過程受重力影響偏移）
		}

		// 計算丟擲方向與目標位置
		Vector2 dir = GetAimDir().sqrMagnitude > 0f ? GetAimDir().normalized : Vector2.right;
		Vector3 start = t.position;
		Vector3 end = start + (Vector3)(dir * throwDistance);

		// 以 PrimeTween 做世界座標位移（線性）
		Tween.Position(t, end, throwDuration).OnComplete(() =>
		{
			// 丟完恢復物理
			if (t && rb != null)
			{
				rb.isKinematic = false;
				rb.gravityScale = 1f; // 如需自訂原始值，可在抓取時記錄後還原
			}
		});

		return true;
	}
	#endregion

	#region ===== Food 模式：普/重 =====
	private bool FoodAttacks(bool isPowerAttack)
	{
		var status = handItem ? handItem.GetComponentInChildren<FoodStatus>() : null;
		if (status == null)
		{
			Debug.LogWarning("FoodStatus not found on handItem children.");
			return false;
		}

		switch (status.foodType)
		{
			case FoodType.Beer:
			case FoodType.Drumstick:
			{
				if (animationManager != null && animationManager.IsBusy())
					return false;

				// combo>0 先消耗第一個物品（沿用你的習慣）
				if (cakeComboIndex > 0) playerMovement.DestoryFirstItem();

				// 暫時隱藏手上物件、鎖定移動
				if (handItem) handItem.gameObject.SetActive(false);
				playerMovement.SetEnableMoveControll(false);

				float moveDistance = isPowerAttack ? attackMoveDistance * 1.2f : attackMoveDistance;
				float moveDuration = isPowerAttack ? Mathf.Max(0.3f, attackMoveDuration * 0.9f) : attackMoveDuration;

				AudioManager.Instance?.PlayOneShot(FMODEvents.Instance.pieAttack, transform.position);
				playerMovement.MoveDistance(moveDistance, moveDuration, playerMovement.GetMoveInput());

				string anim = ResolveFoodAnim(isPowerAttack);
				string vfxName = ResolveFoodVFX(isPowerAttack);

				// 由 Spine 事件開關 HitBox/VFX
				animationManager.PlayAnimationOnce(anim, foodAttackHitBox, vfxName, () =>
				{
					cakeComboIndex = 1 - cakeComboIndex;
					if (handItem) handItem.gameObject.SetActive(true);
					playerMovement.SetEnableMoveControll(true);
				});
				return true;
			}
			default:
				Debug.Log("This food has not attack");
				return false;
		}
	}

	private string ResolveFoodAnim(bool isPower)
	{
		return (cakeComboIndex % 2 == 0) ? animationManager.drumStick1 : animationManager.drumStick2;
	}

	private string ResolveFoodVFX(bool isPower)
	{
		if (isPower)
			return (cakeComboIndex % 2 == 0) ? "DrumStick_PowerAttack_1" : "DrumStick_PowerAttack_2";
		else
			return (cakeComboIndex % 2 == 0) ? "DrumStick_NormalAttack_1" : "DrumStick_NormalAttack_2";
	}
	#endregion

	#region ===== UI / 武器切換 =====
	public void SetAttackModeUI(AttackMode mode)
	{
		if (!isSwichWeaponFinish) return;
		attackMode = mode;

		if (weaponUIAnimator == null) return;
		switch (attackMode)
		{
			case AttackMode.Basic: weaponUIAnimator.SetInteger("WeaponType", 1); break;
			case AttackMode.Food:  weaponUIAnimator.SetInteger("WeaponType", 2); break;
			default:               weaponUIAnimator.SetInteger("WeaponType", 1); break;
		}
	}

	public AttackMode GetAttackMode() => attackMode;
	public void SetIsSwichWeaponFinish(bool isFinish) => isSwichWeaponFinish = isFinish;
	#endregion

	#region ===== 私有工具 =====
	private bool HasFoodInHand() => handItem != null && handItem.childCount > 0;

	private bool ShouldShowChargeBar()
	{
		// Basic：一定顯示；Food：只有有餐點才顯示
		return attackMode == AttackMode.Basic || (attackMode == AttackMode.Food && HasFoodInHand());
	}

	private void SafeSetActive(GameObject go, bool active)
	{
		if (go != null && go.activeSelf != active) go.SetActive(active);
	}

	private void EnsureAnimationManager()
	{
		if (animationManager == null)
			animationManager = GetComponent<PlayerSpineAnimationManager>();
	}

	private void SetPowerBarVisible(bool visible)
	{
		if (powerAttackBar) powerAttackBar.SetActive(visible);
	}

	private void UpdatePowerBarFill(float ratio01)
	{
		if (!powerAttackBarFill) return;
		ratio01 = Mathf.Clamp01(ratio01);
		Vector3 s = powerAttackBarFill.localScale;
		powerAttackBarFill.localScale = new Vector3(ratio01, s.y, s.z);
	}

	/// <summary>
	/// 取得「抓取」時的朝向：優先使用玩家當下移動輸入；若為零向量，以角色左右翻面判定。
	/// </summary>
	private Vector2 GetAimDir()
	{
		if (playerMovement != null)
		{
			Vector2 inDir = playerMovement.GetMoveInput();
			if (inDir.sqrMagnitude > 0.0001f)
				return inDir.normalized;
		}
		// y 角接近 180 視為向右，否則向左（你的翻面邏輯）
		float y = transform.eulerAngles.y;
		bool facingRight = Mathf.Abs(Mathf.DeltaAngle(y, 180f)) < 1f;
		return facingRight ? Vector2.right : Vector2.left;
	}
	#endregion

	#region ===== Gizmos（除錯視覺化） =====
#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		// 抓取檢測盒視覺化（僅供參考）
		Vector2 aim = Application.isPlaying ? GetAimDir() : Vector2.right;
		Vector2 center = (Vector2)transform.position + aim * grabForwardOffset;
		float angleDeg = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;

		UnityEditor.Handles.color = new Color(1f, 0.8f, 0.2f, 0.8f);
		Matrix4x4 m = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, angleDeg), Vector3.one);
		using (new UnityEditor.Handles.DrawingScope(m))
		{
			UnityEditor.Handles.DrawWireCube(Vector3.zero, new Vector3(grabBoxSize.x, grabBoxSize.y, 0.1f));
		}
	}
#endif
	#endregion
}
