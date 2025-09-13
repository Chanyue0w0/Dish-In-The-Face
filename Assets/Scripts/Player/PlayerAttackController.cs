using UnityEngine;
using PrimeTween;
using System.Collections.Generic;
using FMODUnity;

[System.Serializable]
public enum AttackMode { Food, Basic }

public class PlayerAttackController : MonoBehaviour
{
	private static readonly int WeaponType = Animator.StringToHash("WeaponType");

	#region ===== Inspector：一般設定 =====
	[Header("--------- Setting -----------")]
	[SerializeField] private AttackMode attackMode = AttackMode.Food;
	[SerializeField] private float defaultComboLimitTime = 1f;
	#endregion

	#region ===== Inspector：蓄力/重攻擊設定與 UI =====
	[Header("--------- Power Attack ---------")]
	[SerializeField, Min(0f)] private float heavyAttackThreshold = 0.35f;
	[SerializeField, Min(0.01f)] private float maxChargeTime = 2f;
	
	[Header("--------- Power Attack UI ---------")]
	[SerializeField] private GameObject powerAttackBar;
	[SerializeField] private Transform powerAttackBarFill;
	[SerializeField] private bool grabPickClosest = true;
	
	[Header("----- Grab Search (Position-based Gizmo) -----")]
	
	[SerializeField, Min(0f)] private float grabHoldThreshold = 0.2f;
	[SerializeField, Min(0f)] private float grabForwardOffset = 0.9f;
	[SerializeField, Min(0f)] private float throwDistance = 3f;
	[SerializeField, Min(0.01f)] private float throwDuration = 0.25f;
	#endregion

	#region ===== Inspector：參考物件 =====
	[Header("--------- Reference -----------")]
	[SerializeField] private Transform handItemGroup;           // 手上物件群組
	[SerializeField] private Collider2D grabOverHeadItem;       // 頭上「抓取區域」碰撞器（建議 Trigger）
	[SerializeField] private PlayerMovement playerMovement;
	[SerializeField] private Animator weaponUIAnimator;
	[SerializeField] private AnimationClip[] gloveAnimations;

	// ★ 新增：手套控制器（會驅動 SpineAnimationController）
	[SerializeField] private GloveController gloveController;
	#endregion

	#region ===== 私有狀態 =====
	private PlayerSpineAnimationManager animationManager;

	// 連段（Food/Basic 各自依序播放）
	private int comboIndex;

	private bool isSwitchWeaponFinish = true;

	// 蓄力/按住狀態（在 Basic 模式下當作「正在嘗試抓取」）
	private bool isCharging;
	private float currentChargeTime;

	// 抓取狀態
	private float grabHoldTimer = 0f;
	// 上次成功出招時間（如需做連段重置可用）
	private float lastAttackTime = -999f;
	#endregion

	#region ===== Unity =====
	private void Start()
	{
		animationManager   = GetComponent<PlayerSpineAnimationManager>();
		comboIndex         = 0;
		isSwitchWeaponFinish = true;
		isCharging         = false;
		currentChargeTime  = 0f;
		lastAttackTime     = -999f;

		SetPowerBarVisible(false);
		UpdatePowerBarFill(0f);
	}

	private void Update()
	{
		// Food 模式：正常蓄力並顯示 UI
		if (isCharging && attackMode == AttackMode.Food)
		{
			currentChargeTime += Time.deltaTime;
			if (currentChargeTime > maxChargeTime) currentChargeTime = maxChargeTime;
			UpdatePowerBarFill(currentChargeTime / maxChargeTime);
		}

		// Basic 模式：按住期間，經過 grabHoldThreshold 後才嘗試抓取
		if (isCharging && attackMode == AttackMode.Basic && !IsCarryingSomething())
		{
			grabHoldTimer += Time.deltaTime;
			if (grabHoldTimer >= grabHoldThreshold)
			{
				TryGrabEnemyOverHead();
			}
		}
	}
	#endregion

	#region ===== 對外：蓄力 / 按住控制 =====
	/// <summary>
	/// 按下攻擊鍵。
	/// - Basic：視為「開始抓取」，不顯示蓄力條，按住期間會持續嘗試抓。
	/// - Food：開始蓄力，顯示蓄力條（有餐點時）。
	/// </summary>
	public void BeginCharge(AttackMode mode)
	{
		attackMode = mode;
		if (playerMovement == null) return;
		if (playerMovement.IsPlayerDash() || playerMovement.IsPlayerSlide()) return;

		isCharging = true;
		currentChargeTime = 0f;

		if (attackMode == AttackMode.Basic)
		{
			grabHoldTimer = 0f;  // 重置抓取計時器
			SetPowerBarVisible(false);
		}
		else
		{
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
	}

	/// <summary>
	/// 放開攻擊鍵。
	/// - Basic：如果頭上有抓到目標→丟出；否則播放手套攻擊段數。
	/// - Food：依蓄力時間決定是否為重攻。
	/// </summary>
	public void ReleaseChargeAndAttack()
	{
		if (playerMovement == null) return;

		if (!isCharging)
		{
			// 安全處理：若未在按住狀態，就當作一般攻擊請求
			playerMovement.PerformAttack(false);
			return;
		}

		// 結束按住
		isCharging = false;
		SetPowerBarVisible(false);

		if (attackMode == AttackMode.Basic)
		{
			if (gloveController != null)
			{
				gloveController.gameObject.SetActive(false);
			}
			
			// 放開：有抓到就丟出；沒抓到就手套攻擊
			if (IsCarryingSomething())
			{
				var carried = grabOverHeadItem.transform.GetChild(0);
				if (carried) ThrowCarriedObject(carried);
				return;
			}
			// 沒抓到 → 直接走基本攻擊邏輯
			playerMovement.PerformAttack(false);
			return;
		}

		// Food 模式：依蓄力時間決定是否為重攻
		bool isPower =
			(currentChargeTime >= maxChargeTime) ||
			(currentChargeTime >= heavyAttackThreshold);

		currentChargeTime = 0f;
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

	#region ===== 對外攻擊入口（PlayerMovement 會回呼） =====
	public bool IsAttackSuccess(bool isPowerAttack)
	{
		if (attackMode == AttackMode.Basic)
		{
			return BasicAttack(); // 不再用 isPower 觸發抓取，抓取改在按住期間處理
		}

		if (attackMode == AttackMode.Food)
		{
			if (!HasFoodInHand()) return false; // 沒餐點就不能食物攻擊
			return FoodAttacks(isPowerAttack);
		}

		return false;
	}
	#endregion

	#region ===== Basic 模式：手套三段（放開鍵時若沒抓到才會走這裡） =====
	private bool BasicAttack()
	{
		// 若頭上已有被抓的物件：優先丟出（理論上放開時已處理，這裡再保險一次）
		if (IsCarryingSomething())
		{
			Transform carried = grabOverHeadItem.transform.GetChild(0);
			return ThrowCarriedObject(carried);
		}

		return PlayGloveAttack();
	}

	private bool PlayGloveAttack()
	{
		if (!animationManager.IsCanNextAttack())
			return false;

		int comboCount = gloveAnimations.Length;
		if (comboCount <= 0)
		{
			Debug.LogWarning("No glove animations configured.");
			return false;
		}
		float comboLimitTime = Mathf.Max(0f, defaultComboLimitTime);
		if (Time.time - lastAttackTime > comboLimitTime || comboIndex >= comboCount)
		{
			// 超過容許時間，從第一段重來
			comboIndex = 0;
		}

		AnimationClip attackAnimation = gloveAnimations[comboIndex];
		if (attackAnimation == null)
		{
			Debug.LogWarning($"Glove animation at index {comboIndex} is null. Abort.");
			return false;
		}
		
		lastAttackTime = Time.time;
		animationManager.PlayAttackAnimationClip(attackAnimation);
		comboIndex++;
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

		if (!animationManager.IsCanNextAttack())
			return false;

		// 這裡加入「依據 FoodStatus.comboAttackTime 重置 comboIndex」的判定
		float comboLimitTime = (status.comboAttackTime <= 0f) ? defaultComboLimitTime : status.comboAttackTime;

		var attackList = status.attackList;
		int attacklistCount = attackList?.Count ?? 0;
		if (attacklistCount <= 0)
		{
			Debug.Log("This food has no attackList, cannot perform food attack.");
			return false;
		}

		if (Time.time - lastAttackTime > comboLimitTime || comboIndex >= attacklistCount)
		{
			// 超過容許時間，從第一段重來
			comboIndex = 0;
		}
		
		if (attackList == null)
		{
			Debug.Log("no attack combo info");
			return false;
		}

		AnimationClip attackAnimation = attackList[comboIndex].animationClip;
		if (attackAnimation == null)
		{
			Debug.Log($"Food attack animation at index {comboIndex} is null/empty. Abort.");
			return false;
		}

		// attack success
		EventReference sfx = !attackList[comboIndex].sfx.IsNull ? attackList[comboIndex].sfx : status.defaultSfx;
		AudioManager.Instance.PlayOneShot(sfx, transform.position);
		lastAttackTime = Time.time;
		animationManager.PlayAttackAnimationClip(attackAnimation);
		comboIndex++;

		// 最後一段後消耗食物（維持原本行為）
		if (comboIndex > attacklistCount - 1)
			playerMovement.DestroyFirstItem();

		return true;
	}
	#endregion

	#region ===== UI / 武器切換 =====
	public void SetAttackModeUI(AttackMode mode)
	{
		if (!isSwitchWeaponFinish) return;
		attackMode = mode;

		if (mode == AttackMode.Basic) handItemGroup.gameObject.SetActive(false);
		else handItemGroup.gameObject.SetActive(true);

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
	private bool HasFoodInHand() => handItemGroup != null && handItemGroup.childCount > 0;

	/// <summary> 只有 Food 模式且手上有餐點才顯示蓄力條。 </summary>
	private bool ShouldShowChargeBar()
	{
		return attackMode == AttackMode.Food && HasFoodInHand();
	}

	private bool IsCarryingSomething()
	{
		return grabOverHeadItem != null && grabOverHeadItem.transform.childCount > 0;
	}

	private void SafeSetActive(GameObject go, bool active)
	{
		if (go != null && go.activeSelf != active) go.SetActive(active);
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
		Transform first = handItemGroup.GetChild(0);
		if (first == null) return null;
		return first.GetComponentInChildren<FoodStatus>();
	}

	/// <summary>
	/// 取得丟擲朝向：優先使用玩家移動輸入；若為零向量，以角色左右翻面判定。
	/// </summary>
	private Vector2 GetAimDir()
	{
		if (playerMovement != null)
		{
			Vector2 inDir = playerMovement.GetMoveInput();
			if (inDir.sqrMagnitude > 0.0001f)
				return inDir.normalized;
		}
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

		if (gloveController == null)
		{
			Debug.LogWarning("Grab failed: gloveController is null.");
			return false;
		}

		// 顯示手套、播一次 grabStrat
		gloveController.ShowGloveAndPlayStart();
		
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

		best.GetComponent<BeGrabByPlayer>().SetIsOnBeGrabbing(true);
		
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

		// ★ 成功抓到 → 播 grabEnd（一次）接 grabbing（loop）
		if (gloveController != null)
			gloveController.PlayGrabEndThenGrabbing();

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
			grab.SetIsOnBeGrabbing(false);
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

	#region ===== 給被抓物件／其他系統呼叫的事件 =====
	/// <summary>被抓物件真的掙脫了 → 關閉手套顯示。</summary>
	public void OnGrabbedObjectEscaped()
	{
		if (gloveController != null)
			gloveController.OnObjectEscaped();
	}

	/// <summary>被抓物件「嘗試掙脫」的反應（目前由 GloveController 內部保留、已註解）。</summary>
	public void OnGrabbedObjectTryEscape()
	{
		if (gloveController != null)
			gloveController.OnGrabbedObjectTryEscape();
	}
	#endregion

}
