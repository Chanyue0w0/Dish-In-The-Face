using UnityEngine;
using Spine.Unity;

/// <summary>
/// 控制玩家 Spine 與 Animator 之間的橋接：
/// 1) 依移動/滑行/衝刺 更新 Animator Bool 與方向（isSide / isBack / isMove / isSlide / isDash）
/// 2) 自動切換 Animator Layer 權重（Base、SpecialMove、Attack、React）
/// 3) 提供攻擊/受傷的公開 API（Begin/EndAttack、SetAttackCombo、SetHurt）
/// 4) 保留原本 Spine 播放與一次性播放（含事件）
/// </summary>
public class PlayerSpineAnimationManager : MonoBehaviour
{
	#region ===== Animator Hash =====
	private static readonly int AnimIsSide       = Animator.StringToHash("isSide");
	private static readonly int AnimIsBack       = Animator.StringToHash("isBack");
	private static readonly int AnimIsMove       = Animator.StringToHash("isMove");
	private static readonly int AnimIsSlide      = Animator.StringToHash("isSlide");
	private static readonly int AnimIsDash       = Animator.StringToHash("isDash");
	private static readonly int AnimAttackCombo  = Animator.StringToHash("AttackCombo");
	private static readonly int AnimIsHurt       = Animator.StringToHash("isHurt");
	#endregion

	#region ===== Inspector：Spine / Animator 設定 =====
	[Header("Spine")]
	[SerializeField] private SkeletonAnimation skeletonAnim; // 角色的 SkeletonAnimation
	[SerializeField] private int baseTrack = 0;               // 主要動作 Track（一般用 0）
	
	[Header("Animator / Layers")]
	[SerializeField] private Animator animator;               // 角色 Animator（含多個 Layer）
	[SerializeField] private string baseLayerName      = "Base Move Layer";     // ex: idle / run
	[SerializeField] private string specialMoveLayerName = "Spical Move Layer"; // ex: slide / dash
	[SerializeField] private string attackLayerName    = "Attack Layer";        // 攻擊
	[SerializeField] private string reactLayerName     = "React Layer";         // 受傷/硬直
	#endregion


	#region ===== Inspector：動畫名稱（Spine） =====
	[Header("Spine Animation Names")]
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string idleFront;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string idleBack;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string runFront;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string runBack;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string runSide;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string dashNormal;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string dashSlide;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string[] gloveAttack;
	#endregion

	#region ===== 參考 =====
	[Header("Reference")]
	[SerializeField] private PlayerMovement playerMovement;
	#endregion

	#region ===== 狀態與快取 =====
	private string currentAnimName;
	private bool isAttacking;     // 是否處於攻擊動畫期（由 Begin/EndAttack 控制）
	private bool isHurt;          // 是否處於受傷/硬直（由 SetHurt 控制）
	private bool isSide = false;
	private bool isBack = false;
	
	private int baseLayerIndex;
	private int specialMoveLayerIndex;
	private int attackLayerIndex;
	private int reactLayerIndex;

	// 記錄目前啟用中的 Layer（用來偵測是否切換）
	private int activeLayerIndex = -1;

	// 本類別自己記錄的 idle 方向（當移動輸入為0時維持最後方向）
	private Vector2 lastNonZeroDir = Vector2.right;
	#endregion

	#region ===== Unity 生命週期 =====
	private void Awake()
	{
		if (!skeletonAnim) skeletonAnim = GetComponentInChildren<SkeletonAnimation>();
		if (skeletonAnim && skeletonAnim.AnimationState is { Data: not null })
			skeletonAnim.AnimationState.Data.DefaultMix = 0f; // 關閉自動混合，保證瞬切

		// 取得各 Layer Index（若名稱不正確，Index 會是 -1）
		baseLayerIndex        = SafeGetLayerIndex(animator, baseLayerName);
		specialMoveLayerIndex = SafeGetLayerIndex(animator, specialMoveLayerName);
		attackLayerIndex      = SafeGetLayerIndex(animator, attackLayerName);
		reactLayerIndex       = SafeGetLayerIndex(animator, reactLayerName);
	}

	private void Update()
	{
		UpdateAnimatorFromMovement();
	}
	#endregion

	#region ===== Animator 更新（由 PlayerMovement 狀態驅動） =====
	private void UpdateAnimatorFromMovement()
	{
		// 取得 PlayerMovement 的輸入與狀態
		Vector2 move = playerMovement.GetMoveInput();

		bool isMoving = (move != Vector2.zero);

		if (move.x != 0)
		{
			isSide = true;
			isBack = false;
		}
		else
		{
			isSide = false;
			if (move.y > 0) isBack = true;
			else if (move.y < 0) isBack = false;
		}

		// 寫入 Animator 參數
		animator.SetBool(AnimIsSide, isSide);
		animator.SetBool(AnimIsBack, isBack);
		animator.SetBool(AnimIsMove, isMoving);
		animator.SetBool(AnimIsSlide,  playerMovement.IsPlayerSlide());
		animator.SetBool(AnimIsDash, playerMovement.IsPlayerDash());
		
		// 更新 Layer
		if (animator.GetInteger(AnimAttackCombo) > 0)
		{
			ChangeLayerWeights(attackLayerIndex);
			return;
		}
		
		if (animator.GetBool(AnimIsSlide) || animator.GetBool(AnimIsDash))
		{
			ChangeLayerWeights(specialMoveLayerIndex);
			return;
		}
		
		ChangeLayerWeights(baseLayerIndex);
	}

	#endregion

	#region ===== Animator Layer 權重切換 =====
	private void ChangeLayerWeights(int targetLayer)
	{
		// 先全部歸 0，再依狀態開啟需要的 Layer
		SetLayerWeightSafe(baseLayerIndex,        0f);
		SetLayerWeightSafe(specialMoveLayerIndex, 0f);
		SetLayerWeightSafe(attackLayerIndex,      0f);
		SetLayerWeightSafe(reactLayerIndex,       0f);

		SetLayerWeightSafe(targetLayer, 1f);
		
		if (activeLayerIndex != targetLayer)
		{
			// 先 Update(0) 讓 Animator 取得正確的當前 StateInfo
			animator.Update(0f);

			var info = animator.GetCurrentAnimatorStateInfo(targetLayer);
			// 以 fullPathHash 重播當前狀態，時間歸 0
			if (info.fullPathHash != 0)
				animator.Play(info.fullPathHash, targetLayer, 0f);

			activeLayerIndex = targetLayer;
		}
	}

	private static int SafeGetLayerIndex(Animator ani, string layerName)
	{
		if (!ani || string.IsNullOrEmpty(layerName)) return -1;
		return ani.GetLayerIndex(layerName);
	}

	private void SetLayerWeightSafe(int layerIndex, float weight)
	{
		if (animator && layerIndex >= 0 && layerIndex < animator.layerCount)
			animator.SetLayerWeight(layerIndex, weight);
	}
	#endregion

	#region ===== Spine：切換與一次性播放（保留原本功能） =====
	/// <summary>如果動畫不同才切換；snap=true 會把 mixDuration 設為 0 直接切換</summary>
	private void SetAnimIfChanged(string animName, bool loop, bool snap)
	{
		if (!skeletonAnim || string.IsNullOrEmpty(animName)) return;
		if (currentAnimName == animName) return;

		currentAnimName = animName;
		var entry = skeletonAnim.AnimationState.SetAnimation(baseTrack, animName, loop);
		if (entry != null && snap) entry.MixDuration = 0f;
	}

	/// <summary>
	/// 播放一段 Spine 動畫（不循環），並監聽事件：
	/// - "Attack_HitStart": 可在此啟用 HitBox
	/// - "FX_Show": 於指定位置生成 VFX
	/// - "Attack_MoveStart"/"Attack_MoveEnd": 可臨時調整移動速度/控制
	/// 完成或中斷時自動收尾。
	/// </summary>
	public void PlayAnimationOnce(string animName)
	{
		if (!skeletonAnim || string.IsNullOrEmpty(animName)) return;

		var entry = skeletonAnim.AnimationState.SetAnimation(baseTrack, animName, false);
		if (entry == null) return;
		entry.MixDuration = 0f;

		entry.Event += (t, e) =>
		{
			switch (e.Data.Name)
			{
				case "Attack_HitStart":
					// TODO: 啟用 HitBox
					break;

				case "FX_Show":
					// TODO: 生成特效（可用物件池）
					break;

				case "Attack_MoveStart":
					// TODO: playerMovement.SetEnableMoveControl(false); playerMovement.SetMoveSpeed(attackMoveSpeed);
					break;

				case "Attack_MoveEnd":
					// TODO: playerMovement.SetEnableMoveControl(true); playerMovement.ResetMoveSpeed();
					break;

				case "Dodge_CancelEnable":
					// 可在這裡允許翻滾取消
					break;

				default:
					// Debug.Log("Animation event: " + e.Data.Name);
					break;
			}
		};

		bool closed = false;
		void Close()
		{
			if (closed) return;
			closed = true;
			// TODO: 關閉 HitBox / 還原控制
		}

		entry.Complete  += _ => Close();
		entry.End       += _ => Close();
		entry.Interrupt += _ => Close();
	}

	/// <summary>直接播放循環動畫（立即切換）</summary>
	public void PlayAnimation(string animName)
	{
		SetAnimIfChanged(animName, true, true);
	}
	#endregion

	#region ===== 攻擊 / 受傷：由外部系統呼叫 =====
	/// <summary>設定當前攻擊段數（寫入 Animator 整數參數 AttackCombo）</summary>
	public void SetAttackCombo(int comboIndex)
	{
		if (animator) animator.SetInteger(AnimAttackCombo, comboIndex);
	}

	/// <summary>開始攻擊：切 Attack Layer，並可選擇播放對應 Spine 攻擊動畫</summary>
	public void BeginAttack(int comboIndex = 0, bool playSpine = true)
	{
		isAttacking = true;
		SetAttackCombo(comboIndex);

		if (playSpine && gloveAttack != null && gloveAttack.Length > 0)
		{
			var idx = Mathf.Clamp(comboIndex, 0, gloveAttack.Length - 1);
			var anim = gloveAttack[idx];
			if (!string.IsNullOrEmpty(anim))
				PlayAnimationOnce(anim);
		}
	}

	/// <summary>結束攻擊：關 Attack Layer，回到循環動畫</summary>
	public void EndAttack()
	{
		isAttacking = false;
	}

	/// <summary>設定受傷/硬直狀態；true=進入 React Layer，false=離開</summary>
	public void SetHurt(bool value)
	{
		isHurt = value;
		if (animator) animator.SetBool(AnimIsHurt, isHurt);
	}
	#endregion
}
