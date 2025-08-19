using UnityEngine;
using Spine.Unity;

public class PlayerSpineAnimationManager : MonoBehaviour
{
	[Header("Spine")]
	[SerializeField] private SkeletonAnimation skeletonAnim; // 角色的 SkeletonAnimation
	[SerializeField] private int baseTrack = 0;              // 主要動作 Track（一般用 0）

	[Header("Animation Names")]
	[SerializeField] private string idleFront = "idle/idle";
	[SerializeField] private string idleBack = "idle/idle_back";
	[SerializeField] private string runFront = "run/run";
	[SerializeField] private string runBack = "run/run_back";
	[SerializeField] private string runSide = "run/run_side";

	[Header("Movement Judge")]
	[SerializeField] private float idleDeadzone = 0.05f;   // 視為靜止門檻（向量長度）
	[SerializeField] private float sideXThreshold = 0.05f; // 判定「有水平輸入」的 X 門檻

	// 狀態快取，避免重複 SetAnimation 造成重播
	private string currentAnimName = null;
	private float initialScaleX = 1f;

	private void Reset()
	{
		if (!skeletonAnim) skeletonAnim = GetComponentInChildren<SkeletonAnimation>();
	}

	private void Awake()
	{
		if (skeletonAnim && skeletonAnim.Skeleton != null)
			initialScaleX = skeletonAnim.Skeleton.ScaleX;

		// 取消所有跨動畫混合（避免漸變）
		if (skeletonAnim && skeletonAnim.AnimationState != null && skeletonAnim.AnimationState.Data != null)
			skeletonAnim.AnimationState.Data.DefaultMix = 0f;
	}

	/// <summary>由移動腳本每幀餵入目前移動向量</summary>
	public void UpdateFromMovement(Vector2 moveInput, bool _isDashingIgnored = false, bool _isSlidingIgnored = false)
	{
		if (!skeletonAnim || skeletonAnim.Skeleton == null) return;

		float sqrMag = moveInput.sqrMagnitude;
		bool isMoving = sqrMag > idleDeadzone * idleDeadzone;

		// ===== Idle =====
		if (!isMoving)
		{
			string idleName = (moveInput.y > 0f) ? idleBack : idleFront;
			SetAnimIfChanged(idleName, true, snap: true);
			return;
		}

		// ===== 優先側向（只要 |x| 超過門檻就視為側向）=====
		if (Mathf.Abs(moveInput.x) >= sideXThreshold)
		{
			SetAnimIfChanged(runSide, true, snap: true);
			SetFlipByX(moveInput.x); // 左右翻轉
			return;
		}

		// ===== 非側向：只在純往後才用 back；否則用前視 run =====
		if (moveInput.y > 0f) // 純往後
		{
			SetAnimIfChanged(runBack, true, snap: true);
			ResetFlipX();
		}
		else // 往前或斜前(此時 |x| < threshold)，一律視為前視 run
		{
			SetAnimIfChanged(runFront, true, snap: true);
			ResetFlipX();
		}
	}

	/// <summary>如果動畫不同才切換；snap=true 會把 mixDuration 設為 0 直接切換</summary>
	private void SetAnimIfChanged(string animName, bool loop, bool snap)
	{
		if (currentAnimName == animName) return;

		currentAnimName = animName;
		var entry = skeletonAnim.AnimationState.SetAnimation(baseTrack, animName, loop);
		if (entry != null && snap)
		{
			entry.MixDuration = 0f; // 強制這次切換不做漸變
		}
	}

	/// <summary>依 x 正負翻轉側向：x>0 面向右，x<0 面向左。</summary>
	private void SetFlipByX(float x)
	{
		//if (x > 0f)
		//	skeletonAnim.Skeleton.ScaleX = -Mathf.Abs(initialScaleX);  // 右
		//else if (x < 0f)
		//	skeletonAnim.Skeleton.ScaleX = Mathf.Abs(initialScaleX); // 左
		//															  // x≈0 不改變

		//// 立刻強制刷新骨架（版本相容）
		//skeletonAnim.Update(0f);
		//skeletonAnim.LateUpdate();
	}

	/// <summary>前/後動畫用：把 X 翻轉還原為初始。</summary>
	private void ResetFlipX()
	{
		//if (!Mathf.Approximately(skeletonAnim.Skeleton.ScaleX, initialScaleX))
		//{
		//	skeletonAnim.Skeleton.ScaleX = initialScaleX;
		//	// 強制刷新
		//	skeletonAnim.Update(0f);
		//	skeletonAnim.LateUpdate();
		//}
	}
}
