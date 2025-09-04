using UnityEngine;
using PrimeTween;
using System.Collections.Generic;

public enum AttackMode { Food, Basic }

public class PlayerAttackController : MonoBehaviour
{
	private static readonly int WeaponType = Animator.StringToHash("WeaponType");

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

	#region ===== Inspector：連段重置 =====
	[Header("--------- Combo Settings ---------")]
	[Tooltip("距離上次『成功出招』超過此秒數，所有攻擊連段會重置回第一段（Food/Basic 通用）。")]
	[SerializeField, Min(0f)] private float comboResetSeconds = 1.2f;
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
	// Food 連段：不再寫死 2 段，會依手上食物的 FoodStatus.attackList 數量循環
	private int foodComboIndex = 0;
	// Basic 連段：三段（Glove1/2/3）
	private int gloveComboIndex = 0;

	private bool isSwitchWeaponFinish = true;  // 切武器動畫是否完成

	// 蓄力狀態
	private bool isCharging = false;
	private float currentChargeTime = 0f;

	// 連段重置計時：每次「攻擊動畫播完」才算一次成功出招
	private float lastAttackTime = -999f;
	#endregion

	#region ===== Unity =====
	private void Start()
	{
		isSwitchWeaponFinish = true;

		// 保險關閉 hitbox
		SafeSetActive(foodAttackHitBox, false);
		SafeSetActive(basicAttackHitBox, false);

		// 一開始就關 UI
		SetPowerBarVisible(false);
		UpdatePowerBarFill(0f);

		SetAttackModeUI(attackMode);
		lastAttackTime = -999f;
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

		// 攻擊開始前先檢查是否需要重置連段
		ResetCombosIfTimedOut();

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

	#region ===== Basic 模式：手套三段 =====
	private bool BasicAttack(bool isPowerAttack)
	{
		// 若頭上已有被抓的物件：優先丟出
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
		}

		return PlayGloveAttack();
	}

	private bool PlayGloveAttack()
	{
		if (animationManager != null && animationManager.IsBusy())
			return false;

		SafeSetActive(basicAttackHitBox, false);
		playerMovement.SetEnableMoveControl(false);

		AudioManager.Instance?.PlayOneShot(FMODEvents.Instance.pieAttack, transform.position);

		string anim = animationManager.gloveAttack[gloveComboIndex];

		animationManager.PlayAnimationOnce(anim, basicAttackHitBox, null, () =>
		{
			// 下一段
			gloveComboIndex = (gloveComboIndex + 1) % 3;
			// 成功出招時間點（用於連段重置）
			lastAttackTime = Time.time;
			playerMovement.SetEnableMoveControl(true);
		});

		return true;
	}
	
	#endregion

	#region ===== Food 模式：由 FoodStatus 決定動畫與段數 =====
	private bool FoodAttacks(bool isPowerAttack)
	{
		var status = GetFoodStatusInHand();
		if (status == null)
		{
			Debug.LogWarning("FoodStatus not found on handItem children.");
			return false;
		}

		// 讀取這個食物的攻擊清單
		var list = status.attackList;
		int count = list?.Count ?? 0;
		if (count <= 0)
		{
			Debug.Log("This food has no attackList, cannot perform food attack.");
			return false;
		}

		if (animationManager != null && animationManager.IsBusy())
			return false;

		// 若有多段：第二段之後依你原本習慣可消耗/處理物品（若仍要沿用，保留這行；不需要可移除）
		if (foodComboIndex > 0)
			playerMovement.DestroyFirstItem();

		// 暫時隱藏手上物件、鎖定移動
		if (handItem) handItem.gameObject.SetActive(false);
		playerMovement.SetEnableMoveControl(false);

		AudioManager.Instance?.PlayOneShot(FMODEvents.Instance.pieAttack, transform.position);

		// 依當前段數索引（循環）取出要播的動畫名稱
		int idx = Mathf.Abs(foodComboIndex) % count;
		string animName = list[idx]?.animationName;
		if (string.IsNullOrEmpty(animName))
		{
			Debug.LogWarning($"Food attack animation at index {idx} is null/empty. Abort.");
			// 還原移動/顯示
			if (handItem) handItem.gameObject.SetActive(true);
			playerMovement.SetEnableMoveControl(true);
			return false;
		}

		// 你原本使用 Spine 事件開關 HitBox/VFX，這裡仍沿用 hitbox 注入與完成回呼
		animationManager.PlayAnimationOnce(animName, foodAttackHitBox, /*vfx*/ null, () =>
		{
			// 下一段（依 attackList 數量循環）
			foodComboIndex = (foodComboIndex + 1) % count;

			// 記錄成功出招時間點
			lastAttackTime = Time.time;

			// 還原移動/顯示
			if (handItem) handItem.gameObject.SetActive(true);
			playerMovement.SetEnableMoveControl(true);
		});

		return true;
	}
	#endregion

	#region ===== UI / 武器切換 =====
	public void SetAttackModeUI(AttackMode mode)
	{
		if (!isSwitchWeaponFinish) return;
		attackMode = mode;

		if (weaponUIAnimator == null) return;
		switch (attackMode)
		{
			case AttackMode.Basic: weaponUIAnimator.SetInteger(WeaponType, 1); break;
			case AttackMode.Food:  weaponUIAnimator.SetInteger(WeaponType, 2); break;
			default:               weaponUIAnimator.SetInteger(WeaponType, 1); break;
		}
	}

	public AttackMode GetAttackMode() => attackMode;
	public void SetIsSwitchWeaponFinish(bool isFinish) => isSwitchWeaponFinish = isFinish;
	#endregion

	#region ===== 私有工具 =====
	private void ResetCombosIfTimedOut()
	{
		if (comboResetSeconds <= 0f) return; // 0 表示永不重置
		if (Time.time - lastAttackTime > comboResetSeconds)
		{
			foodComboIndex  = 0;
			gloveComboIndex = 0;
		}
	}

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

	/// <summary> 取得手上第一個食物的 FoodStatus（往下找 Children）。 </summary>
	private FoodStatus GetFoodStatusInHand()
	{
		if (!HasFoodInHand()) return null;
		// 取第一個子物件（你的專案通常把食物塞在第 0 個）
		Transform first = handItem.GetChild(0);
		if (first == null) return null;

		// 允許在孫層內
		return first.GetComponentInChildren<FoodStatus>();
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

	#region ===== 抓取 / 丟擲 =====
	private bool TryGrabEnemyOverHead()
	{
		if (grabOverHeadItem == null)
		{
			Debug.LogWarning("Grab failed: grabOverHeadItem is null.");
			return false;
		}

		var results = new List<Collider2D>(16);
		ContactFilter2D filter = new ContactFilter2D
		{
			useTriggers = true,
			useLayerMask = false
		};
		int count = grabOverHeadItem.OverlapCollider(filter, results);

		if (count <= 0) return false;

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

		if (best == null) return false;

		best.GetComponent<BeGrabByPlayer>().SetIsOnBeGrabing(true);
		Transform t = best.transform;
		Vector3 worldScale = t.lossyScale;
		t.SetParent(grabOverHeadItem.transform, true);
		t.localPosition = Vector3.zero;
		t.localRotation = Quaternion.identity;

		Vector3 pScale = grabOverHeadItem.transform.lossyScale;
		t.localScale = new Vector3(
			worldScale.x / (pScale.x == 0 ? 1 : pScale.x),
			worldScale.y / (pScale.y == 0 ? 1 : pScale.y),
			worldScale.z / (pScale.z == 0 ? 1 : pScale.z)
		);

		if (best.attachedRigidbody is { } rb)
		{
			rb.velocity = Vector2.zero;
			rb.angularVelocity = 0f;
			rb.isKinematic = true;
			rb.gravityScale = 0f;
		}

		return true;
	}

	private bool ThrowCarriedObject(Transform t)
	{
		if (t == null) return false;

		Vector3 worldScale = t.lossyScale;
		t.SetParent(null, true);
		t.localScale = worldScale;

		if (t.TryGetComponent<BeGrabByPlayer>(out var grab))
		{
			grab.SetIsOnBeGrabing(false);
		}

		Rigidbody2D rb = t.GetComponent<Rigidbody2D>();
		if (rb != null)
		{
			rb.velocity = Vector2.zero;
			rb.angularVelocity = 0f;
			rb.isKinematic = true;
		}

		Vector2 dir = GetAimDir().sqrMagnitude > 0f ? GetAimDir().normalized : Vector2.right;
		Vector3 start = t.position;
		Vector3 end = start + (Vector3)(dir * throwDistance);

		Tween.Position(t, end, throwDuration).OnComplete(() =>
		{
			if (t && rb != null)
			{
				rb.isKinematic = false;
				rb.gravityScale = 1f;
			}
		});

		return true;
	}
	#endregion

	#region ===== Gizmos（除錯視覺化） =====
#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
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
