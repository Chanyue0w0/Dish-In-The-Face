using Spine;
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
	private static readonly int AnimOnAttack  = Animator.StringToHash("OnAttack");
	private static readonly int AnimIsHurt       = Animator.StringToHash("isHurt");
	private const int BaseTrack = 0;               // 主要動作 Track（一般用 0）
	#endregion

	#region ===== Inspector：Spine / Animator 設定 =====
	[Header("Spine")]
	[SerializeField] private SkeletonAnimation skeletonAnim; // 角色的 SkeletonAnimation
	
	[Header("Animator / Layers")]
	[SerializeField] private Animator animator;               // 角色 Animator（含多個 Layer）
	[SerializeField] private string baseLayerName        = "Base Move Layer";     // ex: idle / run
	[SerializeField] private string specialMoveLayerName = "Spical Move Layer"; // ex: slide / dash
	[SerializeField] private string attackLayerName      = "Attack Layer";        // 攻擊
	[SerializeField] private string reactLayerName       = "React Layer";         // 受傷/硬直
	#endregion


	// #region ===== Inspector：動畫名稱（Spine） =====
	// [Header("Spine Animation Names")]
	// [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string idleFront;
	// [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string idleBack;
	// [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string runFront;
	// [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string runBack;
	// [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string runSide;
	// [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string dashNormal;
	// [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string dashSlide;
	// [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string[] gloveAttack;
	// #endregion

	#region ===== 參考 =====
	[Header("Reference")]
	[SerializeField] private PlayerMovement playerMovement;

	[SerializeField] private GameObject handItemsGroup;
	#endregion

	#region ===== 狀態與快取 =====
	private string currentAnimName;
	private bool onAttack;     // 是否處於攻擊動畫期（由 Begin/EndAttack 控制）
	private bool canNextAttack;
	private bool isHurt;          // 是否處於受傷/硬直（由 SetHurt 控制）
	private bool isSide;
	private bool isBack;
	
	private int baseLayerIndex;
	private int specialMoveLayerIndex;
	private int attackLayerIndex;
	private int reactLayerIndex;

	// 記錄目前啟用中的 Layer（用來偵測是否切換）
	private int activeLayerIndex = -1;

	#endregion

	#region ===== Unity 生命週期 =====
	private void Awake()
	{
		if (!skeletonAnim) skeletonAnim = GetComponentInChildren<SkeletonAnimation>();
		if (skeletonAnim && skeletonAnim.AnimationState is { Data: not null })
			skeletonAnim.AnimationState.Data.DefaultMix = 0f; // 關閉自動混合，保證瞬切

		isSide = false;
		isBack = false;
		onAttack = false;
		canNextAttack = true;
		
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

	#region ===== Animator 更新 =====
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
		// animator.SetInteger(AnimAttackCombo, comboIndex);
		
		// 更新 Layer
		if (onAttack)
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

	private void ChangeLayerWeights(int targetLayer)
	{
		if (activeLayerIndex == targetLayer) return;
		
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
	private TrackEntry SetAnimIfChanged(string animName, bool loop, bool snap)
	{
		if (!skeletonAnim || string.IsNullOrEmpty(animName)) return null;
		if (currentAnimName == animName) return null;

		currentAnimName = animName;
		var entry = skeletonAnim.AnimationState.SetAnimation(BaseTrack, animName, loop);
		if (entry != null && snap) entry.MixDuration = 0f;

		return entry;
	}

	/// <summary>
	/// 播放一段 Spine 動畫（不循環），並監聽事件：
	/// 完成或中斷時自動收尾。
	/// </summary>
	public void PlaySpineAnimationOnce(string animName)
	{
		if (!skeletonAnim || string.IsNullOrEmpty(animName)) return;

		// SetAnimIfChanged(animName, false, true);
		
		var entry = SetAnimIfChanged(animName, false, true);
		if (entry == null) return;
		entry.MixDuration = 0f;

		// bool closed = false;
		// void Close()
		// {
		// 	if (closed) return;
		// 	closed = true;
		//
		// 	if (animator.GetInteger(AnimAttackCombo) == comboIndex)
		// 		ResetAttackCombo();
		// }
		//
		// entry.Complete  += _ => Close();
		// entry.End       += _ => Close();
		// entry.Interrupt += _ => Close();
		
		entry.Event += (_, e) =>
		{
			switch (e.Data.Name)
			{
				case "FX_Show":
					// TODO: 生成特效（可用物件池）
					break;
			}
		};
	}


	/// <summary>直接播放循環動畫（立即切換）</summary>
	public void PlayAnimation(string animName)
	{
		SetAnimIfChanged(animName, true, true);
	}

	public void PlayAttackAnimationClip(AnimationClip animationClip)
	{
		handItemsGroup.SetActive(false);
		onAttack = true;
		canNextAttack = false;
		animator.SetTrigger(AnimOnAttack);
		animator.Play(animationClip.name);

		// ★ 播放完畢後把 handItemsGroup 關掉
		StartCoroutine(_WaitAndHideHandItems(animationClip));
	}

	// 新增：等待動畫播放完畢再關閉 handItemsGroup
	private System.Collections.IEnumerator _WaitAndHideHandItems(AnimationClip clip)
	{
		if (clip == null)
			yield break;

		// 以 Animator 的播放倍率估算實際時長（簡化處理）
		float speed = (animator != null && animator.speed > 0f) ? animator.speed : 1f;
		yield return new WaitForSeconds(clip.length / speed);

		handItemsGroup.SetActive(true);
	}
	#endregion

	#region ===== 攻擊 / 受傷：由外部系統呼叫 =====

	/// <summary>設定受傷/硬直狀態；true=進入 React Layer，false=離開</summary>
	public void SetHurt(bool value)
	{
		isHurt = value;
		if (animator) animator.SetBool(AnimIsHurt, isHurt);
	}

	public bool IsAnimationOnAttack() => onAttack;
	public bool IsCanNextAttack() => canNextAttack;
	public void CanNextAttack() => canNextAttack = true;
	public void ResetAttackCombo()
	{
		// Debug.Log("reset combo");
		onAttack = false;
		canNextAttack = true;
	}
	
	#endregion
}
