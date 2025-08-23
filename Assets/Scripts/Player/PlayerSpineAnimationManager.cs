using UnityEngine;
using Spine.Unity;

public class PlayerSpineAnimationManager : MonoBehaviour
{
	[Header("Spine")]
	[SerializeField] private SkeletonAnimation skeletonAnim; // 角色的 SkeletonAnimation
	[SerializeField] private int baseTrack = 0;              // 主要動作 Track（一般用 0）
	[SerializeField] private int overlayTrack = 1; // 專門放攻擊/一次性動畫
	private Vector2 lastMoveInput = Vector2.zero;  // 快取最後的移動輸入


	[Header("Animation Names")]
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)]  public string IdleFront;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string IdleBack;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string RunFront;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string RunBack;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string RunSide;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string DashNormal;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string DashSlide;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string DrumStick1;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string DrumStick2;

	[Header("Movement Judge")]
	[SerializeField] private float idleDeadzone = 0.05f;   // 視為靜止門檻（向量長度）
	[SerializeField] private float sideXThreshold = 0.05f; // 判定「有水平輸入」的 X 門檻
	// 放到 PlayerSpineAnimationManager 類別內
	private bool isOneShotPlaying = false;

	[Header("Reference")]
	[SerializeField] private PlayerMovement playerMovement;

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
		lastMoveInput = moveInput;

		// 如果 overlay 正在播一次性動畫，就讓底層照常更新或直接 return 都可。
		// 建議：底層維持「當前姿勢」，所以這裡不早退，照常根據 moveInput 決定 base track 的動畫。
		// 若你想一次性動畫時不更新底層，可以打開下面這行：
		// if (isOneShotPlaying) return;

		float sqrMag = moveInput.sqrMagnitude;
		bool isMoving = sqrMag > idleDeadzone * idleDeadzone;

		if (playerMovement.IsPlayerSlide())
		{
			SetAnimIfChanged(DashSlide, true, snap: true);
			return;
		}

		if (playerMovement.IsPlayerDash())
		{
			SetAnimIfChanged(DashNormal, true, snap: true);
			return;
		}

		if (!isMoving)
		{
			string idleName = (moveInput.y > 0f) ? IdleBack : IdleFront;
			SetAnimIfChanged(idleName, true, snap: true);
			return;
		}

		if (Mathf.Abs(moveInput.x) >= sideXThreshold)
		{
			SetAnimIfChanged(RunSide, true, snap: true);
			return;
		}

		if (moveInput.y > 0f)
			SetAnimIfChanged(RunBack, true, snap: true);
		else
			SetAnimIfChanged(RunFront, true, snap: true);
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

	/// <summary>
	/// 播放一段 Spine 動畫（不循環），並監聽事件：
	/// </summary>
	public void PlayAnimationOnce(string animName, GameObject hitBox, string vfxName, System.Action onComplete = null)
	{
		if (!skeletonAnim || string.IsNullOrEmpty(animName)) return;

		if (hitBox) hitBox.SetActive(false);

		// 在 overlay track（例如 1）播一次性動畫，不破壞 baseTrack 的移動/待機
		var entry = skeletonAnim.AnimationState.SetAnimation(overlayTrack, animName, false);
		if (entry == null) return;

		entry.MixDuration = 0f;
		isOneShotPlaying = true;

		entry.Event += (t, e) =>
		{
			var evtName = e.Data.Name;
			if (evtName == "Attack_HitStart") // - "Attack_HitStart": 啟用傳入的 hitBox
			{
				if (hitBox) hitBox.SetActive(true);
			}
			else if (evtName == "FX_Show" && !string.IsNullOrEmpty(vfxName)) // - "FX_Show": 於 hitBox（若為空則角色）位置生成傳入的 vfxName，生存 2 秒
			{
				var pos = hitBox ? hitBox.transform.position : skeletonAnim.transform.position;
				VFXPool.Instance.SpawnVFX(vfxName, pos, transform.rotation, 2f);
			}
		};

		bool closed = false;
		System.Action close = () =>
		{
			if (closed) return;
			closed = true;
			if (hitBox) hitBox.SetActive(false); // 動畫結束/被中斷時會自動關閉 hitBox、解除鎖定，並呼叫 onComplete（若提供）。

			// 清空 overlay track，讓底層（base track）立刻完全可見
			skeletonAnim.AnimationState.SetEmptyAnimation(overlayTrack, 0f);
			// 或者：skeletonAnim.AnimationState.ClearTrack(overlayTrack);

			isOneShotPlaying = false;

			// 這裡其實不一定要再呼叫 UpdateFromMovement，因為 base track 一直在跑
			// 若你想根據當前輸入再刷新一次，可保留：
			UpdateFromMovement(lastMoveInput);

			playerMovement.SetEnableMoveControll(true);
			onComplete?.Invoke();
		};

		entry.Complete += _ => close();
		entry.End += _ => close();
		entry.Interrupt += _ => close();
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


	public bool IsBusy() => isOneShotPlaying;
}
